---
key: MN-23
type: story
status: backlog
epic: MN-4
points: 3
priority: minor
labels: [MindNova]
relates:
  - key: MN-21
    why: "queries availability blocks"
  - key: MN-19
    why: "queries sessions (follows the same filtering pattern)"
---

# Calendar Query Endpoint

📌 Background

* Front-desk staff and therapists need a unified view of a therapist's
  schedule: which slots are available, which are booked, and which are blocked.
  This is the read-only calendar view.

🎯 What's the Goal?

* As a receptionist or therapist,
* I want to query a calendar view for a therapist and date range showing
  availability and booked sessions together,
* So that I can see at a glance where openings exist.

💡 Expected Value

* Single API call for the calendar UI. Reduces the need for the client to
  merge availability and session data themselves.

✅ Success Criteria

* AC-1: GET /api/calendar?therapist_id=<id>&date_from=<date>&date_to=<date>
  returns a list of calendar entries sorted by Date then StartTime.
* AC-2: A recurring availability block within the date range produces one
  Available entry per matching day (e.g. every Monday in the range).
* AC-3: A one-off availability block whose SpecificDate falls within the date
  range produces a single Available entry.
* AC-4: A recurring block whose EffectiveTo is before the queried range
  produces no entries.
* AC-5: A booked session within the date range produces a Booked entry with
  the SessionId populated.
* AC-6: Each entry has Date, StartTime, EndTime, EntryType (Available or
  Booked), and SessionId (present only for Booked).
* AC-7: A therapist with no availability and no sessions in the range returns
  an empty list (not an error).
* AC-8: Available and Booked entries for the same day are both returned
  (the endpoint merges, it does not subtract).
* AC-9: Requests without a valid authentication token receive a 401 response.

(Test traits: each AC covered by xUnit integration tests tagged
[Trait("Story","MN-23")] + [Trait("AC","AC-n")] using the SqlServerFixture
pattern.)

🛠️ How we'll do it

* Add a CalendarController with a single GET endpoint.
* Add ICalendarService that queries availability blocks and sessions for the
  therapist/date range, expands recurring rules into concrete dates, and
  merges into a sorted list of CalendarEntry DTOs.
* Reuse existing IAvailabilityService and ISessionService for data retrieval.

⚠️ Risks & Blockers

* Depends on MN-21 (availability data) and MN-3 (session data, already done).
* Expanding recurring rules into concrete dates requires careful handling of
  EffectiveFrom/EffectiveTo boundaries.

## Artifacts and references

* API contract - specs/calendar.openapi.yaml
