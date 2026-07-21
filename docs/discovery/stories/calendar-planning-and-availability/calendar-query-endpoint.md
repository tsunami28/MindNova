---
key: MN-31
type: story
status: backlog
epic: MN-4
points: 5
priority: minor
labels: [MindNova]
relates:
  - key: MN-29
    why: "requires availability slots to expand into date entries"
  - key: MN-16
    why: "follows the same aggregation query pattern (timeline)"
---

# Calendar Query Endpoint

📌 Background

* Front-desk staff and therapists need a unified calendar view: availability slots
  and booked sessions merged for a given therapist and date range.

🎯 What's the Goal?

* As a therapist or scheduler,
* I want to query a merged calendar view for a therapist over a date range,
* So that I can see open slots and booked sessions in one call.

💡 Expected Value

* Single API call for the calendar UI. No client-side merging of separate endpoints.

✅ Success Criteria

* AC-1: GET /api/therapists/{therapistId}/calendar with date_from and date_to returns
  a list of CalendarEntry objects.
* AC-2: Each CalendarEntry includes Date, StartTime, EndTime, EntryType (Availability
  or Session), and SourceId.
* AC-3: Availability slots expand recurring rules into concrete date entries within
  the requested range.
* AC-4: Sessions within the range appear as Session-type entries.
* AC-5: An empty range returns an empty list (not an error).
* AC-6: Requires authentication.

🛠️ How we'll do it

* Add GetCalendar action on TherapistsController or a dedicated CalendarController.
* Service method fetches availability slots, expands recurring ones into date range,
  fetches sessions, merges and returns sorted list.
* CalendarEntryResponse DTO.
* No pagination (capped at max 90 days per request).

⚠️ Risks & Blockers

* Depends on MN-28 and MN-29 (availability) plus MN-3 (sessions, done).
* Expanding recurring slots for large date ranges needs sensible defaults (max 90 days).
