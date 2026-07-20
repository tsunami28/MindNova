---
key: MN-24
type: story
status: backlog
epic: MN-6
points: 3
priority: minor
labels: [MindNova]
relates:
  - key: MN-17
    why: "follows the same domain model pattern (entity, EF config, migration)"
---

# Therapist Profile Domain Model and Migration

📌 Background

* MindNova assigns therapists to clients based on specialisation and workload.
  The current ApplicationUser (IdentityUser) has no profile data. A dedicated
  TherapistProfile entity stores specialisations and capacity.

🎯 What's the Goal?

* As a developer,
* I want a TherapistProfile entity with EF Core configuration and migration,
* So that therapist-specific data is persisted and queryable.

💡 Expected Value

* Enables all downstream allocation features (CRUD, assignment, caseload dashboard).

✅ Success Criteria

* AC-1: TherapistProfile entity exists with properties: Id (Guid), UserId (string FK
  to ApplicationUser), Specialisations (List<string>), MaxCaseload (int),
  IsActive (bool), CreatedAt (DateTime), UpdatedAt (DateTime).
* AC-2: EF Core entity configuration maps TherapistProfile with a required FK to
  ApplicationUser and an index on UserId.
* AC-3: Specialisations is stored as a JSON column (nvarchar(max)).
* AC-4: An EF Core migration adds the TherapistProfiles table and applies cleanly.
* AC-5: The DbContext exposes a DbSet<TherapistProfile>.
* AC-6: A unique constraint on UserId prevents duplicate profiles per user.

🛠️ How we'll do it

* Add TherapistProfile.cs in src/MindNova.Domain/Entities/.
* Add TherapistProfileConfiguration.cs in src/MindNova.Infrastructure/Data/Configurations/.
* Register DbSet<TherapistProfile> in MindNovaDbContext.
* Generate and apply EF Core migration.
* Follow the same pattern as Session entity (FK to ApplicationUser via string UserId).

⚠️ Risks & Blockers

* Specialisations as JSON column requires EF Core value conversion. Alternative:
  separate TherapistSpecialisation join table (simpler queries but more tables).
