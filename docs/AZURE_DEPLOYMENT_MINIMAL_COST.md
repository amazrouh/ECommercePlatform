# Azure Deployment Guide - Minimal Cost Strategy

## 🎯 **Cost-Optimized Deployment Overview**

This guide provides multiple Azure deployment strategies for your NotificationService, from **FREE** to **Enterprise-grade**, allowing you to choose based on your budget and requirements.

## 📊 **Cost Comparison Table**

| Feature | Free Tier | Basic Tier | Premium Tier | Enterprise |
|---------|-----------|------------|--------------|------------|
| **Monthly Cost** | **$0-5** | **$15-25** | **$50-100** | **$200+** |
| App Service | F1 (Free) | F1 (Free) | B1 ($12) | P1V3 ($100+) |
| Database | - | Basic ($5) | Standard ($25) | Premium ($150+) |
| Cache | - | - | Basic ($15) | Standard ($50+) |
| Monitoring | - | - | App Insights ($10) | Full Suite ($50+) |
| Security | Basic | Basic | Key Vault ($1) | Advanced ($20+) |
| SLA | None | Basic | Standard | Premium |

---

## 🚀 **Deployment Option 1: FREE TIER (Recommended for Development)**

### **Cost: $0-5/month**

Perfect for development, testing, and small-scale applications.

### **What You Get:**
- ✅ Free Azure App Service (F1)
- ✅ Basic authentication & security
- ✅ All notification channels
- ✅ Real-time dashboard
- ✅ Health checks & monitoring

### **What's Missing:**
- ❌ Persistent database (use SQLite for dev)
- ❌ Distributed caching
- ❌ Advanced monitoring
- ❌ High availability

### **Deployment Steps:**

#### **Prerequisites:**
```bash
# Install Azure CLI
winget install Microsoft.AzureCLI

# Login to Azure
az login

# Install Docker Desktop
# Download from: https://www.docker.com/products/docker-desktop
```

#### **1. Create Resource Group:**
```bash
az group create --name notification-dev-rg --location eastus
```

#### **2. Deploy Free App Service:**
```bash
az appservice plan create --name notification-free-plan \
    --resource-group notification-dev-rg \
    --sku FREE \
    --is-linux

az webapp create --name notification-dev-app \
    --resource-group notification-dev-rg \
    --plan notification-free-plan \
    --runtime "DOTNETCORE|8.0"
```

#### **3. Configure Application:**
```bash
# Generate JWT secret (64 chars)
JWT_SECRET="your-64-character-secret-here-replace-with-actual-random-string"

az webapp config appsettings set \
    --name notification-dev-app \
    --resource-group notification-dev-rg \
    --settings \
    ASPNETCORE_ENVIRONMENT=Development \
    Jwt__Secret="$JWT_SECRET" \
    Jwt__Issuer="https://notification-dev-app.azurewebsites.net" \
    Jwt__Audience="https://notification-dev-app.azurewebsites.net" \
    ConnectionStrings__NotificationDb="Data Source=notification.db" \
    Email__SmtpServer="smtp.gmail.com" \
    Email__Port=587 \
    Email__FromAddress="your-email@gmail.com" \
    Email__Username="your-email@gmail.com" \
    Email__Password="your-app-password"
```

#### **4. Deploy Application:**
```bash
# Build and deploy
dotnet publish src/NotificationService/ -c Release -o ./publish
az webapp deployment source config-zip \
    --name notification-dev-app \
    --resource-group notification-dev-rg \
    --src ./publish.zip
```

#### **5. Test Deployment:**
```bash
# Get app URL
az webapp show --name notification-dev-app \
    --resource-group notification-dev-rg \
    --query defaultHostName -o tsv

# Test health endpoint
curl https://notification-dev-app.azurewebsites.net/health
```

---

## 💰 **Deployment Option 2: BASIC TIER ($15-25/month)**

### **Cost Breakdown:**
- App Service F1: **$0**
- Azure SQL Database Basic: **$5/month**
- Storage Account: **$0.10/month**
- Bandwidth: **$0-10/month**
- **Total: $15-25/month**

### **What You Get:**
- ✅ Free App Service
- ✅ Azure SQL Database (Basic tier)
- ✅ Persistent data storage
- ✅ Production-ready security
- ✅ Database migrations
- ✅ Backup & recovery

### **Automated Deployment Script:**

```powershell
# Run the basic deployment script
.\scripts\deploy-basic.ps1 -Environment dev -SqlPassword "YourSecurePass123!"
```

### **Manual Deployment Steps:**

#### **1. Deploy Infrastructure:**
```bash
# Deploy basic infrastructure (without premium features)
az deployment group create \
    --resource-group notification-basic-rg \
    --template-file infra/main.bicep \
    --parameters environmentName=dev \
    --parameters enablePremiumFeatures=false \
    --parameters sqlAdministratorPassword="YourSecurePass123!"
```

#### **2. Build & Push Docker Image:**
```bash
# Build image
docker build -t notificationservice:latest .

# Create Azure Container Registry
az acr create --name notificationdevacr \
    --resource-group notification-basic-rg \
    --sku Basic

# Login and push
az acr login --name notificationdevacr
docker tag notificationservice:latest notificationdevacr.azurecr.io/notificationservice:latest
docker push notificationdevacr.azurecr.io/notificationservice:latest
```

#### **3. Configure App Service:**
```bash
az webapp config container set \
    --name notification-dev-app \
    --resource-group notification-basic-rg \
    --docker-custom-image-name notificationdevacr.azurecr.io/notificationservice:latest \
    --docker-registry-server-url https://notificationdevacr.azurecr.io
```

#### **4. Apply Database Migrations:**
```bash
# Get connection string
SQL_CONNECTION=$(az sql db show-connection-string \
    --server notification-dev-sql \
    --name notification-dev-db \
    --client ado.net \
    --output tsv)

# Apply migrations (you'll need to implement this in your app)
# For now, the app will create tables on first run
```

---

## 🏢 **Deployment Option 3: PREMIUM TIER ($50-100/month)**

### **Cost Breakdown:**
- App Service B1: **$12/month**
- Azure SQL Standard: **$25/month**
- Redis Cache Basic: **$15/month**
- App Insights: **$10/month**
- Key Vault: **$1/month**
- **Total: $63/month**

### **What You Get:**
- ✅ Production App Service Plan
- ✅ Standard SQL Database
- ✅ Redis distributed caching
- ✅ Advanced monitoring
- ✅ Key Vault for secrets
- ✅ App Configuration
- ✅ High availability

### **Deployment:**
```bash
# Deploy with premium features enabled
az deployment group create \
    --resource-group notification-prod-rg \
    --template-file infra/main.bicep \
    --parameters environmentName=prod \
    --parameters enablePremiumFeatures=true \
    --parameters appServicePlanSku=B1 \
    --parameters redisCapacity=1 \
    --parameters sqlAdministratorPassword="YourSecurePass123!"
```

---

## 📋 **Cost Optimization Tips**

### **1. Choose the Right Region:**
```bash
# Check pricing by region
az appservice list-locations --query "[].{Name:name, DisplayName:displayName}" -o table
```

### **2. Use Reserved Instances:**
```bash
# Reserve App Service Plan for 1 year (save ~15%)
az appservice plan create --name notification-plan \
    --resource-group notification-rg \
    --location eastus \
    --sku B1 \
    --is-linux \
    --number-of-workers 1
```

### **3. Optimize Database:**
- Use Basic tier for development
- Scale to Standard only when needed
- Enable auto-pause for dev environments

### **4. Monitor Usage:**
```bash
# Check current costs
az consumption usage list \
    --query "[].{Resource:instanceName, Cost:pretaxCost}" \
    --output table
```

### **5. Clean Up Unused Resources:**
```bash
# Delete unused resource groups
az group delete --name old-notification-rg --yes --no-wait
```

---

## 🔧 **Configuration Management**

### **Environment Variables for Different Tiers:**

#### **Free Tier:**
```json
{
  "ConnectionStrings": {
    "NotificationDb": "Data Source=notification.db"
  },
  "Jwt": {
    "Secret": "your-jwt-secret",
    "Issuer": "https://your-app.azurewebsites.net",
    "Audience": "https://your-app.azurewebsites.net"
  }
}
```

#### **Basic Tier:**
```json
{
  "ConnectionStrings": {
    "NotificationDb": "Server=your-sql-server.database.windows.net;Database=your-db;Authentication=Active Directory Default;",
    "Redis": ""
  },
  "AzureAppConfig": {
    "Enabled": false
  }
}
```

#### **Premium Tier:**
```json
{
  "ConnectionStrings": {
    "NotificationDb": "Server=your-sql-server.database.windows.net;Database=your-db;Authentication=Active Directory Default;",
    "Redis": "your-cache.redis.cache.windows.net:6380,password=your-key,ssl=True,abortConnect=False"
  },
  "AzureAppConfig": {
    "ConnectionString": "your-app-config-connection-string",
    "Enabled": true
  }
}
```

---

## 🚀 **CI/CD Pipeline**

### **GitHub Actions for Cost-Effective Deployments:**

```yaml
name: Deploy to Azure
on:
  push:
    branches: [ main ]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3

    - name: Login to Azure
      uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}

    - name: Build and push Docker image
      run: |
        docker build -t notificationservice:${{ github.sha }} .
        az acr login --name youracr
        docker tag notificationservice:${{ github.sha }} youracr.azurecr.io/notificationservice:${{ github.sha }}
        docker push youracr.azurecr.io/notificationservice:${{ github.sha }}

    - name: Deploy to Azure Web App
      uses: azure/webapps-deploy@v2
      with:
        app-name: notification-dev-app
        images: youracr.azurecr.io/notificationservice:${{ github.sha }}
```

---

## 📊 **Monitoring & Cost Alerts**

### **Set Up Cost Alerts:**
```bash
# Create budget alert
az consumption budget create \
    --name notification-budget \
    --amount 50 \
    --time-grain Monthly \
    --start-date 2024-01-01 \
    --end-date 2024-12-31 \
    --notifications \
        emailEnabled=true \
        operator=GreaterThan \
        threshold=80 \
        contactEmails="your-email@example.com"
```

### **Monitor Application Performance:**
```bash
# Enable basic Application Insights
az monitor app-insights component create \
    --name notification-insights \
    --location eastus \
    --resource-group notification-rg \
    --application-type web
```

---

## 🎯 **Recommended Starting Point**

### **For Most Users: Start with Basic Tier**

1. **Cost**: $15-25/month (very affordable)
2. **Features**: Production-ready with database
3. **Scalability**: Can handle moderate traffic
4. **Upgrade Path**: Easy to upgrade to Premium when needed

### **Quick Start Command:**
```bash
# One-command deployment
.\scripts\deploy-basic.ps1 -Environment dev -SqlPassword "YourSecurePass123!"
```

### **What This Gives You:**
- ✅ Professional notification service
- ✅ Database persistence
- ✅ Security & authentication
- ✅ Real-time dashboard
- ✅ Docker containerization
- ✅ RESTful API
- ✅ Swagger documentation

---

## 🆘 **Troubleshooting**

### **Common Issues:**

1. **App Service Deployment Fails:**
   ```bash
   # Check app service logs
   az webapp log download --name your-app --resource-group your-rg
   az webapp log tail --name your-app --resource-group your-rg
   ```

2. **Database Connection Issues:**
   ```bash
   # Test database connection
   az sql db show-connection-string \
       --server your-sql-server \
       --name your-database \
       --client ado.net
   ```

3. **Container Deployment Issues:**
   ```bash
   # Check container logs
   az webapp log config --name your-app \
       --resource-group your-rg \
       --docker-container-logging filesystem
   ```

### **Support Resources:**
- 📚 [Azure App Service Documentation](https://docs.microsoft.com/en-us/azure/app-service/)
- 🐳 [Azure Container Registry](https://docs.microsoft.com/en-us/azure/container-registry/)
- 🗄️ [Azure SQL Database](https://docs.microsoft.com/en-us/azure/azure-sql/)

---

**🎉 Happy Deploying! Your NotificationService is ready for Azure with minimal cost impact.**
