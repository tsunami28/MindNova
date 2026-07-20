---
key: MN-16
type: story
status: in-progress
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

* AC-1: GET /api/clients/{id}/timeline for a client with sessions returns a
  PagedResponse<TimelineEvent> containing events ordered by Date descending
  (newest first).
* AC-2: Each TimelineEvent contains Date (DateTime), EventType (string),
  Summary (string), and SourceId (Guid).
* AC-3: A session event has EventType = "Session", Date = the session's
  ScheduledAt, SourceId = the session's Id, and a Summary describing the
  session type and status.
* AC-4: GET /api/clients/{id}/timeline for a client with no sessions returns
  a PagedResponse with an empty Items list and TotalCount = 0 (not an error).
* AC-5: GET /api/clients/{id}/timeline with a non-existent client ID returns
  a ProblemDetails error indicating not found.
* AC-6: Results are paginated via page and page_size query parameters with
  default page = 1, default page_size = 20, page_size clamped to 1-100.
* AC-7: When multiple pages of events exist, the response TotalCount reflects
  the full count and Items contains only the requested page.
* AC-8: GET /api/clients/{id}/timeline for an archived client still returns
  its timeline (archived clients remain retrievable).
* AC-9: The endpoint requires authentication; requests without a valid token
  receive a 401 response.
* AC-10: The timeline service uses an ITimelineEventSource abstraction so new
  event types can be added without modifying the aggregation logic.

Test trait mapping:
- AC-1: `[Trait("Story","MN-16")]` + `[Trait("AC","AC-1")]` - integration test;
  asserts descending order by Date.
- AC-2: `[Trait("Story","MN-16")]` + `[Trait("AC","AC-2")]` - integration test;
  asserts all DTO properties present and typed correctly.
- AC-3: `[Trait("Story","MN-16")]` + `[Trait("AC","AC-3")]` - integration test;
  creates a session, fetches timeline, asserts event fields match.
- AC-4: `[Trait("Story","MN-16")]` + `[Trait("AC","AC-4")]` - integration test;
  client with no sessions returns empty Items.
- AC-5: `[Trait("Story","MN-16")]` + `[Trait("AC","AC-5")]` - integration test;
  asserts ProblemDetails on non-existent client.
- AC-6: `[Trait("Story","MN-16")]` + `[Trait("AC","AC-6")]` - integration test;
  asserts defaults and clamping behavior.
- AC-7: `[Trait("Story","MN-16")]` + `[Trait("AC","AC-7")]` - integration test;
  seeds multiple sessions, requests page 2, asserts correct slice.
- AC-8: `[Trait("Story","MN-16")]` + `[Trait("AC","AC-8")]` - integration test;
  archives client, fetches timeline, asserts 200.
- AC-9: `[Trait("Story","MN-16")]` + `[Trait("AC","AC-9")]` - integration test;
  unauthenticated request asserts 401.
- AC-10: `[Trait("Story","MN-16")]` + `[Trait("AC","AC-10")]` - unit test;
  registers a second ITimelineEventSource, asserts aggregation includes events
  from both sources.

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

## Artifacts and references

* API contract - specs/clients.openapi.yaml (timeline endpoint: GET /clients/{id}/timeline)
* Controller action - src/MindNova.Api/Controllers/ClientsController.cs (GetTimeline)
* Domain model - src/MindNova.Domain/Entities/TimelineEvent.cs
* Response DTO - src/MindNova.Api/Contracts/TimelineEventResponse.cs
* Service interface - src/MindNova.Infrastructure/Services/ITimelineService.cs
* Service implementation - src/MindNova.Infrastructure/Services/TimelineService.cs
* Event source interface - src/MindNova.Infrastructure/Services/ITimelineEventSource.cs
* Session event source - src/MindNova.Infrastructure/Services/SessionTimelineEventSource.cs
* Integration tests - tests/MindNova.Api.Tests/Clients/ClientTimelineEndpointTests.cs
* Unit tests - tests/MindNova.Api.Tests/Timeline/TimelineServiceTests.cs
