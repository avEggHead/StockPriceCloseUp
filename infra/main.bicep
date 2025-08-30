// ===== Parameters =====
@allowed([
  'eastus'
  'eastus2'
  'centralus'
  'westus2'
  'westus3'
])
param location string = 'eastus'

param appName string
param sqlAdmin string
@secure()
param sqlPassword string

// Finnhub API key will be stored in Key Vault as a secret
@secure()
param finnhubApiKey string

// ===== App Service Plan (Free F1, Windows) =====
// (Free tier is available on Windows. Staying Windows keeps things simple for MVC.)
resource plan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: '${appName}-plan'
  location: location
  sku: {
    name: 'F1'
    tier: 'Free'
    size: 'F1'
    capacity: 1
  }
  properties: {
    reserved: false // Windows
  }
}

// ===== Web App with System-Assigned Managed Identity =====
resource webApp 'Microsoft.Web/sites@2022-09-01' = {
  name: appName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: false // On Free F1 you don’t get custom domain/SSL. (Platform-managed cert requires higher tiers.)
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      appSettings: [
        // Pass Key Vault name so your app can resolve it
        { name: 'KeyVaultName', value: '${appName}-kv' }

        // Optional: ASPNETCORE_ENVIRONMENT=Production at runtime
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
      ]
      connectionStrings: [
        // We’ll fill this dynamically after SQL is created (below) using format string
      ]
    }
  }
  kind: 'app'
}

// ===== SQL Server (logical) =====
resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: '${appName}-sql'
  location: location
  properties: {
    administratorLogin: sqlAdmin
    administratorLoginPassword: sqlPassword
    publicNetworkAccess: 'Enabled'
  }
}

// Allow Azure services to access the server (so the Web App can connect)
resource allowAzureIPs 'Microsoft.Sql/servers/firewallRules@2022-05-01-preview' = {
  name: 'AllowAllWindowsAzureIps'
  parent: sqlServer
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ===== SQL Database (Serverless with Auto-Pause) =====
// Max 1 vCore to keep cost tiny; autoPauseDelay 60 minutes (minimum)
resource sqlDb 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  name: 'appdb'
  parent: sqlServer
  sku: {
    name: 'GP_S_Gen5_1' // GeneralPurpose Serverless, Gen5, max 1 vCore
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  properties: {
    autoPauseDelay: 60 // minutes; pauses compute to $0 when idle; you still pay storage
    minCapacity: 0.5   // (Optional) minimum vCores; 0.5 keeps ramp-up cheap
    readScale: 'Disabled'
    zoneRedundant: false
  }
}

// ===== Key Vault (Standard) with RBAC Authorization =====
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${appName}-kv'
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      name: 'standard'
      family: 'A'
    }
    // Use RBAC for data-plane access instead of legacy accessPolicies
    enableRbacAuthorization: true
    softDeleteRetentionInDays: 7
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    publicNetworkAccess: 'Enabled'
  }
}

// Secret for Finnhub
resource finnhubSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'Finnhub-ApiKey'
  parent: keyVault
  properties: {
    value: finnhubApiKey
  }
}

// ===== RBAC: grant Web App MI permission to read secrets =====
resource kvRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, webApp.name, 'kv-secrets-user')
  scope: keyVault
  properties: {
    principalId: webApp.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6') // Key Vault Secrets User
    principalType: 'ServicePrincipal'
  }
}

// ===== Wire SQL connection string into the Web App (as a connection string) =====
resource webAppConfig 'Microsoft.Web/sites/config@2022-09-01' = {
  name: '${webApp.name}/connectionstrings'
  properties: {
    DefaultConnection: {
      value: 'Server=tcp=${sqlServer.name}.database.windows.net,1433;Initial Catalog=${sqlDb.name};Persist Security Info=False;User ID=${sqlAdmin};Password=${sqlPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
      type: 'SQLAzure'
    }
  }
  dependsOn: [
    sqlDb
    allowAzureIPs
  ]
}

// ===== Outputs =====
output webAppName string = webApp.name
output webAppUrl string = 'https://${webApp.name}.azurewebsites.net'
output keyVaultName string = keyVault.name
output sqlServerName string = sqlServer.name
output sqlDbName string = sqlDb.name
