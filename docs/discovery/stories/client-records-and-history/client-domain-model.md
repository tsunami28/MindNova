---
key: MN-13
type: story
status: backlog
epic: MN-2
points: 3
priority: high
labels: [MindNova]
relates:
  - key: MN-9
    why: "depends on project scaffold and DbContext"
---

# Client Domain Model and Migration

📌 Background

* The client entity is the core domain object that all other features reference.
  Before any endpoint can be built, the data model, EF Core configuration, and
  migration must exist.

🎯 What's the Goal?

* As a developer,
* I want a Client entity with EF Core configuration and a database migration,
* So that client data can be persisted and the CRUD endpoints have a foundation.

💡 Expected Value

* Establishes the data schema for the entire Client Records epic. Unblocks all
  client-related endpoints.

✅ Success Criteria

* Client entity in MindNova.Domain: Id (Guid), FirstName, LastName, DateOfBirth,
  Email, Phone, EmergencyContactName, EmergencyContactPhone, Address, CreatedAt,
  UpdatedAt, IsArchived.
* EF Core entity configuration in MindNova.Infrastructure (explicit column types,
  indexes on LastName and Email).
* Migration creates the Clients table in the database.
* Migration runs cleanly against the local SQL Server container.
* Unit tests verify entity instantiation and validation rules.

🛠️ How we'll do it

* Add Client class to MindNova.Domain/Entities/.
* Add ClientConfiguration : IEntityTypeConfiguration<Client> in Infrastructure.
* Register in DbContext, generate migration via dotnet ef migrations add.
* Add basic domain validation (required fields, email format).

⚠️ Risks & Blockers

* Depends on MN-9 (project scaffold with DbContext).
* Field list may expand when GDPR/consent requirements are scoped; keep the
  entity extensible.
