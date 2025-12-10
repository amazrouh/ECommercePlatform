#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploys Notification Service to Azure with minimal cost (Free Tier)

.DESCRIPTION
    This script deploys the Notification Service using Azure Free Tier resources
    and minimal configuration for cost-effective deployment.

.PARAMETER Environment
    The target environment (dev, staging, prod)

.PARAMETER Location
    Azure region for deployment (default: eastus)

.PARAMETER SqlPassword
    SQL Server administrator password (required)

.EXAMPLE
    .\deploy-basic.ps1 -Environment dev -SqlPassword "MySecurePass123!"

.EXAMPLE
    .\deploy-basic.ps1 -Environment staging -Location "westus2" -SqlPassword "MySecurePass123!"
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment,

    [Parameter(Mandatory = $false)]
    [string]$Location = "eastus",

    [Parameter(Mandatory = $true)]
    [string]$SqlPassword
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Configuration
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath
$resourceGroupName = "notification-$Environment-rg"
$appName = "notification-$Environment"

Write-Host "🚀 Deploying Notification Service (Basic/Free Tier)" -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Gray
Write-Host "Location: $Location" -ForegroundColor Gray
Write-Host "Resource Group: $resourceGroupName" -ForegroundColor Gray
Write-Host ""

try {
    # Check prerequisites
    Write-Host "Checking prerequisites..." -ForegroundColor Yellow

    # Check Azure CLI
    $azVersion = az --version 2>$null | Select-Object -First 1
    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI is not installed or not in PATH"
    }
    Write-Host "✓ Azure CLI available" -ForegroundColor Green

    # Check login
    $account = az account show --query 'name' -o tsv 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Not logged in to Azure CLI. Run 'az login' first."
    }
    Write-Host "✓ Azure CLI logged in: $account" -ForegroundColor Green

    # Create resource group
    Write-Host "Creating resource group..." -ForegroundColor Yellow
    az group create --name $resourceGroupName --location $Location --output none
    Write-Host "✓ Resource group created" -ForegroundColor Green

    # Deploy infrastructure (basic tier)
    Write-Host "Deploying infrastructure (basic tier)..." -ForegroundColor Yellow
    $deploymentResult = az deployment group create `
        --resource-group $resourceGroupName `
        --template-file "$rootPath/infra/main.bicep" `
        --parameters environmentName=$Environment `
        --parameters location=$Location `
        --parameters sqlAdministratorPassword=$SqlPassword `
        --parameters enablePremiumFeatures=false `
        --output json

    if ($LASTEXITCODE -ne 0) {
        throw "Infrastructure deployment failed"
    }
    Write-Host "✓ Infrastructure deployed successfully" -ForegroundColor Green

    # Extract deployment outputs
    $outputs = $deploymentResult | ConvertFrom-Json
    $appServiceName = $outputs.properties.outputs.appServiceName.value
    $appServiceUrl = $outputs.properties.outputs.appServiceUrl.value
    $sqlServerName = $outputs.properties.outputs.sqlServerName.value
    $sqlDatabaseName = $outputs.properties.outputs.sqlDatabaseName.value

    Write-Host "Deployment outputs:" -ForegroundColor Cyan
    Write-Host "  App Service: $appServiceName" -ForegroundColor Gray
    Write-Host "  URL: $appServiceUrl" -ForegroundColor Gray
    Write-Host "  SQL Server: $sqlServerName" -ForegroundColor Gray
    Write-Host "  Database: $sqlDatabaseName" -ForegroundColor Gray
    Write-Host ""

    # Build and publish application
    Write-Host "Building and publishing application..." -ForegroundColor Yellow

    # Navigate to project directory
    Push-Location "$rootPath/src/NotificationService"

    # Build Docker image
    Write-Host "Building Docker image..." -ForegroundColor Gray
    docker build -t notificationservice:latest -f "$rootPath/Dockerfile" "$rootPath"
    if ($LASTEXITCODE -ne 0) {
        throw "Docker build failed"
    }
    Write-Host "✓ Docker image built" -ForegroundColor Green

    # Tag and push to Azure Container Registry (ACR)
    Write-Host "Pushing to Azure Container Registry..." -ForegroundColor Gray

    # Get ACR name (assuming it was created by Bicep)
    $acrName = az acr list --resource-group $resourceGroupName --query '[0].name' -o tsv 2>$null
    if (-not $acrName) {
        # Create ACR if not exists (for basic deployment)
        $acrName = "$($appName)acr"
        az acr create --resource-group $resourceGroupName --name $acrName --sku Basic --output none
    }

    # Login to ACR
    az acr login --name $acrName --output none

    # Tag image
    $imageTag = "$acrName.azurecr.io/notificationservice:latest"
    docker tag notificationservice:latest $imageTag

    # Push image
    docker push $imageTag
    if ($LASTEXITCODE -ne 0) {
        throw "Docker push failed"
    }
    Write-Host "✓ Docker image pushed to ACR" -ForegroundColor Green

    # Configure App Service to use container
    Write-Host "Configuring App Service for container deployment..." -ForegroundColor Gray
    az webapp config container set `
        --name $appServiceName `
        --resource-group $resourceGroupName `
        --docker-custom-image-name $imageTag `
        --docker-registry-server-url "https://$acrName.azurecr.io" `
        --output none

    Write-Host "✓ App Service configured for container" -ForegroundColor Green

    # Set environment variables
    Write-Host "Configuring environment variables..." -ForegroundColor Yellow

    # Generate JWT secret
    $jwtSecret = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | ForEach-Object {[char]$_})

    # Set basic app settings
    az webapp config appsettings set `
        --name $appServiceName `
        --resource-group $resourceGroupName `
        --setting WEBSITES_PORT=80 `
        --setting ASPNETCORE_ENVIRONMENT=$Environment `
        --setting Jwt__Secret=$jwtSecret `
        --setting Jwt__Issuer=$appServiceUrl `
        --setting Jwt__Audience=$appServiceUrl `
        --setting ConnectionStrings__NotificationDb="Server=$sqlServerName.database.windows.net;Database=$sqlDatabaseName;Authentication=Active Directory Default;" `
        --output none

    Write-Host "✓ Environment variables configured" -ForegroundColor Green

    # Apply database migrations
    Write-Host "Applying database migrations..." -ForegroundColor Yellow

    # For basic deployment, we'll use SQL authentication
    # In production, you'd want to use managed identity
    $sqlConnectionString = "Server=$sqlServerName.database.windows.net;Database=$sqlDatabaseName;User Id=sqladmin;Password=$SqlPassword;"

    # Wait for app to be ready
    Write-Host "Waiting for App Service to be ready..." -ForegroundColor Gray
    Start-Sleep -Seconds 30

    # Note: For basic deployment, you might need to manually run migrations
    # or add a migration endpoint to your application
    Write-Host "⚠️ Note: Database migrations need to be applied manually for basic deployment" -ForegroundColor Yellow
    Write-Host "   Connection String: $sqlConnectionString" -ForegroundColor Gray

    # Return to original location
    Pop-Location

    # Final summary
    Write-Host ""
    Write-Host "🎉 Deployment completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 Deployment Summary:" -ForegroundColor Cyan
    Write-Host "  Environment: $Environment" -ForegroundColor White
    Write-Host "  App Service URL: $appServiceUrl" -ForegroundColor White
    Write-Host "  Health Check: $appServiceUrl/health" -ForegroundColor White
    Write-Host "  Swagger UI: $appServiceUrl/swagger" -ForegroundColor White
    Write-Host ""
    Write-Host "💰 Estimated Monthly Cost: ~$15-25" -ForegroundColor Green
    Write-Host "  - App Service (Free): $0" -ForegroundColor Gray
    Write-Host "  - SQL Database (Basic): ~$5/month" -ForegroundColor Gray
    Write-Host "  - Storage (for logs): ~$0.10/month" -ForegroundColor Gray
    Write-Host "  - Bandwidth: ~$0-10/month" -ForegroundColor Gray
    Write-Host ""
    Write-Host "🔧 Next Steps:" -ForegroundColor Yellow
    Write-Host "  1. Apply database migrations using the connection string above" -ForegroundColor White
    Write-Host "  2. Configure email/SMS providers in app settings" -ForegroundColor White
    Write-Host "  3. Test the API endpoints" -ForegroundColor White
    Write-Host "  4. Set up monitoring alerts if needed" -ForegroundColor White

} catch {
    Write-Error "Deployment failed: $($_.Exception.Message)"

    # Cleanup on failure (optional)
    Write-Host "Consider cleaning up resources with:" -ForegroundColor Yellow
    Write-Host "az group delete --name $resourceGroupName --yes --no-wait" -ForegroundColor Gray

    exit 1
}
