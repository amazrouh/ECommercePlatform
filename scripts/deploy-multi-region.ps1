# Multi-Region Deployment Script - Try different regions with better quotas
$regions = @("eastus2", "centralus", "westus2", "northeurope", "uksouth", "australiaeast")
$resourceGroupBase = "notification-dev-rg"

foreach ($region in $regions) {
    $resourceGroupName = "$resourceGroupBase-$region"

    Write-Host "🌍 Trying deployment in $region..." -ForegroundColor Yellow

    # Create resource group
    az group create --name $resourceGroupName --location $region

    if ($LASTEXITCODE -eq 0) {
        # Try deployment
        az deployment group create `
            --resource-group $resourceGroupName `
            --template-file infra/main.bicep `
            --parameters environmentName=dev sqlAdministratorPassword=YourStrongPass123! appServicePlanSku=F1 enablePremiumFeatures=false location=$region

        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Success! Deployed to $region" -ForegroundColor Green
            Write-Host "🔗 Resource Group: $resourceGroupName" -ForegroundColor Cyan
            break
        } else {
            Write-Host "❌ Failed in $region, trying next region..." -ForegroundColor Red
            # Clean up failed deployment
            az group delete --name $resourceGroupName --yes --no-wait
        }
    }
}

Write-Host "🎯 If all regions failed, consider:" -ForegroundColor Yellow
Write-Host "  1. Azure Container Instances (ACI)" -ForegroundColor White
Write-Host "  2. Local Docker development" -ForegroundColor White
Write-Host "  3. Different cloud provider (AWS/GCP)" -ForegroundColor White




