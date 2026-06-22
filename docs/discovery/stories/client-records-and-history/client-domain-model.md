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

* AC-1: Client entity class exists in MindNova.Domain/Entities/ with all
  twelve properties: Id (Guid), FirstName, LastName, DateOfBirth (DateTime),
  Email, Phone, EmergencyContactName, EmergencyContactPhone, Address,
  CreatedAt (DateTime), UpdatedAt (DateTime), IsArchived (bool).
* AC-2: MindNovaDbContext declares a DbSet<Client> property and the Client
  entity is registered in OnModelCreating.
* AC-3: An IEntityTypeConfiguration<Client> configures explicit SQL column
  types, max lengths, and marks FirstName, LastName, and Email as required.
* AC-4: The entity configuration defines indexes on LastName and Email.
* AC-5: An EF Core migration creates the Clients table with all configured
  columns, constraints, and indexes.
* AC-6: The migration applies cleanly against a SQL Server Testcontainer
  without errors.
* AC-7: Domain validation rejects a Client with a missing or empty
  FirstName, LastName, or Email.
* AC-8: Domain validation rejects a Client with an invalid email format.

(Test traits: each AC covered by xUnit tests tagged [Trait("Story","MN-13")]
+ [Trait("AC","AC-n")]. AC-1 through AC-4 and AC-7/AC-8 are unit tests.
AC-5/AC-6 are integration tests using the existing SqlServerContainer
pattern.)

🛠️ How we'll do it

* Add Client class to MindNova.Domain/Entities/.
* Add ClientConfiguration : IEntityTypeConfiguration<Client> in Infrastructure.
* Register in DbContext, generate migration via dotnet ef migrations add.
* Add basic domain validation (required fields, email format).

⚠️ Risks & Blockers

* Depends on MN-9 (project scaffold with DbContext).
* Field list may expand when GDPR/consent requirements are scoped; keep the
  entity extensible.
