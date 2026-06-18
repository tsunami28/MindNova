---
key: MN-8
type: spike
status: done
priority: high
labels: [MindNova]
relates:
  - key: MN-1
    why: "foundation scaffolding depends on the database choice"
---

# Evaluate Database Technology for MindNova

📌 Background

* MindNova needs a backend database, but the technology is undecided. The choice
  affects EF Core provider, hosting cost, backup strategy, and migration tooling.
  Candidates include Azure SQL, PostgreSQL (Flexible Server), and Cosmos DB.

🎯 What's the Goal?

* Evaluate candidate databases against MindNova's requirements (relational client
  data, session scheduling, treatment notes, single-tenant, Azure-hosted) and
  recommend one with a justification.

💡 Expected Value

* Unblocks MN-1 (foundation) and all domain epics. Prevents a costly mid-project
  database migration.

✅ Success Criteria

* Comparison matrix covering: cost (dev + prd), EF Core support maturity, Azure
  managed-service availability, backup/restore, encryption at rest, and local dev
  experience.
* Clear recommendation with trade-offs documented.
* Result recorded as an ADR in docs/adrs/.

⚠️ Risks & Blockers

* Must complete before MN-1 scaffolding locks in the EF provider.
