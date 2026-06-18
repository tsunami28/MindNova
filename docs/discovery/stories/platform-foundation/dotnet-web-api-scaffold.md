---
key: MN-9
type: story
status: done
epic: MN-1
points: 5
priority: high
labels: [MindNova]
relates:
  - key: MN-8
    why: "implements the Azure SQL + EF Core decision from ADR 0008"
---

# .NET 10 Web API Project Scaffold

📌 Background

* No application code exists yet. The solution structure, EF Core configuration,
  local development setup, and a baseline health endpoint are needed before any
  feature work can start.
* ADR 0008 confirmed Azure SQL Database (serverless) with EF Core SQL Server provider.

🎯 What's the Goal?

* As a developer,
* I want a .NET 10 Web API solution with EF Core (SQL Server), Docker Compose for
  local dev, and a health check endpoint,
* So that I can run the application locally and begin building domain features.

💡 Expected Value

* Establishes the project structure, conventions, and local dev loop that every
  subsequent story depends on.

✅ Success Criteria

* AC-1: Solution compiles targeting net10.0 with `<Nullable>disable</Nullable>` in all project files.
* AC-2: Project structure contains src/MindNova.Api (Web API), src/MindNova.Domain (class lib),
  src/MindNova.Infrastructure (class lib), and tests/MindNova.Api.Tests (xUnit).
* AC-3: GET /health returns HTTP 200 with a healthy status when the database is reachable.
* AC-4: GET /health returns HTTP 503 (unhealthy) when the database is unreachable.
* AC-5: EF Core DbContext is registered with the SQL Server provider and resolves from DI.
* AC-6: An initial EF Core migration exists and applies cleanly to an empty database.
* AC-7: Docker Compose file starts a SQL Server 2022 container; the API connects to it on startup.
* AC-8: Central Package Management (Directory.Packages.props) is in use; no `<Version>` attributes
  appear in individual .csproj files.
* AC-9: A README documents prerequisites and steps to run the application locally.

Test trait mapping:
- AC-1: build-time verification (compilation fails if violated); not unit-tested.
- AC-2: build-time verification (project structure); not unit-tested.
- AC-3: `[Trait("Story","MN-9")]` + `[Trait("AC","AC-3")]` - integration test via WebApplicationFactory
  with a test SQL container; asserts 200 and healthy response body.
- AC-4: `[Trait("Story","MN-9")]` + `[Trait("AC","AC-4")]` - integration test with an invalid connection
  string; asserts 503.
- AC-5: `[Trait("Story","MN-9")]` + `[Trait("AC","AC-5")]` - unit test resolving MindNovaDbContext from
  a test service provider; asserts non-null and correct provider.
- AC-6: `[Trait("Story","MN-9")]` + `[Trait("AC","AC-6")]` - integration test applying migrations to a
  test database; asserts no exceptions and schema exists.
- AC-7: manual verification (Docker Compose up, observe API logs); not unit-tested.
- AC-8: build-time verification (CI script or test scanning .csproj files for stray Version attributes);
  not unit-tested.
- AC-9: manual verification (file exists, content reviewed); not unit-tested.

🛠️ How we'll do it

* Create solution: src/MindNova.Api (Web API), src/MindNova.Domain (class lib),
  src/MindNova.Infrastructure (EF Core, data access).
* Add tests/MindNova.Api.Tests (xUnit + Moq + AutoFixture).
* Docker Compose: SQL Server 2022 container + API container (or dotnet run).
* Wire EF Core DbContext in Infrastructure, register in Api's Program.cs.
* Health check via ASP.NET Core built-in health checks middleware.

⚠️ Risks & Blockers

* None - MN-8 (DB choice) is resolved.

🔧 Technical Implementation

* Solution: `MindNova/MindNova.sln` (4 projects)
* API host: `MindNova/src/MindNova.Api/Program.cs` (health checks, DI, controllers)
* DbContext: `MindNova/src/MindNova.Infrastructure/Data/MindNovaDbContext.cs`
* DI registration: `MindNova/src/MindNova.Infrastructure/DependencyInjection.cs`
* Migration: `MindNova/src/MindNova.Infrastructure/Data/Migrations/` (InitialCreate)
* Docker Compose: `MindNova/docker-compose.yml` (SQL Server 2022)
* Tests: `MindNova/tests/MindNova.Api.Tests/Health/HealthEndpointTests.cs` (AC-3, AC-4),
  `MindNova/tests/MindNova.Api.Tests/Infrastructure/DbContextRegistrationTests.cs` (AC-5),
  `MindNova/tests/MindNova.Api.Tests/Infrastructure/MigrationTests.cs` (AC-6)
* ADR: `docs/adrs/0008-azure-sql-database-serverless.md`
* Branch: `MN-9`
