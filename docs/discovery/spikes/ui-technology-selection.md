---
key: MN-39
type: spike
status: backlog
priority: minor
labels: [MindNova]
relates:
  - key: MN-8
    why: "technology choice gates how UI stories are sliced and estimated"
  - key: MN-1
    why: "must integrate with the existing .NET 10 backend and Azure deployment"
---

# UI Technology Selection

📌 Background

* The MindNova backend is .NET 10 with ASP.NET Identity (JWT), deployed via
  Azure App Service (Bicep/azd). The UI framework must integrate with this
  stack, consume the REST API, and be maintainable by the existing team.

🎯 What's the Goal?

* As the tech lead,
* I want a recommended UI framework with a prototype proving it works end-to-end,
* So that the team can slice UI stories with confidence in the chosen stack.

💡 Expected Value

* Avoids rework from picking the wrong framework. Proves auth, API consumption,
  and deployment work before committing to full UI development.

✅ Success Criteria

* Evaluate at least 3 options (e.g. Blazor Server, Blazor WASM, React + Vite).
* Score each on: team skillset fit, .NET integration, auth flow (JWT),
  deployment model (Azure App Service), component ecosystem, bundle size,
  development speed.
* Build a thin prototype with the recommended option: login page, one list
  page (e.g. clients), one form (e.g. create client), confirming API consumption
  and JWT auth work end-to-end.
* Document the decision as an ADR in docs/adrs/.

🛠️ How we'll do it

* Research and compare the candidates against the scoring criteria.
* Build the prototype in a feature branch.
* Write the ADR with the comparison table and recommendation.
* Present findings for team review.

⚠️ Risks & Blockers

* Team familiarity with frontend frameworks varies; the spike should account
  for the learning curve in its recommendation.
* If Blazor is chosen, decide between Server and WASM hosting models (each
  has different deployment and latency characteristics).
