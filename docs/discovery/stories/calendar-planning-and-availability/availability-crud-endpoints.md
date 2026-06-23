---
key: MN-21
type: story
status: backlog
epic: MN-4
points: 5
priority: minor
labels: [MindNova]
relates:
  - key: MN-20
    why: "depends on the AvailabilityBlock entity and migration"
  - key: MN-18
    why: "follows the same CRUD endpoint pattern"
---

# Availability CRUD Endpoints

📌 Background

* Therapists and schedulers need to manage availability blocks: create weekly
  recurring slots, add one-off availability, update times, and remove blocks
  that are no longer valid.

🎯 What's the Goal?

* As a therapist or scheduler,
* I want to create, view, update, and delete availability blocks via the API,
* So that my schedule reflects when I am genuinely available for bookings.

💡 Expected Value

* Enables therapists to define their working hours. Required before conflict
  detection can validate proposed sessions against availability.

✅ Success Criteria

* AC-1: POST /api/availability with valid recurring block data returns the
  created block with a generated Id and CreatedAt set.
* AC-2: POST /api/availability with valid one-off block data (IsRecurring =
  false, SpecificDate set) returns the created block.
* AC-3: POST /api/availability with a TherapistUserId that does not reference
  an existing ApplicationUser returns a ProblemDetails error.
* AC-4: POST /api/availability with invalid data (empty TherapistUserId,
  StartTime >= EndTime, or recurring without DayOfWeek) returns a
  ProblemDetails error.
* AC-5: GET /api/availability?therapist_id=<id> returns all blocks for that
  therapist.
* AC-6: GET /api/availability?therapist_id=<id> for a therapist with no
  blocks returns an empty list (not an error).
* AC-7: GET /api/availability/{id} with a valid ID returns that block's full
  data.
* AC-8: GET /api/availability/{id} with a non-existent ID returns a
  ProblemDetails error indicating not found.
* AC-9: PUT /api/availability/{id} with valid data updates the block and
  returns the updated resource with UpdatedAt refreshed.
* AC-10: PUT /api/availability/{id} with a non-existent ID returns a
  ProblemDetails error indicating not found.
* AC-11: DELETE /api/availability/{id} removes the block and returns a
  confirmation.
* AC-12: DELETE /api/availability/{id} with a non-existent ID returns a
  ProblemDetails error indicating not found.
* AC-13: All endpoints require authentication; requests without a valid token
  receive a 401 response.

(Test traits: each AC covered by xUnit integration tests tagged
[Trait("Story","MN-21")] + [Trait("AC","AC-n")] using the SqlServerFixture
pattern.)

🛠️ How we'll do it

* Add AvailabilityController (thin) in MindNova.Api/Controllers/.
* Add IAvailabilityService / AvailabilityService in Infrastructure/Services.
* Use request/response DTOs.
* Register IAvailabilityService in DI.

⚠️ Risks & Blockers

* Depends on MN-20 (domain model).
* Overlap detection between availability blocks themselves (e.g. two blocks on
  the same day/time) is out of scope; the conflict detection story (MN-22)
  handles session-vs-availability conflicts only.

## Artifacts and references

* API contract - specs/availability.openapi.yaml
