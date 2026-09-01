# MindNova API

A .NET 10 Web API for managing psychotherapy consultancy operations.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Podman](https://podman.io/) (for local SQL Server container)

## Running locally

Full runbook, including troubleshooting: [docs/local-dev-setup.md](../docs/local-dev-setup.md).

Quick start, from the `MindNova` directory:

```powershell
# 1. Start the database
podman machine start
podman start mindnova-sql   # first time: see the runbook for the podman run command

# 2. Build and migrate
dotnet build MindNova.slnx -c Debug
dotnet ef database update --project src/MindNova.Infrastructure --startup-project src/MindNova.Api

# 3. Run the API (terminal 1)
dotnet exec src/MindNova.Api/bin/Debug/net10.0/MindNova.Api.dll `
  --contentRoot src/MindNova.Api --environment Development --urls http://localhost:5193

# 4. Run the web UI (terminal 2)
dotnet exec src/MindNova.Web/bin/Debug/net10.0/MindNova.Web.dll `
  --contentRoot src/MindNova.Web --environment Development --urls http://localhost:5080
```

Then browse to `http://localhost:5080`. Health check: `http://localhost:5193/health`.

Debug builds set `UseAppHost=false`, so no `.exe` is produced and `dotnet exec` is used in
place of `dotnet run`. This avoids endpoint security software blocking the freshly built
apphost. Release builds are unaffected. A fresh database has no user account; register one
via `POST /api/auth/register` as described in the runbook.

## Running tests

```bash
cd MindNova
dotnet test
```

Integration tests use [Testcontainers](https://dotnet.testcontainers.org/) and start their own SQL Server container automatically. Podman must be running (`podman machine start`).

## Project structure

```
MindNova/
├── src/
│   ├── MindNova.Api             # Web API host, controllers, health checks
│   ├── MindNova.Domain          # Domain entities and logic
│   └── MindNova.Infrastructure  # EF Core, data access, external services
└── tests/
    └── MindNova.Api.Tests       # xUnit integration and unit tests
```

## Configuration

Connection strings are configured per environment:

- **Development**: `appsettings.Development.json` (points to Docker SQL Server on localhost:1433)
- **Production**: Set `ConnectionStrings__MindNova` via environment variable or Azure App Configuration.
