---
key: MN-31
type: story
status: in-progress
epic: MN-4
points: 5
priority: minor
labels: [MindNova]
relates:
  - key: MN-29
    why: "requires availability slots to expand into date entries"
  - key: MN-16
    why: "follows the same aggregation query pattern (timeline)"
  - spec: specs/calendar.openapi.yaml
    why: "contract for the merged calendar query endpoint"
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

## Decisions and ADRs

* 2026-07-21: Dedicated CalendarController and spec (specs/calendar.openapi.yaml) rather than
  adding to TherapistsController - calendar is a read-only aggregation crossing sessions and
  availability, warranting its own domain per C07.
* 2026-07-21: CalendarEntry is a flat shape with EntryType enum (Availability, Session) and
  SourceId linking back to the source record. No nested types per entry type.
* 2026-07-21: Both date_from and date_to are required; range capped at 90 days server-side.
  No pagination (bounded response size).

## Artifacts and references

* API contract - specs/calendar.openapi.yaml
* Domain model - src/MindNova.Domain/Entities/CalendarEntry.cs
* Service interface - src/MindNova.Infrastructure/Services/ICalendarService.cs
* Service implementation - src/MindNova.Infrastructure/Services/CalendarService.cs
* Controller - src/MindNova.Api/Controllers/CalendarController.cs
* DTOs - src/MindNova.Api/Contracts/CalendarEntryResponse.cs, CalendarResponse.cs
* DI registration - src/MindNova.Infrastructure/DependencyInjection.cs
* Tests - tests/MindNova.Api.Tests/Calendar/CalendarEndpointTests.cs
* PR - https://github.com/tsunami28/MindNova/pull/26
