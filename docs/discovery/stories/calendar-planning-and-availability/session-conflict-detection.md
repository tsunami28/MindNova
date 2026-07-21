---
key: MN-30
type: story
status: backlog
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
