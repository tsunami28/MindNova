---
key: MN-1
type: epic
status: done
priority: high
labels: [MindNova]
---

# Platform Foundation

📌 Background

* MindNova is a new digital solution supporting psychotherapy consultancies.
  Before any domain feature can ship, the project needs a .NET 10 Web API scaffold,
  ASP.NET Identity for local-account authentication, and Azure WebApp hosting
  configuration.
* Single-tenant deployment model: one instance per consultancy.

🎯 What's the Goal?

* As a development team,
* I want a production-ready .NET 10 project scaffold with authentication and
  Azure WebApp deployment,
* So that domain features can be built on a solid, deployable foundation.

💡 Expected Value

* Unblocks all domain epics. Establishes conventions, CI pipeline, and hosting
  from day one.

✅ Success Criteria

* .NET 10 Web API project compiles and runs locally.
* ASP.NET Identity configured with registration, login, and role-based access.
* Azure WebApp deployment (Bicep/azd) provisions and deploys the API.
* Health check endpoint returns 200.
* CI pipeline runs build + test on PR.

🛠️ How we'll do it

* Scaffold .NET 10 Web API with controller-based routing.
* Add ASP.NET Identity with Entity Framework provider (database choice per MN-8).
* Create Bicep templates for Azure WebApp + supporting resources.
* Configure dev/prd environment split per repo conventions.

⚠️ Risks & Blockers

* Database choice (MN-8) must be resolved before the EF provider is locked in.
* Auth scope limited to local accounts; SSO/Entra integration is out of scope.
