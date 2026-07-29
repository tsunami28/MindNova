---
key: MN-37
type: story
status: backlog
epic: MN-7
points: 3
priority: minor
labels: [MindNova]
relates:
  - key: MN-36
    why: "follows the same reporting pattern (practice stats)"
  - key: MN-27
    why: "extends the per-therapist view from the caseload dashboard"
  - spec: specs/reports.openapi.yaml
    why: "contract for therapist statistics endpoint"
---

# Session Statistics by Therapist Endpoint

📌 Background

* Practice managers need a per-therapist breakdown of session activity to
  identify utilisation imbalances and support staffing decisions.

🎯 What's the Goal?

* As a practice manager,
* I want to query session statistics broken down by therapist for a date range,
* So that I can see which therapists are over- or under-utilised.

💡 Expected Value

* Identifies utilisation imbalances. Supports equitable caseload distribution.

✅ Success Criteria

* AC-1: GET /api/reports/therapist-stats with date_from and date_to returns a
  list of TherapistStatEntry objects.
* AC-2: Each entry includes TherapistUserId, TherapistName, TotalSessions,
  CompletedCount, NoShowCount, CancelledCount, AvailableSlotCount,
  UtilisationRate (sessions / available slots as percentage).
* AC-3: AvailableSlotCount is computed from expanded availability slots within
  the date range (same expansion logic as the calendar endpoint).
* AC-4: An empty date range returns an empty list (not an error).
* AC-5: Requires authentication.

🛠️ How we'll do it

* Add GetTherapistStats action to ReportsController.
* Query Sessions grouped by TherapistUserId, AvailabilitySlots expanded for the
  date range, and TherapistProfiles for display names.
* Reuse the slot expansion pattern from CalendarService.

⚠️ Risks & Blockers

* Depends on MN-36 (reporting pattern established first).

## Artifacts and references

* API contract - specs/reports.openapi.yaml
