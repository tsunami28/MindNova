---
key: MN-22
type: story
status: backlog
epic: MN-4
points: 5
priority: minor
labels: [MindNova]
relates:
  - key: MN-21
    why: "uses availability data for validation"
  - key: MN-18
    why: "validates sessions against availability when creating/updating"
---

# Session Conflict Detection

📌 Background

* Without conflict detection, sessions can be booked into slots where the
  therapist is unavailable or already occupied. This story adds validation
  that prevents double-booking and out-of-availability scheduling.

🎯 What's the Goal?

* As a scheduler,
* I want the system to reject session bookings that conflict with existing
  sessions or fall outside therapist availability,
* So that double-bookings and scheduling errors are prevented.

💡 Expected Value

* Prevents the most common scheduling mistake (double-booking). Ensures the
  calendar is trustworthy.

✅ Success Criteria

* AC-1: POST /api/sessions with a time that overlaps an existing session for
  the same therapist returns a ProblemDetails error indicating a scheduling
  conflict.
* AC-2: POST /api/sessions with a time that partially overlaps an existing
  session (starts before it ends) returns a ProblemDetails conflict error.
* AC-3: POST /api/sessions with a time that falls outside all availability
  blocks for that therapist returns a ProblemDetails error indicating no
  availability.
* AC-4: POST /api/sessions with a time that fits within a recurring
  availability block (matching DayOfWeek and time range) is accepted.
* AC-5: POST /api/sessions with a time that fits within a one-off
  availability block (matching SpecificDate and time range) is accepted.
* AC-6: PUT /api/sessions/{id} changing ScheduledAt to a time that overlaps
  another session returns a ProblemDetails conflict error.
* AC-7: PUT /api/sessions/{id} changing ScheduledAt to a time outside
  availability returns a ProblemDetails error.
* AC-8: PUT /api/sessions/{id} changing ScheduledAt to a valid slot succeeds
  normally.
* AC-9: A session that exactly fills an availability block (same start and
  end time) is accepted without conflict.
* AC-10: Two sessions for different therapists at the same time do not
  conflict with each other.

(Test traits: each AC covered by xUnit integration tests tagged
[Trait("Story","MN-22")] + [Trait("AC","AC-n")] using the SqlServerFixture
pattern.)

🛠️ How we'll do it

* Add a ConflictDetectionService (or extend SessionService) that queries
  existing sessions and availability blocks for the therapist and proposed
  time window.
* Integrate the check into the create and update flows in SessionsController.
* Availability matching: for recurring blocks, match DayOfWeek + time range;
  for one-off blocks, match SpecificDate + time range.

⚠️ Risks & Blockers

* Depends on MN-21 (availability CRUD) for availability data to exist.
* Edge case: sessions spanning midnight or multi-day blocks are out of scope
  for V1 (all sessions assumed same-day).
