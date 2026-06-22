// ---------------------------------------------------------------------------
// MindNova - Dev environment parameters
// AC-5: Free/Basic App Service Plan, serverless SQL with auto-pause
// ---------------------------------------------------------------------------

using 'main.bicep'

param environmentName = 'dev'

// Basic App Service Plan (B1) - lowest tier supporting always-on;
// use F1 (Free) if cost must be zero but alwaysOn will be disabled
param appServicePlanSkuName = 'B1'
param appServicePlanSkuCapacity = 1

// Serverless SQL with auto-pause after 60 minutes of inactivity (ADR 0008)
param sqlAutoPauseDelay = 60
param sqlMinCapacity = '0.5'
param sqlMaxSizeBytes = 2147483648 // 2 GB

// Entra ID SQL admin - replace with your dev admin group
param sqlAdminObjectId = '00000000-0000-0000-0000-000000000000'
param sqlAdminLogin = 'MindNova SQL Admins Dev'
