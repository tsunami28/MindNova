---
key: MN-34
type: story
status: in-progress
epic: MN-5
points: 3
priority: minor
labels: [MindNova]
relates:
  - key: MN-33
    why: "depends on note CRUD (notes must exist to query)"
  - key: MN-19
    why: "follows the same query/filtering pattern (session history)"
  - spec: specs/notes.openapi.yaml
    why: "contract for client-scoped notes query endpoint (v1.1.0)"
---

# Treatment Note Query Endpoints

📌 Background

* Therapists and supervisors need to view notes across sessions for a given client
  (longitudinal view) or filter notes by date range.

🎯 What's the Goal?

* As a therapist or supervisor,
* I want to list treatment notes by client or by session with date filtering,
* So that I can review a client's progress history in one view.

💡 Expected Value

* Single API call for the notes timeline. Supports clinical review and handoffs.

✅ Success Criteria

* AC-1: GET /api/clients/{client_id}/notes returns all notes for sessions
  belonging to that client, sorted by session date descending.
* AC-2: Supports optional date_from and date_to query params to filter by
  session ScheduledAt date.
* AC-3: Supports pagination (page, page_size) with the same defaults as other
  list endpoints (page=1, page_size=20, max 100).
* AC-4: Only the treating therapist(s) and Supervisors can access a client's notes.
* AC-5: Deleted notes are excluded by default.
* AC-6: Requires authentication.

🛠️ How we'll do it

* Add a list action to NotesController or a dedicated endpoint on ClientsController.
* Query joins TreatmentNotes -> Sessions -> filter by ClientId.
* Reuse paging pattern from existing list endpoints.
* Access check: user is a therapist for that client or has Supervisor role.

⚠️ Risks & Blockers

* Depends on MN-33 (note CRUD must exist first).

## Decisions and ADRs

* 2026-07-28: Client-scoped query lives in the notes domain (specs/notes.openapi.yaml v1.1.0),
  not the clients domain, since it returns notes and the access control is the notes pattern
  (therapist or Admin).
* 2026-07-28: Paged response (PagedNoteResponse) with the same defaults as all list endpoints
  (page=1, page_size=20, max 100).

## Artifacts and references

* API contract - specs/notes.openapi.yaml (v1.1.0, client-scoped query addition)
* Service method - src/MindNova.Infrastructure/Services/TreatmentNoteService.cs (ListByClientAsync)
* Interface addition - src/MindNova.Infrastructure/Services/ITreatmentNoteService.cs
* Controller action - src/MindNova.Api/Controllers/NotesController.cs (ListByClient)
* Tests - tests/MindNova.Api.Tests/Notes/NoteQueryTests.cs
* PR - https://github.com/tsunami28/MindNova/pull/31
