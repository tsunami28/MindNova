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

* AC-1: infra/main.bicep exists and compiles without errors (`az bicep build` succeeds).
* AC-2: Bicep defines modules for App Service Plan, WebApp (Linux, .NET 10 runtime),
  Azure SQL Database (serverless compute tier), and Key Vault.
* AC-3: azure.yaml at the repo root references the MindNova.Api project as the
  deployable service.
* AC-4: Environment-specific parameter files exist for dev and prd
  (e.g. main.dev.bicepparam, main.prd.bicepparam).
* AC-5: Dev parameters specify a Free or Basic App Service Plan SKU and serverless SQL
  with auto-pause enabled.
* AC-6: Prd parameters specify a Standard App Service Plan SKU and serverless or
  provisioned SQL without auto-pause.
* AC-7: The WebApp authenticates to Azure SQL via Managed Identity; no password-based
  connection string appears in app settings or Bicep outputs.
* AC-8: `azd up` from a clean state provisions all resources and deploys the API
  end-to-end without manual intervention.

Test trait mapping:
- AC-1: `[Trait("Story","MN-11")]` + `[Trait("AC","AC-1")]` - build-time verification;
  a CI step or local script runs `az bicep build` and asserts exit code 0.
- AC-2: verified by inspecting Bicep module structure; not unit-tested.
- AC-3: verified by inspecting azure.yaml content; not unit-tested.
- AC-4: verified by file existence check; not unit-tested.
- AC-5: verified by inspecting dev parameter file values; not unit-tested.
- AC-6: verified by inspecting prd parameter file values; not unit-tested.
- AC-7: verified by reviewing Bicep outputs and deployed app settings for absence of
  password-based connection strings; not unit-tested.
- AC-8: manual verification (run `azd up` against a test subscription and observe
  successful deployment); not unit-tested.

🛠️ How we'll do it

* Create infra/ directory with main.bicep, using modules (web-app, sql, key-vault) based on the [Azure Verified Modules](https://github.com/Azure/Azure-Verified-Modules)
* Add azure.yaml at repo root referencing the API project.
* Use Bicep parameter files (main.bicepparam) with environment-specific values.
* Configure managed identity and SQL AAD authentication for passwordless access.

⚠️ Risks & Blockers

* Subscription quotas or permissions may block first deployment; verify access early.
* Managed Identity + SQL AAD auth requires the deployer to have directory permissions.
