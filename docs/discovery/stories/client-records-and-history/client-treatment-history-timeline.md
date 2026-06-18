---
key: MN-16
type: story
status: backlog
epic: MN-2
points: 5
priority: minor
labels: [MindNova]
relates:
  - key: MN-14
    why: "depends on client endpoints and service layer"
  - key: MN-3
    why: "aggregates session data once sessions epic ships"
  - key: MN-5
    why: "aggregates treatment note data once notes epic ships"
---

# Client Treatment History Timeline

📌 Background

* A therapist reviewing a client's record needs to see a chronological timeline of
  all interactions: sessions attended, notes written, and status changes. This is
  a read-only aggregation view.

🎯 What's the Goal?

* As a therapist,
* I want to view a chronological timeline of a client's treatment history,
* So that I have full context before and during a session.

💡 Expected Value

* Complete client context in one view. Reduces time spent searching across
  separate session and note records.

✅ Success Criteria

* GET /api/clients/{id}/timeline returns an ordered list of timeline events.
* Each event includes: date, type (session, note, status change), summary, and
  a link/ID to the source record.
* Results are paginated (newest first) with configurable page size.
* Returns an empty list (not an error) when no history exists yet.
* Endpoint is extensible: adding new event types (from future epics) requires
  only a new event source, not endpoint changes.
* Tests: empty timeline, mixed event types ordering, pagination.

🛠️ How we'll do it

* Define a TimelineEvent DTO (Date, EventType enum, Summary, SourceId).
* Add ITimelineService that queries session and note repositories and merges
  results chronologically.
* Initially returns empty or session-only data until MN-3 (sessions) and MN-5
  (notes) ship.
* Design the service with an ITimelineEventSource interface so new sources plug in
  without modifying the aggregation logic.

⚠️ Risks & Blockers

* Full functionality depends on MN-3 (sessions) and MN-5 (notes) for source data.
  The endpoint structure and aggregation logic ship now; real data populates later.
* Performance at scale (many events per client) may need cursor-based pagination;
  offset pagination is acceptable for V1.
