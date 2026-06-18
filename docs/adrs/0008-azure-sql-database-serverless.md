# ADR 0008: Azure SQL Database (serverless) as the MindNova backend database

**Status:** Accepted
**Date:** 2026-06-18
**Supersedes:** none
**Superseded by:** none

## Context

MindNova requires a backend database to persist client records, therapy sessions, treatment notes, therapist profiles, calendar availability, and reporting aggregates. The application is:

- .NET 10 with EF Core as the ORM.
- Single-tenant (one deployment per consultancy).
- Hosted on Azure WebApp.
- Operating in dev and prd environments.

The data is inherently relational: clients have sessions, sessions have notes, therapists are assigned to clients. FK constraints, joins, and EF Core migrations are the natural tools.

Three candidates were evaluated: Azure SQL Database, Azure Database for PostgreSQL (Flexible Server), and Cosmos DB.

## Decision

Use **Azure SQL Database** with the **serverless compute tier** for both dev and prd.

- EF Core provider: `Microsoft.EntityFrameworkCore.SqlServer`.
- Dev environment: serverless tier with auto-pause (minimal cost when idle).
- Prd environment: serverless or provisioned General Purpose, sized to consultancy load.
- Local development: SQL Server container (`mcr.microsoft.com/mssql/server:2022-latest`) or LocalDB.
- Connection strings stored in Azure App Configuration, referenced per environment suffix (`-dev`, `-prd`).

## Consequences

**Positive:**

- First-class EF Core support (Microsoft's primary development target), eliminating provider edge-case risks.
- Serverless auto-pause reduces dev environment cost to approximately $5/month.
- TDE (Transparent Data Encryption) enabled by default, meeting encryption-at-rest requirements with no extra work.
- Single-vendor stack (.NET + Azure SQL + Azure WebApp) simplifies support and documentation.
- Automatic backups with point-in-time restore up to 35 days included at no extra cost.

**Negative:**

- Vendor lock-in to SQL Server dialect; migrating to PostgreSQL or another engine later would require rewriting migrations and testing provider-specific LINQ translations.
- Serverless cold-start latency (up to ~10s on first connection after auto-pause) is acceptable for dev but may need monitoring in prd if traffic is very bursty.
- Licensing is proprietary; no open-source portability.

**Neutral:**

- Semi-structured treatment note fields (free text, structured observations) will be stored as regular columns or as `nvarchar(max)` JSON when flexible schema is needed. SQL Server's JSON support is adequate for query-light scenarios; if heavy JSON querying becomes a requirement, this decision should be revisited.

## Alternatives considered

1. **Azure Database for PostgreSQL (Flexible Server).** Mature EF Core support via Npgsql, lower lock-in, excellent JSONB support for semi-structured data. Rejected because: slightly higher dev cost (no auto-pause equivalent), and the single-tenant Azure-only scope does not benefit from cross-cloud portability. Remains a viable fallback if vendor lock-in becomes a concern.

2. **Cosmos DB.** Globally distributed document database. Rejected because: MindNova's data is relational, the EF Core Cosmos provider does not support joins or traditional migrations, the minimum cost is higher, and global distribution is unnecessary for a single-tenant regional app.

## Verification

- Confirm EF Core migrations run against both the local SQL Server container and the Azure SQL serverless instance.
- Verify auto-pause activates in dev after the configured inactivity period (default 60 minutes).
- Confirm TDE is enabled by querying `sys.dm_database_encryption_keys` on the deployed database.
- Integration tests should run against the SQL Server container in CI to validate provider behaviour.

## References

- `docs/discovery/spikes/database-technology-selection.md` - the originating spike (MN-8).
- `docs/discovery/epics/platform-foundation.md` - MN-1, which consumes this decision for EF provider selection.
- [Azure SQL serverless tier documentation](https://learn.microsoft.com/en-us/azure/azure-sql/database/serverless-tier-overview)
- [EF Core SQL Server provider](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/)
