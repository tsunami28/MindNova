// ---------------------------------------------------------------------------
// MindNova - Azure Infrastructure
// Provisions: App Service Plan, WebApp, Azure SQL (serverless), Key Vault
// Uses Azure Verified Modules (AVM) from the Bicep public registry (ADR 0009)
// ---------------------------------------------------------------------------

targetScope = 'resourceGroup'

// ---------------------------------------------------------------------------
// Parameters
// ---------------------------------------------------------------------------

@description('Environment identifier (dev or prd)')
@allowed(['dev', 'prd'])
param environmentName string

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Base name for resource naming')
param appName string = 'mindnova'

@description('App Service Plan SKU name')
param appServicePlanSkuName string

@description('App Service Plan SKU capacity (number of instances)')
param appServicePlanSkuCapacity int = 1

@description('SQL Database auto-pause delay in minutes (-1 to disable)')
param sqlAutoPauseDelay int

@description('SQL Database minimum vCore capacity')
param sqlMinCapacity string = '0.5'

@description('SQL Database maximum size in bytes (2 GB default)')
param sqlMaxSizeBytes int = 2147483648

@description('Object ID of the Entra ID group or user for SQL admin')
param sqlAdminObjectId string

@description('Display name of the Entra ID SQL admin')
param sqlAdminLogin string

@description('Entra ID tenant ID')
param tenantId string = subscription().tenantId

// ---------------------------------------------------------------------------
// Variables
// ---------------------------------------------------------------------------

var resourceToken = uniqueString(resourceGroup().id, appName, environmentName)
var tags = {
  application: appName
  environment: environmentName
  managedBy: 'bicep'
}

// ---------------------------------------------------------------------------
// App Service Plan (AVM)
// ---------------------------------------------------------------------------

module appServicePlan 'br/public:avm/res/web/serverfarm:0.7.0' = {
  name: 'appServicePlan'
  params: {
    name: 'asp-${appName}-${environmentName}-${resourceToken}'
    location: location
    tags: tags
    kind: 'linux'
    reserved: true
    skuName: appServicePlanSkuName
    skuCapacity: appServicePlanSkuCapacity
  }
}

// ---------------------------------------------------------------------------
// Key Vault (AVM)
// ---------------------------------------------------------------------------

module keyVault 'br/public:avm/res/key-vault/vault:0.13.0' = {
  name: 'keyVault'
  params: {
    name: 'kv-${appName}-${environmentName}-${resourceToken}'
    location: location
    tags: tags
    enableRbacAuthorization: true
    enablePurgeProtection: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    sku: 'standard'
  }
}

// ---------------------------------------------------------------------------
// Azure SQL Server + Database (AVM)
// Per ADR 0008: serverless compute tier
// ---------------------------------------------------------------------------

module sqlServer 'br/public:avm/res/sql/server:0.21.0' = {
  name: 'sqlServer'
  params: {
    name: 'sql-${appName}-${environmentName}-${resourceToken}'
    location: location
    tags: tags
    administrators: {
      azureADOnlyAuthentication: true
      login: sqlAdminLogin
      principalType: 'Group'
      sid: sqlAdminObjectId
      tenantId: tenantId
    }
    minimalTlsVersion: '1.2'
    databases: [
      {
        name: 'sqldb-${appName}-${environmentName}'
        availabilityZone: -1
        sku: {
          name: 'GP_S_Gen5'
          tier: 'GeneralPurpose'
          family: 'Gen5'
          capacity: 1
        }
        autoPauseDelay: sqlAutoPauseDelay
        minCapacity: sqlMinCapacity
        maxSizeBytes: sqlMaxSizeBytes
        collation: 'SQL_Latin1_General_CP1_CI_AS'
        requestedBackupStorageRedundancy: 'Local'
      }
    ]
    firewallRules: [
      {
        name: 'AllowAllAzureServices'
        startIpAddress: '0.0.0.0'
        endIpAddress: '0.0.0.0'
      }
    ]
  }
}

// ---------------------------------------------------------------------------
// WebApp (AVM)
// System-assigned managed identity for SQL auth (AC-7)
// ---------------------------------------------------------------------------

module webApp 'br/public:avm/res/web/site:0.23.0' = {
  name: 'webApp'
  params: {
    name: 'app-${appName}-${environmentName}-${resourceToken}'
    location: location
    tags: tags
    kind: 'app,linux'
    serverFarmResourceId: appServicePlan.outputs.resourceId
    managedIdentities: {
      systemAssigned: true
    }
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      alwaysOn: appServicePlanSkuName != 'F1'
      appSettings: [
        {
          name: 'AZURE_SQL_CONNECTION_STRING'
          value: 'Server=tcp:${sqlServer.outputs.fullyQualifiedDomainName},1433;Database=sqldb-${appName}-${environmentName};Authentication=Active Directory Default;TrustServerCertificate=false;Encrypt=true;'
        }
        {
          name: 'AZURE_KEY_VAULT_ENDPOINT'
          value: keyVault.outputs.uri
        }
        {
          name: 'ConnectionStrings__MindNova'
          value: 'Server=tcp:${sqlServer.outputs.fullyQualifiedDomainName},1433;Database=sqldb-${appName}-${environmentName};Authentication=Active Directory Default;TrustServerCertificate=false;Encrypt=true;'
        }
      ]
    }
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------

@description('The default hostname of the deployed WebApp')
output webAppHostName string = webApp.outputs.defaultHostname

@description('The resource ID of the deployed WebApp')
output webAppResourceId string = webApp.outputs.resourceId

@description('The fully qualified domain name of the SQL Server')
output sqlServerFqdn string = sqlServer.outputs.fullyQualifiedDomainName

@description('The name of the Key Vault')
output keyVaultName string = keyVault.outputs.name

@description('The URI of the Key Vault')
output keyVaultUri string = keyVault.outputs.uri
