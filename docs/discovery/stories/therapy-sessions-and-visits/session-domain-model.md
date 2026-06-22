---
key: MN-17
type: story
status: in-progress
epic: MN-3
points: 3
priority: minor
labels: [MindNova]
relates:
  - key: MN-13
    why: "follows the same domain model pattern (entity, EF config, migration)"
  - key: MN-2
    why: "Session.ClientId references the Client entity"
---

# Session Domain Model and Migration

📌 Background

* The session entity is the core record for therapy visits. Before any session
  endpoint can be built, the data model, enums, EF Core configuration, and
  migration must exist.

🎯 What's the Goal?

* As a developer,
* I want a Session entity with status and type enums, EF Core configuration,
  and a database migration,
* So that session data can be persisted and the CRUD endpoints have a foundation.

💡 Expected Value

* Establishes the data schema for the entire Therapy Sessions epic. Unblocks
  all session-related endpoints.

✅ Success Criteria

* AC-1: Session entity class exists in MindNova.Domain/Entities/ with all ten
  properties: Id (Guid), ClientId (Guid), TherapistUserId (string),
  ScheduledAt (DateTime), DurationMinutes (int), SessionType (SessionType
  enum), Status (SessionStatus enum), Notes (string), CreatedAt (DateTime),
  UpdatedAt (DateTime).
* AC-2: SessionType enum exists with exactly four members: Individual, Group,
  Intake, FollowUp.
* AC-3: SessionStatus enum exists with exactly four members: Scheduled,
  Completed, Cancelled, NoShow.
* AC-4: MindNovaDbContext declares a DbSet<Session> property and the Session
  entity is registered in OnModelCreating.
* AC-5: An IEntityTypeConfiguration<Session> configures a FK from ClientId to
  Client and a FK from TherapistUserId to ApplicationUser.
* AC-6: The entity configuration defines indexes on ClientId,
  TherapistUserId, and ScheduledAt.
* AC-7: The entity configuration marks ClientId, TherapistUserId, and
  ScheduledAt as required, and sets a max length on Notes.
* AC-8: An EF Core migration creates the Sessions table with all configured
  columns, constraints, foreign keys, and indexes.
* AC-9: The migration applies cleanly against a SQL Server Testcontainer
  without errors.
* AC-10: Domain validation rejects a Session with an empty ClientId.
* AC-11: Domain validation rejects a Session with an empty or whitespace
  TherapistUserId.
* AC-12: Domain validation rejects a Session with DurationMinutes less than
  or equal to zero.

(Test traits: each AC covered by xUnit tests tagged [Trait("Story","MN-17")]
+ [Trait("AC","AC-n")]. AC-1 through AC-7 and AC-10 through AC-12 are unit
tests. AC-8/AC-9 are integration tests using the existing SqlServerContainer
pattern.)

🛠️ How we'll do it

* Add SessionType and SessionStatus enums to MindNova.Domain/Entities/.
* Add Session class to MindNova.Domain/Entities/.
* Add SessionConfiguration : IEntityTypeConfiguration<Session> in Infrastructure.
* Register in DbContext, generate migration via dotnet ef migrations add.
* Add SessionValidator with required field and business rule checks.
* TherapistUserId is a string FK to ApplicationUser.Id (Identity uses string
  keys). When MN-6 ships the TherapistProfile entity, sessions remain linked
  to the user identity; the profile adds metadata (specialisations, caseload).

⚠️ Risks & Blockers

* Depends on MN-2 (Client entity) for the FK target, which is complete.
* TherapistUserId references ApplicationUser (Identity), not a dedicated
  Therapist entity. MN-6 adds TherapistProfile later without breaking this FK.

## Artifacts and references

* Entity - src/MindNova.Domain/Entities/Session.cs
* SessionType enum - src/MindNova.Domain/Entities/SessionType.cs
* SessionStatus enum - src/MindNova.Domain/Entities/SessionStatus.cs
* Validator - src/MindNova.Domain/Validation/SessionValidator.cs
* EF configuration - src/MindNova.Infrastructure/Data/SessionConfiguration.cs
* Migration - src/MindNova.Infrastructure/Data/Migrations/ (AddSessions)
* Unit tests - tests/MindNova.Api.Tests/Domain/SessionEntityTests.cs, SessionValidatorTests.cs
* Config tests - tests/MindNova.Api.Tests/Infrastructure/SessionConfigurationTests.cs
* Integration tests - tests/MindNova.Api.Tests/Infrastructure/MigrationTests.cs
