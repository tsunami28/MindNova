# MindNova API

A .NET 10 Web API for managing psychotherapy consultancy operations.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Podman](https://podman.io/) (for local SQL Server container)

## Running locally

1. Ensure the Podman machine is running:

```bash
podman machine start
```

2. Start the SQL Server container:

```bash
cd MindNova
podman compose up -d
```

2. Apply EF Core migrations:

```bash
cd MindNova
dotnet ef database update --project src/MindNova.Infrastructure --startup-project src/MindNova.Api
```

3. Run the API:

```bash
cd MindNova
dotnet run --project src/MindNova.Api
```

4. Verify the health endpoint:

```bash
curl https://localhost:5001/health
```

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
