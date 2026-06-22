// ---------------------------------------------------------------------------
// MindNova - Prd environment parameters
// AC-6: Standard App Service Plan, serverless SQL without auto-pause
// ---------------------------------------------------------------------------

using 'main.bicep'

param environmentName = 'prd'

// Standard App Service Plan (S1)
param appServicePlanSkuName = 'S1'
param appServicePlanSkuCapacity = 1

// Serverless SQL with auto-pause disabled (ADR 0008)
param sqlAutoPauseDelay = -1
param sqlMinCapacity = '1'
param sqlMaxSizeBytes = 10737418240 // 10 GB

// Entra ID SQL admin - replace with your prd admin group
param sqlAdminObjectId = '00000000-0000-0000-0000-000000000000'
param sqlAdminLogin = 'MindNova SQL Admins Prd'
