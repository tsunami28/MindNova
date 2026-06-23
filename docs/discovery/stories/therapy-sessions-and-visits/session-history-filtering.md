---
key: MN-19
type: story
status: in-progress
epic: MN-3
points: 3
priority: minor
labels: [MindNova]
relates:
  - key: MN-18
    why: "extends the session list endpoint with filtering and pagination"
  - key: MN-15
    why: "follows the same search and pagination pattern"
---

# Session History and Filtering

📌 Background

* Therapists need to review their session history, and office staff need to
  look up sessions by client, therapist, status, or date range. The basic
  GET /api/sessions list is insufficient without filtering capabilities.

🎯 What's the Goal?

* As a therapist or practice manager,
* I want to filter sessions by client, therapist, status, and date range, and
  page through results,
* So that I can quickly find relevant session records.

💡 Expected Value

* Enables per-client and per-therapist session history views. Supports
  reporting workflows and schedule review.

✅ Success Criteria

* GET /api/sessions?client_id=<guid> filters sessions by client.
* GET /api/sessions?therapist_id=<id> filters sessions by therapist.
* GET /api/sessions?status=<status> filters by session status.
* GET /api/sessions?date_from=<date>&date_to=<date> filters by date range.
* Filters are combinable (e.g. client_id + status + date range).
* Pagination via page and page_size query parameters, returning
  PagedResponse with TotalCount, Page, PageSize.
* Returns an empty Items list (not an error) when no sessions match.

🛠️ How we'll do it

* Extend the existing GET /api/sessions endpoint in SessionsController with
  query parameters (snake_case per convention).
* Add filter composition in SessionService using IQueryable.
* Reuse the PagedResponse<T> wrapper from MN-15.

⚠️ Risks & Blockers

* Depends on MN-18 (CRUD endpoints and service layer).
* Complex date range queries may need indexed columns on ScheduledAt; the
  MN-17 configuration should include this index.

## Artifacts and references

* API contract - specs/sessions.openapi.yaml (listSessions operation)
* Integration tests - tests/MindNova.Api.Tests/Sessions/SessionFilterTests.cs
