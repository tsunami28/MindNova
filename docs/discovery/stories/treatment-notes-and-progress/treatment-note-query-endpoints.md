---
key: MN-34
type: story
status: backlog
epic: MN-5
points: 3
priority: minor
labels: [MindNova]
relates:
  - key: MN-33
    why: "depends on note CRUD (notes must exist to query)"
  - key: MN-19
    why: "follows the same query/filtering pattern (session history)"
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
