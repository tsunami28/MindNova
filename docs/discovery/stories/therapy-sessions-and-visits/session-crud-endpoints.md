---
key: MN-18
type: story
status: backlog
epic: MN-3
points: 5
priority: minor
labels: [MindNova]
relates:
  - key: MN-17
    why: "depends on the Session entity and migration"
  - key: MN-14
    why: "follows the same CRUD endpoint pattern (controller, service, DTOs)"
---

# Session CRUD API Endpoints

📌 Background

* With the Session model in place, the API needs endpoints to create, read,
  update, and manage session status. These are the primary operations for
  therapists recording visits and receptionists managing the schedule.

🎯 What's the Goal?

* As a therapist or receptionist,
* I want to create, view, update, and manage therapy session records via the API,
* So that every visit is documented and the schedule is maintained.

💡 Expected Value

* Core session management capability. Enables scheduling, recording outcomes,
  and tracking cancellations and no-shows.

✅ Success Criteria

* AC-1: POST /api/sessions with valid data returns the created session with
  Status = Scheduled, a generated Id, and CreatedAt set.
* AC-2: POST /api/sessions with a ClientId that does not reference an existing
  non-archived client returns a ProblemDetails error.
* AC-3: POST /api/sessions with a TherapistUserId that does not reference an
  existing ApplicationUser returns a ProblemDetails error.
* AC-4: POST /api/sessions with invalid data (missing required fields or
  DurationMinutes <= 0) returns a ProblemDetails error.
* AC-5: GET /api/sessions/{id} with a valid ID returns that session's full
  data.
* AC-6: GET /api/sessions/{id} with a non-existent ID returns a ProblemDetails
  error indicating not found.
* AC-7: GET /api/sessions returns a paginated list of sessions as a
  PagedResponse with Items, TotalCount, Page, PageSize.
* AC-8: PUT /api/sessions/{id} with valid data updates session fields and
  returns the updated resource with UpdatedAt refreshed.
* AC-9: PUT /api/sessions/{id} transitioning status from Scheduled to
  Completed succeeds.
* AC-10: PUT /api/sessions/{id} transitioning status from Scheduled to
  Cancelled succeeds.
* AC-11: PUT /api/sessions/{id} transitioning status from Scheduled to NoShow
  succeeds.
* AC-12: PUT /api/sessions/{id} transitioning status from Cancelled to
  Completed returns a ProblemDetails error (invalid transition).
* AC-13: PUT /api/sessions/{id} transitioning status from Completed to any
  other status returns a ProblemDetails error (completed is terminal).
* AC-14: PUT /api/sessions/{id} with a non-existent ID returns a ProblemDetails
  error indicating not found.
* AC-15: All endpoints require authentication; requests without a valid token
  receive a 401 response.

(Test traits: each AC covered by xUnit integration tests tagged
[Trait("Story","MN-18")] + [Trait("AC","AC-n")] using the SqlServerFixture
pattern.)

🛠️ How we'll do it

* Add SessionsController (thin) in MindNova.Api/Controllers/.
* Add ISessionService / SessionService in MindNova.Infrastructure/Services/.
* Use request/response DTOs (CreateSessionRequest, SessionResponse,
  UpdateSessionRequest).
* Status transition logic in the service layer with explicit allowed
  transitions.
* Validate ClientId and TherapistUserId exist before creating/updating.

⚠️ Risks & Blockers

* Depends on MN-17 (domain model).
* Role-based restrictions (e.g. therapists see only their own sessions) are
  out of scope; that comes with a future authorization story.

## Artifacts and references

* API contract - specs/sessions.openapi.yaml (covers MN-18, MN-19)
