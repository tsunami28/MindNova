---
key: MN-20
type: story
status: backlog
epic: MN-4
points: 3
priority: minor
labels: [MindNova]
relates:
  - key: MN-17
    why: "follows the same domain model pattern (entity, EF config, migration)"
---

# Availability Domain Model and Migration

📌 Background

* Therapists have recurring weekly availability (e.g. Monday 09:00-17:00) and
  one-off overrides (e.g. available Saturday 10:00-14:00 for a specific date).
  Before availability endpoints or conflict detection can be built, the data
  model must exist.

🎯 What's the Goal?

* As a developer,
* I want an AvailabilityBlock entity with EF Core configuration and a database
  migration,
* So that therapist availability can be persisted and queried for scheduling.

💡 Expected Value

* Establishes the data schema for the Calendar Planning epic. Unblocks
  availability CRUD and conflict detection.

✅ Success Criteria

* AC-1: AvailabilityBlock entity class exists in MindNova.Domain/Entities/
  with all eleven properties: Id (Guid), TherapistUserId (string), DayOfWeek
  (DayOfWeek?), StartTime (TimeSpan), EndTime (TimeSpan), EffectiveFrom
  (DateTime), EffectiveTo (DateTime?), IsRecurring (bool), SpecificDate
  (DateTime?), CreatedAt (DateTime), UpdatedAt (DateTime).
* AC-2: MindNovaDbContext declares a DbSet<AvailabilityBlock> property and
  the entity is registered in OnModelCreating.
* AC-3: An IEntityTypeConfiguration<AvailabilityBlock> configures a FK from
  TherapistUserId to ApplicationUser and marks TherapistUserId as required.
* AC-4: The entity configuration defines indexes on TherapistUserId and
  DayOfWeek.
* AC-5: The entity configuration maps StartTime and EndTime to SQL time(7)
  columns.
* AC-6: An EF Core migration creates the AvailabilityBlocks table with all
  configured columns, constraints, foreign key, and indexes.
* AC-7: The migration applies cleanly against a SQL Server Testcontainer
  without errors.
* AC-8: Domain validation rejects an AvailabilityBlock with an empty or
  whitespace TherapistUserId.
* AC-9: Domain validation rejects an AvailabilityBlock where StartTime >=
  EndTime.
* AC-10: Domain validation rejects a recurring block (IsRecurring = true)
  that has a null DayOfWeek.
* AC-11: Domain validation accepts a valid recurring block with DayOfWeek,
  StartTime < EndTime, and non-empty TherapistUserId.
* AC-12: Domain validation accepts a valid one-off block (IsRecurring =
  false) with a SpecificDate and no DayOfWeek.

(Test traits: each AC covered by xUnit tests tagged [Trait("Story","MN-20")]
+ [Trait("AC","AC-n")]. AC-1 through AC-5 and AC-8 through AC-12 are unit
tests. AC-6/AC-7 are integration tests using the existing SqlServerContainer
pattern.)

🛠️ How we'll do it

* Add AvailabilityBlock class to MindNova.Domain/Entities/.
* Add AvailabilityBlockConfiguration : IEntityTypeConfiguration in Infrastructure.
* Register in DbContext, generate migration.
* Add AvailabilityBlockValidator with rule checks.

⚠️ Risks & Blockers

* TimeSpan for time-of-day is idiomatic in .NET but maps to time(7) in SQL
  Server; verify EF Core handles this correctly.
