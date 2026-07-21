---
key: MN-28
type: story
status: backlog
epic: MN-4
points: 3
priority: minor
labels: [MindNova]
relates:
  - key: MN-24
    why: "follows the same domain model pattern (entity, EF config, migration)"
  - key: MN-6
    why: "availability slots are per therapist (TherapistProfile FK)"
---

# Availability Slot Domain Model and Migration

📌 Background

* Therapists define when they are available for sessions. An AvailabilitySlot
  represents either a recurring weekly block (e.g. Mondays 9-12) or a one-off
  date-specific block, per therapist.

🎯 What's the Goal?

* As a developer,
* I want an AvailabilitySlot entity with EF Core configuration and migration,
* So that therapist availability is persisted and queryable.

💡 Expected Value

* Enables downstream features: availability CRUD, conflict detection, calendar view.

✅ Success Criteria

* AC-1: AvailabilitySlot entity with properties: Id (Guid), TherapistProfileId (Guid FK),
  DayOfWeek (int?, 0-6 for recurring), SpecificDate (DateTime?, for one-off),
  StartTime (TimeSpan), EndTime (TimeSpan), IsRecurring (bool),
  CreatedAt (DateTime), UpdatedAt (DateTime).
* AC-2: EF Core config with required FK to TherapistProfile and index on
  TherapistProfileId.
* AC-3: A check constraint or validation ensures either DayOfWeek (recurring) or
  SpecificDate (one-off) is set, not both.
* AC-4: Migration adds AvailabilitySlots table and applies cleanly.
* AC-5: DbContext exposes DbSet<AvailabilitySlot>.

🛠️ How we'll do it

* Add AvailabilitySlot.cs in src/MindNova.Domain/Entities/.
* Add AvailabilitySlotConfiguration.cs in src/MindNova.Infrastructure/Data/.
* Register DbSet and configuration in MindNovaDbContext.
* Generate EF Core migration.

⚠️ Risks & Blockers

* Recurring availability with exceptions (e.g. holiday overrides) could be complex.
  V1 models slots independently; a "blocked" slot type can override a recurring one.
