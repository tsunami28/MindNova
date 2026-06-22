---
key: MN-11
type: story
status: in-progress
epic: MN-1
points: 5
priority: high
labels: [MindNova]
relates:
  - key: MN-9
    why: "needs the project structure to define azure.yaml service reference"
  - key: MN-8
    why: "ADR 0008 decides serverless SQL tier used in AC-2/AC-5/AC-6"
---

# Azure Infrastructure (Bicep/azd)

📌 Background

* MindNova deploys to Azure WebApp with Azure SQL. Infrastructure-as-code ensures
  repeatable provisioning across dev and prd environments.
* Can be developed in parallel with MN-10 (independent of application auth logic).

🎯 What's the Goal?

* As a DevOps engineer,
* I want Bicep templates and azd configuration that provision the Azure WebApp and
  Azure SQL Database for dev and prd,
* So that deployment is automated, repeatable, and environment-aware.

💡 Expected Value

* One-command provisioning. No manual portal clicks. Environment parity between
  dev and prd (differing only in SKU and scale).

✅ Success Criteria

* Bicep modules: Azure WebApp (Linux, .NET 10), Azure SQL Database (serverless),
  App Service Plan, Key Vault (connection string storage).
* azure.yaml wires the API project for azd deploy.
* Parameter files for dev and prd environments.
* Dev uses serverless SQL (auto-pause) and Free/Basic App Service Plan.
* Prd uses serverless or Standard SQL and Standard App Service Plan.
* Managed Identity connects WebApp to SQL (no connection string secrets in app settings).
* azd up provisions and deploys the API end-to-end.

🛠️ How we'll do it

* Create infra/ directory with main.bicep, modules/ (web-app.bicep, sql.bicep, key-vault.bicep).
* Add azure.yaml at repo root referencing the API project.
* Use Bicep parameter files (main.bicepparam) with environment-specific values.
* Configure managed identity and SQL AAD authentication for passwordless access.

⚠️ Risks & Blockers

* Subscription quotas or permissions may block first deployment; verify access early.
* Managed Identity + SQL AAD auth requires the deployer to have directory permissions.

## Decisions and ADRs

* 2026-06-18: Azure SQL Database (serverless) selected as backend database - see docs/adrs/0008-azure-sql-database-serverless.md
* 2026-06-22: AVM registry modules (pinned versions) over local Bicep modules - see docs/adrs/0009-avm-registry-modules-for-bicep.md

## Artifacts and references

* Bicep orchestrator - MindNova/infra/main.bicep
* Dev parameters - MindNova/infra/main.dev.bicepparam
* Prd parameters - MindNova/infra/main.prd.bicepparam
* azd manifest - MindNova/azure.yaml
