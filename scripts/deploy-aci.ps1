# Azure Container Instances Deployment Script
param(
    [string]$ResourceGroupName = "notification-dev-rg",
    [string]$Location = "eastus",
    [string]$EnvironmentName = "dev"
)

Write-Host "🚀 Deploying Notification Service to Azure Container Instances" -ForegroundColor Green

# Build Docker image
Write-Host "📦 Building Docker image..." -ForegroundColor Yellow
docker build -t notificationservice:latest .

# Tag for Docker Hub (you'll need to login and push)
$dockerHubUsername = Read-Host "Enter your Docker Hub username (or press Enter to use local registry)"
if ($dockerHubUsername) {
    $imageName = "$dockerHubUsername/notificationservice:latest"
    Write-Host "🏷️ Tagging image for Docker Hub..." -ForegroundColor Yellow
    docker tag notificationservice:latest $imageName

    Write-Host "🔐 Please login to Docker Hub:" -ForegroundColor Yellow
    Write-Host "docker login" -ForegroundColor White
    $loginConfirm = Read-Host "Press Enter after you've logged in to Docker Hub"

    Write-Host "📤 Pushing to Docker Hub..." -ForegroundColor Yellow
    docker push $imageName
} else {
    $imageName = "notificationservice:latest"
    Write-Host "⚠️ Using local image. ACI deployment may fail if image isn't accessible." -ForegroundColor Yellow
    Write-Host "💡 Consider pushing to Docker Hub or Azure Container Registry for cloud deployment." -ForegroundColor Yellow
}

# Deploy to ACI
Write-Host "☁️ Deploying to Azure Container Instances..." -ForegroundColor Yellow
az deployment group create `
    --resource-group $ResourceGroupName `
    --template-file infra/aci-deployment.bicep `
    --parameters environmentName=$EnvironmentName location=$Location containerImage=$imageName `
    --name "aci-deployment-$(Get-Date -Format 'yyyyMMddHHmmss')"

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Deployment successful!" -ForegroundColor Green

    # Get container information
    $containerInfo = az container show `
        --resource-group $ResourceGroupName `
        --name "notificationservice-$EnvironmentName-aci" `
        --query "{ip:ipAddress.ip, fqdn:ipAddress.fqdn}" -o json

    $containerObj = $containerInfo | ConvertFrom-Json

    Write-Host "🌐 Service URL: http://$($containerObj.fqdn):8080" -ForegroundColor Cyan
    Write-Host "🖥️ Container IP: $($containerObj.ip)" -ForegroundColor Cyan

    Write-Host "`n🧪 Test the deployment:" -ForegroundColor Yellow
    Write-Host "curl http://$($containerObj.fqdn):8080/health" -ForegroundColor White
    Write-Host "curl http://$($containerObj.fqdn):8080/swagger" -ForegroundColor White
} else {
    Write-Host "❌ Deployment failed. Check the error messages above." -ForegroundColor Red
}
