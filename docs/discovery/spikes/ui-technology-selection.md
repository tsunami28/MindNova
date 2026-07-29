---
key: MN-39
type: spike
status: done
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

## Key findings

* Blazor Server is the fastest path for a C#-only team: no client download,
  no new language, same debugging tools. SignalR dependency is acceptable for
  an internal practice management tool with a small user base.
* Blazor WASM removes SignalR but adds a 5-10 MB initial download with no
  clear benefit over Server for V1.
* React offers the richest component ecosystem but introduces TypeScript, a
  second build pipeline, and a separate deployment model. No existing team
  expertise.
* Blazor Server deploys as part of the existing ASP.NET app (single azure.yaml
  service entry, no Bicep changes). WASM and React both need embedding or
  separate static hosting.
* All three support JWT auth. Blazor uses AuthenticationStateProvider natively;
  React requires manual token handling.

## Implications

* Recommend Blazor Server for V1. Minimal new tooling, deploys with the existing
  API, team stays in C#. MudBlazor provides tables, forms, dialogs, and charts
  covering all MN-8 success criteria.
* Migration from Blazor Server to WASM is straightforward later (same component
  code, different hosting model) if offline or richer client experience is needed.
* Prototype should confirm: login flow, client list with search, create client
  form, all consuming the REST API with JWT.

## Open questions

* Blazor UI as a separate project (MindNova.Web) or embedded in the API project?
  Separate is cleaner for build isolation.
* Component library: MudBlazor vs Radzen? MudBlazor has broader community
  adoption and Material Design alignment.
* SignalR transport: WebSockets or Long Polling fallback in Azure App Service?

## Decisions and ADRs

* 2026-07-29: Blazor Server with MudBlazor selected for the web UI - see docs/adrs/0010-blazor-server-for-web-ui.md

## Artifacts and references

* ADR - docs/adrs/0010-blazor-server-for-web-ui.md
