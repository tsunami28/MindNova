---
key: MN-30
type: story
status: in-progress
epic: MN-4
points: 5
priority: minor
labels: [MindNova]
relates:
  - key: MN-28
    why: "validates proposed sessions against availability slots"
  - key: MN-29
    why: "requires availability slots to exist for validation"
  - key: MN-18
    why: "modifies session creation/update logic in SessionService"
  - spec: specs/sessions.openapi.yaml
    why: "contract for conflict and availability validation on session create/update"
---

# Session Conflict Detection

📌 Background

* When creating or updating a session, the system must verify the proposed time
  does not overlap an existing booking for the same therapist, and falls within
  a defined availability slot.

🎯 What's the Goal?

* As a scheduler,
* I want the system to reject session bookings that conflict with existing sessions
  or fall outside therapist availability,
* So that double-bookings are prevented.

💡 Expected Value

* Eliminates scheduling errors. Therapists trust the system to protect their time.

✅ Success Criteria

* AC-1: POST /api/sessions rejects a session that overlaps an existing session for
  the same therapist (same ScheduledAt + DurationMinutes window).
* AC-2: The overlap error returns ProblemDetails with a clear conflict message.
* AC-3: POST /api/sessions rejects a session outside the therapist's availability slots.
* AC-4: A session within an availability slot and with no time overlap succeeds.
* AC-5: PUT /api/sessions/{id} validates the new time against conflicts too.
* AC-6: Existing tests for session creation still pass (no regression).

🛠️ How we'll do it

* Add conflict-check logic to SessionService.CreateAsync and UpdateAsync.
* Query existing sessions for the same therapist in the proposed time window.
* Query availability slots to confirm the proposed time is covered.
* Return specific error messages distinguishing "conflict" from "no availability".

⚠️ Risks & Blockers

* Depends on MN-28 (availability model) and MN-29 (slots must exist to validate against).
* Modifying SessionService changes an existing, tested component; run full regression.

## Decisions and ADRs

* 2026-07-21: API contract defines two distinct ProblemDetails error types for conflict detection -
  "urn:mindnova:error:session-conflict" (with ConflictDetail payload carrying the conflicting
  session's ID and time window) and "urn:mindnova:error:outside-availability" - on the existing
  POST /sessions and PUT /sessions/{id} operations. No new endpoints; validation is internal to
  the sessions domain. Spec version bumped to 1.1.0.
* 2026-07-21: Availability check is graceful - skipped when no TherapistProfile or no
  AvailabilitySlots exist for the therapist. This makes the feature additive (only enforced
  when availability is configured) and avoids breaking existing session creation flows.

## Artifacts and references

* API contract - specs/sessions.openapi.yaml (v1.1.0, conflict detection additions)
* Service implementation - src/MindNova.Infrastructure/Services/SessionService.cs (CreateAsync, UpdateAsync, FindConflictingSessionAsync, CheckAvailabilityCoverageAsync)
* Controller updates - src/MindNova.Api/Controllers/SessionsController.cs (MapConflictError)
* Interface change - src/MindNova.Infrastructure/Services/ISessionService.cs (CreateAsync return type)
* Tests - tests/MindNova.Api.Tests/Sessions/SessionConflictTests.cs
