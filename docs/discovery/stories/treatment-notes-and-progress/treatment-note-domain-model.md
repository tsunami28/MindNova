---
key: MN-32
type: story
status: done
epic: MN-5
points: 3
priority: minor
labels: [MindNova]
relates:
  - key: MN-17
    why: "follows the same domain model pattern (entity, EF config, migration)"
  - key: MN-18
    why: "notes attach to session records via SessionId FK"
---

# Treatment Note Domain Model and Migration

📌 Background

* Therapists document session observations, treatment plans, and client progress
  after each session. A TreatmentNote entity is the persistence foundation for
  all note-related features.

🎯 What's the Goal?

* As a developer,
* I want a TreatmentNote entity with EF Core configuration and migration,
* So that clinical notes are persisted and queryable per session.

💡 Expected Value

* Enables downstream features: note CRUD, query by client/session, soft-delete audit.

✅ Success Criteria

* AC-1: TreatmentNote entity with properties: Id (Guid), SessionId (Guid FK),
  TherapistUserId (string), PresentingIssue (string, max 4000), Interventions
  (string, max 4000), Homework (string, max 2000), ProgressRating (int, 1-10),
  FreeText (string, max 8000), CreatedAt (DateTime), UpdatedAt (DateTime),
  IsDeleted (bool), DeletedAt (DateTime?), DeletedByUserId (string?).
* AC-2: EF Core config with required FK to Session, index on SessionId, and
  index on TherapistUserId.
* AC-3: Migration adds TreatmentNotes table and applies cleanly.
* AC-4: DbContext exposes DbSet<TreatmentNote>.
* AC-5: Soft-delete columns (IsDeleted, DeletedAt, DeletedByUserId) are present
  and default to not-deleted.

🛠️ How we'll do it

* Add TreatmentNote.cs in src/MindNova.Domain/Entities/.
* Add TreatmentNoteConfiguration.cs in src/MindNova.Infrastructure/Data/.
* Register DbSet and configuration in MindNovaDbContext.
* Generate EF Core migration.

⚠️ Risks & Blockers

* None - follows established pattern from MN-17, MN-24, MN-28.

## Artifacts and references

* Entity - src/MindNova.Domain/Entities/TreatmentNote.cs
* EF configuration - src/MindNova.Infrastructure/Data/TreatmentNoteConfiguration.cs
* DbContext registration - src/MindNova.Infrastructure/Data/MindNovaDbContext.cs
* Migration - src/MindNova.Infrastructure/Data/Migrations/ (AddTreatmentNotes)
* Tests - tests/MindNova.Api.Tests/Infrastructure/TreatmentNoteConfigurationTests.cs
* PR - https://github.com/tsunami28/MindNova/pull/28
