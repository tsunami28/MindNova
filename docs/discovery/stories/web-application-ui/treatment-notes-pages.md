---
key: MN-43
type: story
status: in-progress
epic: MN-8
points: 5
priority: minor
labels: [MindNova]
relates:
  - key: MN-40
    why: "depends on the Blazor project scaffold and auth"
  - key: MN-33
    why: "consumes the treatment note CRUD API endpoints"
  - key: MN-34
    why: "consumes the client-scoped notes query endpoint"
---

# Treatment Notes Pages

📌 Background

* Therapists need to write and review treatment notes for their sessions.
  Notes are access-controlled (therapist or Admin only).

🎯 What's the Goal?

* As a therapist,
* I want to create, view, and manage treatment notes through the web interface,
* So that clinical documentation is captured during or after sessions.

💡 Expected Value

* Digital clinical documentation. Replaces paper notes or external tools.

✅ Success Criteria

* AC-1: Notes list on the session detail page showing all notes for that session.
* AC-2: Create note form with structured fields (PresentingIssue, Interventions,
  Homework, ProgressRating slider 1-10, FreeText).
* AC-3: Edit note form pre-populated with current data.
* AC-4: Client notes timeline page (GET /api/clients/{id}/notes) with date filtering.
* AC-5: Soft-delete with confirmation dialog (shows warning about audit trail).
* AC-6: Deleted notes hidden by default; Admin toggle to show deleted notes.
* AC-7: Access denied message for non-therapist/non-Admin users.

🛠️ How we'll do it

* Add Pages/Notes/ with SessionNotes.razor, CreateNote.razor, EditNote.razor,
  ClientNotes.razor.
* MudForm with ProgressRating as MudSlider.
* Confirmation dialog (MudDialog) for soft-delete.
* Role check in UI to show/hide Admin-only features.

⚠️ Risks & Blockers

* Depends on MN-40 (scaffold) and MN-42 (session pages for navigation context).

## Artifacts and references

* Models - src/MindNova.Web/Models/NoteModels.cs
* API service - src/MindNova.Web/Services/NoteApiService.cs
* Session notes page - src/MindNova.Web/Pages/Notes/SessionNotes.razor
* Create note page - src/MindNova.Web/Pages/Notes/CreateNote.razor
* Edit note page - src/MindNova.Web/Pages/Notes/EditNote.razor
* Client notes page - src/MindNova.Web/Pages/Notes/ClientNotes.razor
* Delete dialog - src/MindNova.Web/Shared/DeleteConfirmDialog.razor
* PR - https://github.com/tsunami28/MindNova/pull/42
