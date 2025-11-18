@description('The name of the environment (dev, staging, prod)')
param environmentName string = 'dev'

@description('Location for all resources.')
param location string = resourceGroup().location

@description('The name of the application')
param appName string = 'notificationservice'

@description('SQL Server administrator login')
param sqlAdministratorLogin string = 'sqladmin'

@description('SQL Server administrator password')
@secure()
param sqlAdministratorPassword string

@description('Redis cache capacity (0-6)')
param redisCapacity int = 0

@description('App Service Plan SKU')
param appServicePlanSku string = environmentName == 'prod' ? 'B1' : 'F1'

var resourceName = '${appName}-${environmentName}'
var sqlServerName = '${resourceName}-sql'
var sqlDatabaseName = '${resourceName}-db'
var redisCacheName = '${resourceName}-redis'
var appServicePlanName = '${resourceName}-plan'
var appServiceName = '${resourceName}-app'
var appInsightsName = '${resourceName}-insights'
var appConfigName = '${resourceName}-config'
var keyVaultName = '${resourceName}-kv'

// Resource Group (if needed, can be deployed separately)
resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' existing

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2021-02-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: appServicePlanSku
    tier: appServicePlanSku == 'F1' ? 'Free' : 'Basic'
    size: appServicePlanSku
    family: appServicePlanSku == 'F1' ? 'F' : 'B'
    capacity: 1
  }
  kind: 'app'
}

// App Service
resource appService 'Microsoft.Web/sites@2021-02-01' = {
  name: appServiceName
  location: location
  kind: 'app'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      alwaysOn: true
      http20Enabled: true
      netFrameworkVersion: 'v8.0'
      use32BitWorkerProcess: false
      appSettings: [
        {
          name: 'DOCKER_REGISTRY_SERVER_URL'
          value: 'https://index.docker.io'
        }
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'false'
        }
        {
          name: 'DOCKER_ENABLE_CI'
          value: 'true'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environmentName == 'prod' ? 'Production' : 'Staging'
        }
        {
          name: 'ApplicationInsights__InstrumentationKey'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'ApplicationInsights__ConnectionString'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'AzureAppConfig__ConnectionString'
          value: appConfig.listKeys().primaryConnectionString
        }
        {
          name: 'ConnectionStrings__NotificationDb'
          value: 'Server=${sqlServer.properties.fullyQualifiedDomainName};Database=${sqlDatabaseName};Authentication=Active Directory Default;'
        }
        {
          name: 'ConnectionStrings__Redis'
          value: '${redisCache.properties.hostName}:${redisCache.properties.sslPort},password=${redisCache.listKeys().primaryKey},ssl=True,abortConnect=False'
        }
      ]
      connectionStrings: [
        {
          name: 'NotificationDb'
          connectionString: 'Server=${sqlServer.properties.fullyQualifiedDomainName};Database=${sqlDatabaseName};Authentication=Active Directory Default;'
          type: 'SQLAzure'
        }
      ]
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// Staging deployment slot for blue-green deployment
resource stagingSlot 'Microsoft.Web/sites/slots@2021-02-01' = {
  name: '${appServiceName}/staging'
  location: location
  kind: 'app'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      alwaysOn: true
      http20Enabled: true
      netFrameworkVersion: 'v8.0'
      use32BitWorkerProcess: false
      appSettings: [
        {
          name: 'DOCKER_REGISTRY_SERVER_URL'
          value: 'https://index.docker.io'
        }
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'false'
        }
        {
          name: 'DOCKER_ENABLE_CI'
          value: 'true'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Staging'
        }
        {
          name: 'ApplicationInsights__InstrumentationKey'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'ApplicationInsights__ConnectionString'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'AzureAppConfig__ConnectionString'
          value: appConfig.listKeys().primaryConnectionString
        }
        {
          name: 'ConnectionStrings__NotificationDb'
          value: 'Server=${sqlServer.properties.fullyQualifiedDomainName};Database=${sqlDatabaseName};Authentication=Active Directory Default;'
        }
        {
          name: 'ConnectionStrings__Redis'
          value: '${redisCache.properties.hostName}:${redisCache.properties.sslPort},password=${redisCache.listKeys().primaryKey},ssl=True,abortConnect=False'
        }
      ]
      connectionStrings: [
        {
          name: 'NotificationDb'
          connectionString: 'Server=${sqlServer.properties.fullyQualifiedDomainName};Database=${sqlDatabaseName};Authentication=Active Directory Default;'
          type: 'SQLAzure'
        }
      ]
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// SQL Server
resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// SQL Database
resource sqlDatabase 'Microsoft.Sql/servers/databases@2021-11-01' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2 GB
    zoneRedundant: false
  }
}

// SQL Server Firewall Rule (allow Azure services)
resource sqlFirewallRule 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Redis Cache
resource redisCache 'Microsoft.Cache/redis@2021-06-01' = {
  name: redisCacheName
  location: location
  properties: {
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: redisCapacity
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
  }
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

// Log Analytics Workspace (required for App Insights)
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2020-08-01' = {
  name: '${appInsightsName}-workspace'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// Azure App Configuration
resource appConfig 'Microsoft.AppConfiguration/configurationStores@2021-10-01-preview' = {
  name: appConfigName
  location: location
  sku: {
    name: 'standard'
  }
  properties: {
    enablePurgeProtection: false
    publicNetworkAccess: 'Enabled'
  }
}

// Key Vault (for secrets management)
resource keyVault 'Microsoft.KeyVault/vaults@2021-10-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enabledForDeployment: false
    enabledForTemplateDeployment: true
    enabledForDiskEncryption: false
    enablePurgeProtection: environmentName == 'prod'
    softDeleteRetentionInDays: environmentName == 'prod' ? 90 : 7
  }
}

// Role assignments for managed identity
resource appServiceKeyVaultAccess 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(appService.id, 'Key Vault Secrets User', subscription().subscriptionId)
  scope: keyVault
  properties: {
    principalId: appService.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69ae') // Key Vault Secrets User
    principalType: 'ServicePrincipal'
  }
}

resource appServiceSqlAccess 'Microsoft.Sql/servers/administrators@2021-11-01' = {
  parent: sqlServer
  name: 'ActiveDirectory'
  properties: {
    administratorType: 'ActiveDirectory'
    login: appService.name
    sid: appService.identity.principalId
    tenantId: subscription().tenantId
  }
}

// Outputs
output appServiceName string = appService.name
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output stagingSlotName string = stagingSlot.name
output stagingSlotUrl string = 'https://${stagingSlot.properties.defaultHostName}'
output sqlServerName string = sqlServer.name
output sqlDatabaseName string = sqlDatabase.name
output redisCacheName string = redisCache.name
output redisConnectionString string = 'redis.cache.windows.net:${redisCache.properties.sslPort},password=${redisCache.listKeys().primaryKey},ssl=True,abortConnect=False'
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey
output appConfigConnectionString string = appConfig.listKeys().primaryConnectionString
output keyVaultName string = keyVault.name
