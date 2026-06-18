---
key: MN-9
type: story
status: backlog
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

* Solution compiles targeting net10.0 with nullable disabled.
* Controller-based routing with a /health endpoint returning 200.
* EF Core configured with SQL Server provider and an initial migration (empty DbContext).
* Docker Compose file starts a SQL Server container and the API connects to it.
* Central Package Management (Directory.Packages.props) in use.
* README documents how to run locally.

🛠️ How we'll do it

* Create solution: src/MindNova.Api (Web API), src/MindNova.Domain (class lib),
  src/MindNova.Infrastructure (EF Core, data access).
* Add tests/MindNova.Api.Tests (xUnit + Moq + AutoFixture).
* Docker Compose: SQL Server 2022 container + API container (or dotnet run).
* Wire EF Core DbContext in Infrastructure, register in Api's Program.cs.
* Health check via ASP.NET Core built-in health checks middleware.

⚠️ Risks & Blockers

* None - MN-8 (DB choice) is resolved.
