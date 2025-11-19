#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates deployment environment and configuration for Notification Service

.DESCRIPTION
    This script performs comprehensive validation of the deployment environment
    including infrastructure, configuration, security, and dependencies.

.PARAMETER Environment
    The target environment (dev, staging, prod)

.PARAMETER ResourceGroup
    The Azure resource group name

.PARAMETER AppName
    The Azure App Service name

.PARAMETER SkipInfrastructure
    Skip infrastructure validation

.PARAMETER SkipSecurity
    Skip security validation

.EXAMPLE
    .\validate-deployment.ps1 -Environment staging -ResourceGroup notification-staging-rg -AppName notification-staging

.EXAMPLE
    .\validate-deployment.ps1 -Environment prod -SkipInfrastructure -SkipSecurity
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment,

    [Parameter(Mandatory = $false)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $false)]
    [string]$AppName,

    [switch]$SkipInfrastructure,
    [switch]$SkipSecurity
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Configuration
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath

# Validation results
$validationResults = @{
    Passed = 0
    Failed = 0
    Warnings = 0
}

function Write-Header {
    param([string]$Title)
    Write-Host "`n=========================================" -ForegroundColor Cyan
    Write-Host " $Title" -ForegroundColor Cyan
    Write-Host "=========================================" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
    $script:validationResults.Passed++
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
    $script:validationResults.Failed++
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
    $script:validationResults.Warnings++
}

function Test-AzureCli {
    try {
        $version = az --version 2>$null | Select-Object -First 1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Azure CLI is installed: $version"
            return $true
        }
    }
    catch {
        Write-Error "Azure CLI is not installed or not in PATH"
        return $false
    }
}

function Test-AzureLogin {
    try {
        $account = az account show --query 'name' -o tsv 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Azure CLI is logged in: $account"
            return $true
        }
    }
    catch {
        Write-Error "Azure CLI is not logged in"
        return $false
    }
}

function Test-ResourceGroup {
    param([string]$ResourceGroupName)

    try {
        $rg = az group show --name $ResourceGroupName --query 'name' -o tsv 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Resource group exists: $rg"
            return $true
        }
    }
    catch {
        Write-Error "Resource group does not exist: $ResourceGroupName"
        return $false
    }
}

function Test-AppService {
    param([string]$ResourceGroupName, [string]$AppName)

    try {
        $app = az webapp show --resource-group $ResourceGroupName --name $AppName --query 'name' -o tsv 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "App Service exists: $app"

            # Check app service plan
            $plan = az webapp show --resource-group $ResourceGroupName --name $AppName --query 'appServicePlanId' -o tsv 2>$null
            if ($plan) {
                Write-Success "App Service Plan is configured"
            }

            return $true
        }
    }
    catch {
        Write-Error "App Service does not exist: $AppName"
        return $false
    }
}

function Test-Database {
    param([string]$ResourceGroupName, [string]$ServerName, [string]$DatabaseName)

    try {
        $db = az sql db show --resource-group $ResourceGroupName --server $ServerName --name $DatabaseName --query 'name' -o tsv 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "SQL Database exists: $db"
            return $true
        }
    }
    catch {
        Write-Error "SQL Database does not exist: $DatabaseName"
        return $false
    }
}

function Test-Cache {
    param([string]$ResourceGroupName, [string]$CacheName)

    try {
        $cache = az redis show --resource-group $ResourceGroupName --name $CacheName --query 'name' -o tsv 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Redis Cache exists: $cache"
            return $true
        }
    }
    catch {
        Write-Error "Redis Cache does not exist: $CacheName"
        return $false
    }
}

function Test-AppConfiguration {
    param([string]$ResourceGroupName, [string]$ConfigName)

    try {
        $config = az appconfig show --resource-group $ResourceGroupName --name $ConfigName --query 'name' -o tsv 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "App Configuration exists: $config"
            return $true
        }
    }
    catch {
        Write-Warning "App Configuration does not exist: $ConfigName (optional for basic deployment)"
        return $true # Not critical
    }
}

function Test-KeyVault {
    param([string]$ResourceGroupName, [string]$VaultName)

    try {
        $vault = az keyvault show --resource-group $ResourceGroupName --name $VaultName --query 'name' -o tsv 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Key Vault exists: $vault"
            return $true
        }
    }
    catch {
        Write-Warning "Key Vault does not exist: $VaultName (optional for basic deployment)"
        return $true # Not critical
    }
}

function Test-ApplicationInsights {
    param([string]$ResourceGroupName, [string]$InsightsName)

    try {
        $insights = az monitor app-insights component show --resource-group $ResourceGroupName --app $InsightsName --query 'name' -o tsv 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Application Insights exists: $insights"
            return $true
        }
    }
    catch {
        Write-Warning "Application Insights does not exist: $InsightsName (optional for basic deployment)"
        return $true # Not critical
    }
}

function Test-DockerImage {
    param([string]$ImageName = "notificationservice")

    try {
        $images = docker images $ImageName --format "{{.Repository}}:{{.Tag}}"
        if ($images) {
            Write-Success "Docker image exists: $images"
            return $true
        }
        else {
            Write-Error "Docker image not found: $ImageName"
            return $false
        }
    }
    catch {
        Write-Error "Docker is not installed or not running"
        return $false
    }
}

function Test-ConfigurationFiles {
    $configFiles = @(
        "src/NotificationService/appsettings.json",
        "src/NotificationService/appsettings.$Environment.json",
        "docker-compose.yml",
        "infra/main.bicep"
    )

    $allValid = $true

    foreach ($file in $configFiles) {
        $filePath = Join-Path $rootPath $file
        if (Test-Path $filePath) {
            Write-Success "Configuration file exists: $file"
        }
        else {
            Write-Error "Configuration file missing: $file"
            $allValid = $false
        }
    }

    return $allValid
}

function Test-EnvironmentVariables {
    $requiredVars = @(
        "ConnectionStrings__NotificationDb",
        "ConnectionStrings__Redis",
        "Jwt__Secret",
        "Jwt__Issuer",
        "Jwt__Audience"
    )

    $allValid = $true

    foreach ($var in $requiredVars) {
        $value = [Environment]::GetEnvironmentVariable($var)
        if ($value) {
            Write-Success "Environment variable set: $var"
        }
        else {
            Write-Error "Environment variable not set: $var"
            $allValid = $false
        }
    }

    return $allValid
}

function Test-SecurityConfiguration {
    Write-Header "Security Validation"

    # Check for hardcoded secrets in code
    $secretPatterns = @(
        'password\s*=',
        'secret\s*=',
        'key\s*=',
        'token\s*='
    )

    $csFiles = Get-ChildItem -Path $rootPath -Filter "*.cs" -Recurse
    $hasHardcodedSecrets = $false

    foreach ($file in $csFiles) {
        $content = Get-Content $file.FullName -Raw
        foreach ($pattern in $secretPatterns) {
            if ($content -match $pattern -and $content -notmatch 'Environment\.GetEnvironmentVariable|Configuration\[".*"\]|appsettings') {
                Write-Error "Potential hardcoded secret in $($file.Name): $pattern"
                $hasHardcodedSecrets = $true
            }
        }
    }

    if (-not $hasHardcodedSecrets) {
        Write-Success "No hardcoded secrets found in source code"
    }

    return -not $hasHardcodedSecrets
}

# Main validation logic
Write-Header "Notification Service Deployment Validation"
Write-Host "Environment: $Environment"
Write-Host "Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"

# Prerequisites
Write-Header "Prerequisites Check"
Test-ConfigurationFiles | Out-Null

# Infrastructure validation
if (-not $SkipInfrastructure) {
    Write-Header "Infrastructure Validation"

    if ($ResourceGroup -and $AppName) {
        $azureCliOk = Test-AzureCli
        if ($azureCliOk) {
            Test-AzureLogin | Out-Null

            Test-ResourceGroup $ResourceGroup | Out-Null

            if (Test-AppService $ResourceGroup $AppName) {
                # Test related resources
                $serverName = "$AppName-sql"
                $databaseName = "$AppName-db"
                $cacheName = "$AppName-redis"
                $configName = "$AppName-config"
                $vaultName = "$AppName-kv"
                $insightsName = "$AppName-insights"

                Test-Database $ResourceGroup $serverName $databaseName | Out-Null
                Test-Cache $ResourceGroup $cacheName | Out-Null
                Test-AppConfiguration $ResourceGroup $configName | Out-Null
                Test-KeyVault $ResourceGroup $vaultName | Out-Null
                Test-ApplicationInsights $ResourceGroup $insightsName | Out-Null
            }
        }
    }
    else {
        Write-Warning "ResourceGroup and AppName not provided, skipping Azure resource validation"
    }
}

# Docker validation
Write-Header "Docker Validation"
Test-DockerImage | Out-Null

# Environment validation
Write-Header "Environment Validation"
Test-EnvironmentVariables | Out-Null

# Security validation
if (-not $SkipSecurity) {
    Test-SecurityConfiguration | Out-Null
}

# Final results
Write-Header "Validation Summary"

$resultsColor = if ($validationResults.Failed -eq 0) {
    "Green"
} elseif ($validationResults.Failed -le 2) {
    "Yellow"
} else {
    "Red"
}

Write-Host "Results:" -ForegroundColor $resultsColor
Write-Host "  Passed: $($validationResults.Passed)" -ForegroundColor Green
Write-Host "  Failed: $($validationResults.Failed)" -ForegroundColor Red
Write-Host "  Warnings: $($validationResults.Warnings)" -ForegroundColor Yellow

if ($validationResults.Failed -eq 0) {
    Write-Host "`n🎉 Deployment validation PASSED! Ready for deployment." -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n❌ Deployment validation FAILED! Please fix the issues above." -ForegroundColor Red
    exit 1
}
