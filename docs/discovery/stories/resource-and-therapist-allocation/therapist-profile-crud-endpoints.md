---
key: MN-25
type: story
status: in-progress
epic: MN-6
points: 5
priority: minor
labels: [MindNova]
relates:
  - key: MN-24
    why: "depends on the TherapistProfile entity and migration"
  - key: MN-14
    why: "follows the same CRUD endpoint pattern (controller, service, DTOs)"
---

# Therapist Profile CRUD API Endpoints

📌 Background

* With the TherapistProfile model in place, the API needs endpoints to create,
  read, update, and deactivate therapist profiles. Practice managers use these
  to maintain the therapist roster.

🎯 What's the Goal?

* As a practice manager,
* I want to create, view, update, and deactivate therapist profiles via the API,
* So that the therapist roster is managed digitally.

💡 Expected Value

* Core therapist management. Assignment and caseload features depend on profiles
  existing in the system.

✅ Success Criteria

* AC-1: POST /api/therapists with valid data (UserId, Specialisations, MaxCaseload)
  returns the created profile with IsActive = true and a generated Id.
* AC-2: POST /api/therapists with a UserId that does not reference an existing
  ApplicationUser returns a ProblemDetails error.
* AC-3: POST /api/therapists with a duplicate UserId returns a ProblemDetails error.
* AC-4: GET /api/therapists/{id} returns the profile's full data.
* AC-5: GET /api/therapists/{id} with a non-existent ID returns ProblemDetails (404).
* AC-6: GET /api/therapists returns a paginated list of active profiles.
* AC-7: GET /api/therapists with include_inactive=true includes inactive profiles.
* AC-8: PUT /api/therapists/{id} updates Specialisations and MaxCaseload.
* AC-9: DELETE /api/therapists/{id} sets IsActive = false (soft-deactivate).
* AC-10: All endpoints require authentication.

🛠️ How we'll do it

* Add TherapistsController in src/MindNova.Api/Controllers/.
* Add ITherapistService / TherapistService in src/MindNova.Infrastructure/Services/.
* Request/response DTOs in src/MindNova.Api/Contracts/.
* Follow existing controller pattern (thin, Ok() for all responses, ProblemDetails
  for errors).
* Pagination with page/page_size query params (same as ClientsController).

⚠️ Risks & Blockers

* Depends on MN-20 (domain model).
* Role-based restriction (only Admin can manage profiles) is out of scope for V1;
  all authenticated users can access.

## Artifacts and references

* API contract - specs/therapists.openapi.yaml
* Controller - src/MindNova.Api/Controllers/TherapistsController.cs
* DTOs - src/MindNova.Api/Contracts/CreateTherapistRequest.cs, UpdateTherapistRequest.cs, TherapistProfileResponse.cs
* Service interface - src/MindNova.Infrastructure/Services/ITherapistService.cs
* Service implementation - src/MindNova.Infrastructure/Services/TherapistService.cs
* Integration tests - tests/MindNova.Api.Tests/Therapists/TherapistEndpointTests.cs
* PR - https://github.com/tsunami28/MindNova/pull/19
