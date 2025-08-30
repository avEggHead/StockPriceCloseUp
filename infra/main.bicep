﻿// ========= Parameters =========
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

@secure()
param finnhubApiKey string

// ========= App Service Plan (Free - Windows) =========
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
    reserved: false // Windows (not Linux)
  }
}

// ========= Web App with Managed Identity =========
resource webApp 'Microsoft.Web/sites@2022-09-01' = {
  name: appName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      appSettings: [
        // App reads KV name from config, then uses DefaultAzureCredential()
        { name: 'KeyVaultName', value: '${appName}-kv' }
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
      ]
    }
  }
  kind: 'app'
}

// ========= SQL Server + DB (Serverless, Auto-pause) =========
resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: '${appName}-sql'
  location: location
  properties: {
    administratorLogin: sqlAdmin
    administratorLoginPassword: sqlPassword
    publicNetworkAccess: 'Enabled'
  }
}

resource allowAzureIPs 'Microsoft.Sql/servers/firewallRules@2022-05-01-preview' = {
  name: 'AllowAllWindowsAzureIps'
  parent: sqlServer
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDb 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  name: 'appdb'
  parent: sqlServer
  sku: {
    // General Purpose Serverless, Gen5, 1 vCore max (cheap demo)
    name: 'GP_S_Gen5_1'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  properties: {
    // Auto-pause after 60 minutes of inactivity => compute = $0 while paused
    autoPauseDelay: 60
    minCapacity: 0.5
    readScale: 'Disabled'
    zoneRedundant: false
  }
}

// ========= Key Vault (Standard, RBAC) =========
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${appName}-kv'
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      name: 'standard'
      family: 'A'
    },
    enableRbacAuthorization: true
    softDeleteRetentionInDays: 7
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    publicNetworkAccess: 'Enabled'
  }
}

resource finnhubSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'Finnhub-ApiKey'
  parent: keyVault
  properties: {
    value: finnhubApiKey
  }
}

// Grant Web App MI permission to read secrets in KV
resource kvRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, webApp.name, 'kv-secrets-user')
  scope: keyVault
  properties: {
    principalId: webApp.identity.principalId
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '4633458b-17de-408a-b874-0445c86b69e6' // Key Vault Secrets User
    )
    principalType: 'ServicePrincipal'
  }
}

// ========= Web App connection string (parent syntax) =========
// (Linter prefers this over "${webApp.name}/connectionstrings")
resource webAppConnectionStrings 'Microsoft.Web/sites/config@2022-09-01' = {
  name: 'connectionstrings'
  parent: webApp
  properties: {
    DefaultConnection: {
      // NOTE: The linter warns to avoid hardcoding "database.windows.net"
      // For public Azure this is fine for a demo. You can ignore that warning.
      value: 'Server=tcp=${sqlServer.name}.database.windows.net,1433;Initial Catalog=${sqlDb.name};Persist Security Info=False;User ID=${sqlAdmin};Password=${sqlPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
      type: 'SQLAzure'
    }
  }
}

// ========= Outputs =========
output webAppName string = webApp.name
output webAppUrl string = 'https://${webApp.name}.azurewebsites.net'
output keyVaultName string = keyVault.name
output sqlServerName string = sqlServer.name
output sqlDbName string = sqlDb.name
