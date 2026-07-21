---
key: MN-29
type: story
status: in-progress
epic: MN-4
points: 5
priority: minor
labels: [MindNova]
relates:
  - key: MN-28
    why: "depends on the AvailabilitySlot entity and migration"
  - key: MN-25
    why: "follows the same CRUD endpoint pattern (controller, service, DTOs)"
---

# Availability CRUD API Endpoints

📌 Background

* Therapists and schedulers need to create, view, update, and delete availability
  slots. Each slot belongs to a therapist profile.

🎯 What's the Goal?

* As a therapist or scheduler,
* I want to manage availability slots per therapist via the API,
* So that the system knows when each therapist can accept sessions.

💡 Expected Value

* Therapists define their schedules digitally. Foundation for conflict detection
  and calendar views.

✅ Success Criteria

* AC-1: POST /api/therapists/{therapistId}/availability with valid data creates a slot.
* AC-2: POST with an invalid TherapistProfileId returns ProblemDetails (404).
* AC-3: POST with overlapping time for the same day/therapist returns ProblemDetails.
* AC-4: GET /api/therapists/{therapistId}/availability returns all slots for that therapist.
* AC-5: GET supports optional date_from/date_to query params to filter relevant slots.
* AC-6: PUT /api/availability/{id} updates the slot's times.
* AC-7: DELETE /api/availability/{id} removes the slot.
* AC-8: All endpoints require authentication.

🛠️ How we'll do it

* Add AvailabilityController (sub-resource of therapists for create/list,
  top-level for update/delete by slot ID).
* Add IAvailabilityService / AvailabilityService.
* Overlap validation in the service layer.
* DTOs: CreateAvailabilityRequest, AvailabilitySlotResponse.

⚠️ Risks & Blockers

* Depends on MN-28 (domain model).

## Artifacts and references

* Controller - src/MindNova.Api/Controllers/AvailabilityController.cs
* DTOs - src/MindNova.Api/Contracts/CreateAvailabilityRequest.cs, UpdateAvailabilityRequest.cs, AvailabilitySlotResponse.cs
* Service interface - src/MindNova.Infrastructure/Services/IAvailabilityService.cs
* Service implementation - src/MindNova.Infrastructure/Services/AvailabilityService.cs
* Integration tests - tests/MindNova.Api.Tests/Availability/AvailabilityEndpointTests.cs
* PR - https://github.com/tsunami28/MindNova/pull/24
