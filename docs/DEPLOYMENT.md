# Notification Service - Deployment Guide

## 🚀 **Azure Deployment Overview**

This guide covers the complete deployment process for the Notification Service to Azure, including infrastructure provisioning, application deployment, and post-deployment validation.

## 📋 **Prerequisites**

### **Azure Requirements**
- Azure subscription with Contributor role
- Azure CLI installed (`az --version`)
- PowerShell 7+ for deployment scripts
- Docker Desktop (for local testing)

### **Local Development Setup**
```bash
# Clone the repository
git clone <repository-url>
cd ECommercePlatform

# Ensure .NET 9.0 SDK is installed
dotnet --version

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test
```

## 🏗️ **Infrastructure Deployment**

### **1. Environment Setup**

Create parameter files for different environments:

**`infra/parameters.dev.json`**
```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "environmentName": {
      "value": "dev"
    },
    "sqlAdministratorPassword": {
      "value": "YourStrong!Passw0rd123"
    },
    "redisCapacity": {
      "value": 0
    },
    "appServicePlanSku": {
      "value": "F1"
    }
  }
}
```

**`infra/parameters.staging.json`**
```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "environmentName": {
      "value": "staging"
    },
    "sqlAdministratorPassword": {
      "value": "YourStrong!Passw0rd456"
    },
    "redisCapacity": {
      "value": 1
    },
    "appServicePlanSku": {
      "value": "B1"
    }
  }
}
```

**`infra/parameters.prod.json`**
```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "environmentName": {
      "value": "prod"
    },
    "sqlAdministratorPassword": {
      "value": "YourStrong!Passw0rd789"
    },
    "redisCapacity": {
      "value": 2
    },
    "appServicePlanSku": {
      "value": "B2"
    }
  }
}
```

### **2. Deploy Infrastructure**

#### **Development Environment**
```bash
# Login to Azure
az login

# Set subscription
az account set --subscription "your-subscription-id"

# Create resource group
az group create --name notification-dev-rg --location eastus

# Deploy infrastructure
az deployment group create \
  --resource-group notification-dev-rg \
  --template-file infra/main.bicep \
  --parameters infra/parameters.dev.json
```

#### **Staging Environment**
```bash
# Create resource group
az group create --name notification-staging-rg --location eastus2

# Deploy infrastructure
az deployment group create \
  --resource-group notification-staging-rg \
  --template-file infra/main.bicep \
  --parameters infra/parameters.staging.json
```

#### **Production Environment**
```bash
# Create resource group
az group create --name notification-prod-rg --location westus2

# Deploy infrastructure
az deployment group create \
  --resource-group notification-prod-rg \
  --template-file infra/main.bicep \
  --parameters infra/parameters.prod.json
```

### **3. Configure Azure Resources**

#### **Azure App Configuration Setup**
```bash
# Get App Configuration connection string
APP_CONFIG_CONNECTION=$(az appconfig show \
  --resource-group notification-dev-rg \
  --name notification-dev-config \
  --query primaryConnectionString -o tsv)

# Set configuration values
az appconfig set \
  --connection-string $APP_CONFIG_CONNECTION \
  --key "EmailNotifications" \
  --value "true" \
  --label "NotificationService"

az appconfig set \
  --connection-string $APP_CONFIG_CONNECTION \
  --key "SmsNotifications" \
  --value "true" \
  --label "NotificationService"

az appconfig set \
  --connection-string $APP_CONFIG_CONNECTION \
  --key "PushNotifications" \
  --value "false" \
  --label "NotificationService"
```

#### **Key Vault Secrets Setup**
```bash
# Get Key Vault name
KEYVAULT_NAME=$(az keyvault show \
  --resource-group notification-dev-rg \
  --name notification-dev-kv \
  --query name -o tsv)

# Set secrets
az keyvault secret set \
  --vault-name $KEYVAULT_NAME \
  --name "JwtSecret" \
  --value "your-super-secret-jwt-key-for-production-only-64-chars-minimum"

az keyvault secret set \
  --vault-name $KEYVAULT_NAME \
  --name "EmailSmtpPassword" \
  --value "your-email-smtp-password"

az keyvault secret set \
  --vault-name $KEYVAULT_NAME \
  --name "SmsApiKey" \
  --value "your-sms-api-key"
```

## 🐳 **Application Deployment**

### **1. Build and Publish**

```bash
# Build for production
dotnet publish src/NotificationService/NotificationService.csproj \
  -c Release \
  -o ./publish \
  --runtime linux-x64 \
  --self-contained false

# Create Docker image
docker build -t notification-service:latest .

# Tag for Azure Container Registry
ACR_NAME=$(az acr show \
  --resource-group notification-dev-rg \
  --name notificationdevacr \
  --query name -o tsv)

docker tag notification-service:latest $ACR_NAME.azurecr.io/notification-service:latest

# Push to Azure Container Registry
az acr login --name $ACR_NAME
docker push $ACR_NAME.azurecr.io/notification-service:latest
```

### **2. Deploy to Azure App Service**

#### **Using Azure CLI**
```bash
# Get App Service name
APP_NAME=$(az webapp show \
  --resource-group notification-dev-rg \
  --name notification-dev-app \
  --query name -o tsv)

# Deploy container
az webapp config container set \
  --resource-group notification-dev-rg \
  --name $APP_NAME \
  --docker-custom-image-name $ACR_NAME.azurecr.io/notification-service:latest \
  --docker-registry-server-url https://$ACR_NAME.azurecr.io \
  --docker-registry-server-user $(az acr credential show --name $ACR_NAME --query username -o tsv) \
  --docker-registry-server-password $(az acr credential show --name $ACR_NAME --query passwords[0].value -o tsv)

# Configure environment variables
az webapp config appsettings set \
  --resource-group notification-dev-rg \
  --name $APP_NAME \
  --settings \
    ASPNETCORE_ENVIRONMENT=Development \
    Jwt__Secret="@Microsoft.KeyVault(SecretUri=https://$KEYVAULT_NAME.vault.azure.net/secrets/JwtSecret/)" \
    Email__SmtpPassword="@Microsoft.KeyVault(SecretUri=https://$KEYVAULT_NAME.vault.azure.net/secrets/EmailSmtpPassword/)" \
    Sms__ApiKey="@Microsoft.KeyVault(SecretUri=https://$KEYVAULT_NAME.vault.azure.net/secrets/SmsApiKey/)"

# Enable managed identity
az webapp identity assign \
  --resource-group notification-dev-rg \
  --name $APP_NAME

# Get managed identity principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --resource-group notification-dev-rg \
  --name $APP_NAME \
  --query principalId -o tsv)

# Grant access to Key Vault
az keyvault set-policy \
  --name $KEYVAULT_NAME \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list

# Grant access to App Configuration
APP_CONFIG_ID=$(az appconfig show \
  --resource-group notification-dev-rg \
  --name notification-dev-config \
  --query id -o tsv)

az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "App Configuration Data Reader" \
  --scope $APP_CONFIG_ID
```

#### **Using GitHub Actions**
Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Azure
on:
  push:
    branches: [ main ]
  workflow_dispatch:

env:
  AZURE_WEBAPP_NAME: notification-dev-app
  AZURE_RESOURCE_GROUP: notification-dev-rg
  ACR_NAME: notificationdevacr

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v3

    - name: Login to Azure
      uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}

    - name: Login to ACR
      uses: azure/docker-login@v1
      with:
        login-server: ${{ env.ACR_NAME }}.azurecr.io
        username: ${{ secrets.ACR_USERNAME }}
        password: ${{ secrets.ACR_PASSWORD }}

    - name: Build and push Docker image
      run: |
        docker build -t ${{ env.ACR_NAME }}.azurecr.io/notification-service:${{ github.sha }} .
        docker push ${{ env.ACR_NAME }}.azurecr.io/notification-service:${{ github.sha }}

    - name: Deploy to Azure Web App
      uses: azure/webapps-deploy@v2
      with:
        app-name: ${{ env.AZURE_WEBAPP_NAME }}
        images: ${{ env.ACR_NAME }}.azurecr.io/notification-service:${{ github.sha }}
```

## 🗄️ **Database Setup**

### **1. Run Migrations**

```bash
# Get connection string (for local development)
CONNECTION_STRING=$(az sql db show-connection-string \
  --server notification-dev-sql \
  --database notification-dev-db \
  --client ado.net \
  --output tsv)

# Update connection string with credentials
CONNECTION_STRING="${CONNECTION_STRING/<username>/sqladmin}"
CONNECTION_STRING="${CONNECTION_STRING/<password>/YourStrong!Passw0rd123}"

# Run migrations locally
cd src/NotificationService
dotnet ef database update --connection "$CONNECTION_STRING"
```

### **2. Initialize Database**

```powershell
# Run database initialization script
./scripts/init-db.ps1 -Environment dev -ResourceGroup notification-dev-rg
```

## 🔍 **Post-Deployment Validation**

### **1. Run Deployment Validation Script**

```powershell
# Run comprehensive validation
./scripts/validate-deployment.ps1 `
  -Environment dev `
  -ResourceGroup notification-dev-rg `
  -AppName notification-dev-app
```

### **2. Manual Validation Steps**

#### **Health Check**
```bash
# Get app URL
APP_URL=$(az webapp show \
  --resource-group notification-dev-rg \
  --name notification-dev-app \
  --query defaultHostName -o tsv)

# Check health endpoint
curl -f https://$APP_URL/health
```

#### **API Testing**
```bash
# Test authentication
TOKEN=$(curl -X POST https://$APP_URL/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' \
  | jq -r '.token')

# Test notification sending
curl -X POST https://$APP_URL/api/notifications \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "type": "Email",
    "to": "test@example.com",
    "subject": "Deployment Test",
    "body": "Notification service is successfully deployed!"
  }'
```

#### **Real-time Dashboard**
```bash
# Test SignalR connection
curl -I https://$APP_URL/notificationHub/negotiate
```

### **3. Performance Testing**

```bash
# Run load tests against deployed service
cd LoadTests
dotnet run -- \
  --base-url https://$APP_URL \
  --duration 60 \
  --rate 50 \
  --concurrency 20
```

## 🔄 **Blue-Green Deployment**

### **Staging Slot Deployment**

```bash
# Create staging slot
az webapp deployment slot create \
  --resource-group notification-prod-rg \
  --name notification-prod-app \
  --slot staging

# Deploy to staging slot
az webapp config container set \
  --resource-group notification-prod-rg \
  --name notification-prod-app \
  --slot staging \
  --docker-custom-image-name $ACR_NAME.azurecr.io/notification-service:new-version

# Run database migrations on staging
az webapp config appsettings set \
  --resource-group notification-prod-rg \
  --name notification-prod-app \
  --slot staging \
  --settings RUN_MIGRATIONS=true

# Test staging deployment
curl -f https://notification-prod-app-staging.azurewebsites.net/health

# Swap slots
az webapp deployment slot swap \
  --resource-group notification-prod-rg \
  --name notification-prod-app \
  --slot staging
```

## 📊 **Monitoring Setup**

### **Application Insights**

```bash
# Enable Application Insights
az monitor app-insights component connect-webapp \
  --resource-group notification-dev-rg \
  --app notification-dev-app \
  --app-insights notification-dev-insights
```

### **Alert Configuration**

```bash
# Create CPU usage alert
az monitor metrics alert create \
  --name "High CPU Usage" \
  --resource-group notification-dev-rg \
  --scopes /subscriptions/.../resourceGroups/notification-dev-rg/providers/Microsoft.Web/sites/notification-dev-app \
  --condition "avg Percentage CPU > 80" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action-group /subscriptions/.../resourceGroups/notification-dev-rg/providers/microsoft.insights/actionGroups/notification-dev-ag
```

### **Log Analytics**

```bash
# Configure diagnostic settings
az monitor diagnostic-settings create \
  --name "App Service Logs" \
  --resource /subscriptions/.../resourceGroups/notification-dev-rg/providers/Microsoft.Web/sites/notification-dev-app \
  --logs '[{"category": "AppServiceHTTPLogs", "enabled": true}]' \
  --metrics '[{"category": "AllMetrics", "enabled": true}]' \
  --workspace /subscriptions/.../resourceGroups/notification-dev-rg/providers/Microsoft.OperationalInsights/workspaces/notification-dev-workspace
```

## 🔧 **Maintenance Tasks**

### **Certificate Management**

```bash
# Add custom domain and SSL certificate
az webapp config hostname set \
  --resource-group notification-prod-rg \
  --webapp-name notification-prod-app \
  --hostname www.yourdomain.com

# Upload SSL certificate
az webapp config ssl upload \
  --resource-group notification-prod-rg \
  --name notification-prod-app \
  --certificate-file certificate.pfx \
  --certificate-password "certificate-password"

# Bind SSL certificate
az webapp config ssl bind \
  --resource-group notification-prod-rg \
  --name notification-prod-app \
  --certificate-thumbprint "certificate-thumbprint" \
  --ssl-type SNI
```

### **Backup Configuration**

```bash
# Configure database backup
az sql db backup-policy set \
  --resource-group notification-prod-rg \
  --server notification-prod-sql \
  --database notification-prod-db \
  --enable true \
  --retention-days 30

# Configure Redis backup (if premium tier)
az redis update \
  --resource-group notification-prod-rg \
  --name notification-prod-redis \
  --enable-non-ssl-port false \
  --minimum-tls-version "1.2" \
  --enable-rdb-backup true \
  --rdb-backup-frequency 60 \
  --rdb-backup-max-snapshot-count 1
```

## 🚨 **Troubleshooting**

### **Common Issues**

#### **Application Won't Start**
```bash
# Check application logs
az webapp log download \
  --resource-group notification-dev-rg \
  --name notification-dev-app \
  --log-file app_logs.zip

# Check configuration
az webapp config appsettings list \
  --resource-group notification-dev-rg \
  --name notification-dev-app
```

#### **Database Connection Issues**
```bash
# Test database connectivity
az sql db show-connection-string \
  --server notification-dev-sql \
  --database notification-dev-db \
  --client ado.net

# Check firewall rules
az sql server firewall-rule list \
  --resource-group notification-dev-rg \
  --server notification-dev-sql
```

#### **Container Issues**
```bash
# Check container logs
az webapp log tail \
  --resource-group notification-dev-rg \
  --name notification-dev-app \
  --provider docker
```

### **Rollback Procedures**

```bash
# Rollback to previous deployment
az webapp config container set \
  --resource-group notification-prod-rg \
  --name notification-prod-app \
  --docker-custom-image-name $ACR_NAME.azurecr.io/notification-service:previous-version

# Rollback database migration
cd src/NotificationService
dotnet ef migrations remove --connection "$CONNECTION_STRING"
```

## 📈 **Scaling**

### **Horizontal Scaling**
```bash
# Scale out App Service Plan
az appservice plan update \
  --resource-group notification-prod-rg \
  --name notification-prod-plan \
  --number-of-workers 3
```

### **Database Scaling**
```bash
# Scale up database
az sql db update \
  --resource-group notification-prod-rg \
  --server notification-prod-sql \
  --name notification-prod-db \
  --service-objective S2
```

### **Redis Scaling**
```bash
# Scale up Redis cache
az redis update \
  --resource-group notification-prod-rg \
  --name notification-prod-redis \
  --sku Premium \
  --vm-size P2
```

## ✅ **Validation Checklist**

- [ ] Infrastructure deployed successfully
- [ ] Database created and migrations applied
- [ ] Application deployed and healthy
- [ ] Authentication working
- [ ] Notifications can be sent
- [ ] Real-time dashboard accessible
- [ ] Monitoring and logging configured
- [ ] SSL certificate configured
- [ ] Backup policies in place
- [ ] Performance tests pass
- [ ] Security scan completed

## 📞 **Support**

For deployment issues:
1. Check application logs in Azure Portal
2. Review deployment validation script output
3. Verify all prerequisites are met
4. Check Azure resource health status
5. Review Azure Advisor recommendations

**Need Help?** Check the [troubleshooting guide](./TROUBLESHOOTING.md) or create an issue in the repository.
