---
key: MN-40
type: story
status: in-progress
epic: MN-8
points: 8
priority: minor
labels: [MindNova]
relates:
  - key: MN-39
    why: "implements the Blazor Server decision from the UI technology spike"
  - key: MN-10
    why: "follows the same auth infrastructure pattern (JWT, roles)"
---

# Blazor Server Project Scaffold and Auth

📌 Background

* The UI technology decision (ADR 0010) selected Blazor Server with MudBlazor.
  Before any domain pages can be built, the project, auth pipeline, layout,
  and deployment config must be in place.

🎯 What's the Goal?

* As a developer,
* I want a working Blazor Server project with MudBlazor, JWT auth, and role-gated
  navigation,
* So that domain UI pages can be added incrementally.

💡 Expected Value

* Foundation for all subsequent UI stories. Proves the tech stack end-to-end.

✅ Success Criteria

* AC-1: MindNova.Web Blazor Server project added to MindNova.slnx.
* AC-2: MudBlazor installed and configured (theme, layout, navigation drawer).
* AC-3: Login page that authenticates against POST /api/auth/login and stores the
  JWT token.
* AC-4: AuthenticationStateProvider reads the token and exposes claims (user ID,
  roles) to Blazor components.
* AC-5: Navigation menu shows/hides items based on role (Admin, Therapist,
  Receptionist).
* AC-6: Unauthenticated users are redirected to the login page.
* AC-7: azure.yaml updated to include the web project for deployment.
* AC-8: dotnet build MindNova.slnx succeeds with zero warnings.

🛠️ How we'll do it

* Create src/MindNova.Web as a Blazor Server project (dotnet new blazorserver).
* Add MudBlazor NuGet package and configure in Program.cs.
* Implement a custom AuthenticationStateProvider that manages the JWT token.
* Add MainLayout with MudBlazor AppBar, NavMenu with role-gated items.
* Add Login.razor page with email/password form calling the auth API.
* Update azure.yaml with a second service entry for the web project.

⚠️ Risks & Blockers

* SignalR WebSocket support must be enabled in Azure App Service config.
* Token refresh strategy (V1: re-login on expiry; token refresh is a follow-up).

## Artifacts and references

* Project - src/MindNova.Web/MindNova.Web.csproj
* Program.cs - src/MindNova.Web/Program.cs
* Auth state provider - src/MindNova.Web/Services/JwtAuthenticationStateProvider.cs
* Auth service - src/MindNova.Web/Services/AuthService.cs
* Login page - src/MindNova.Web/Pages/Login.razor
* Main layout - src/MindNova.Web/Shared/MainLayout.razor
* Nav menu - src/MindNova.Web/Shared/NavMenu.razor
* Deployment - MindNova/azure.yaml (web service entry)
* ADR - docs/adrs/0010-blazor-server-for-web-ui.md
* PR - https://github.com/tsunami28/MindNova/pull/39
