# ADR 0010: Blazor Server for Web Application UI

**Status:** Accepted
**Date:** 2026-07-29
**Supersedes:** none
**Superseded by:** none

## Context

MindNova's API surface is complete (7 epics, 215 tests). The team needs a web UI for practice staff (MN-8). The backend is .NET 10 with ASP.NET Identity (JWT), deployed to Azure App Service via azd (MindNova/azure.yaml). The team's primary expertise is C#; no one has production React or Angular experience.

Three candidates were evaluated in spike MN-39: Blazor Server, Blazor WebAssembly, and React + Vite. The evaluation criteria were: team skillset fit, .NET integration depth, JWT auth flow, Azure App Service deployment model, component ecosystem, bundle size, and development speed.

## Decision

Use **Blazor Server** with **MudBlazor** as the component library for the MindNova web UI. The UI project will be a separate project in the solution (`MindNova.Web`) consuming the API via HTTP client with JWT authentication.

Blazor Server renders on the server and pushes UI diffs to the browser over a SignalR connection. This means:
- No client-side .NET runtime download (unlike WASM's 5-10 MB)
- The team writes C# and Razor (no TypeScript, no second build pipeline)
- Deploys as part of the existing ASP.NET application (single azure.yaml service entry)

## Consequences

**Positive:**

- No new language or toolchain for the team.
- Single deployment artifact; no changes to Bicep infrastructure.
- MudBlazor provides Material Design components (tables, forms, dialogs, charts) covering all MN-8 success criteria.
- Migration to Blazor WASM later is low-cost (same component code, different hosting model).

**Negative:**

- Every UI interaction requires a server roundtrip via SignalR; latency depends on network quality.
- Server holds per-connection state; memory scales with concurrent users (acceptable for a small practice).
- Disconnected clients see a "reconnecting" overlay; no offline support.

**Neutral:**

- MudBlazor is MIT-licensed and actively maintained but is a third-party dependency.
- WebSocket support in Azure App Service is available on all tiers (must be enabled in App Service config).

## Alternatives considered

1. **Blazor WebAssembly.** Runs client-side after initial download. Rejected because the 5-10 MB initial load adds no benefit for an internal tool, and Server is simpler to deploy with the existing infrastructure.
2. **React + Vite (TypeScript).** Richest component ecosystem. Rejected because the team has no TypeScript expertise, it introduces a second build pipeline, and requires separate SPA deployment or embedding.

## Verification

- The prototype branch (built during spike MN-39) confirms: login page with JWT, client list with search, create client form, all consuming the REST API.
- After the UI project is added, `dotnet build MindNova.slnx` must still succeed with zero warnings.
- MudBlazor renders correctly when the app is accessed at the root URL.

## References

- Spike: docs/discovery/spikes/ui-technology-selection.md (MN-39)
- Epic: docs/discovery/epics/web-application-ui.md (MN-8)
- Deployment config: MindNova/azure.yaml (service `api`, host `appservice`)
- MudBlazor: https://mudblazor.com
