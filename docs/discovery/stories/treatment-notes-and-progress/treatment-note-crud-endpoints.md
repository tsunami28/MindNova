---
key: MN-33
type: story
status: in-progress
epic: MN-5
points: 5
priority: minor
labels: [MindNova]
relates:
  - key: MN-32
    why: "depends on the TreatmentNote entity and migration"
  - key: MN-25
    why: "follows the same CRUD endpoint pattern (controller, service, DTOs)"
  - spec: specs/notes.openapi.yaml
    why: "contract for treatment note CRUD endpoints"
---

# Treatment Note CRUD Endpoints

📌 Background

* Therapists need to create, view, and update treatment notes for their sessions.
  Access is restricted: only the treating therapist or a supervisor can read or
  write notes for a session.

🎯 What's the Goal?

* As a therapist,
* I want to create, read, and update treatment notes via the API,
* So that clinical documentation is captured and maintained securely.

💡 Expected Value

* Clinical notes are recorded digitally with proper access control.

✅ Success Criteria

* AC-1: POST /api/sessions/{session_id}/notes with valid data creates a note
  linked to that session. The TherapistUserId is set from the authenticated user.
* AC-2: POST by a user who is not the session's therapist and not a Supervisor
  returns ProblemDetails (403 forbidden).
* AC-3: GET /api/sessions/{session_id}/notes/{id} returns the note.
* AC-4: GET by a non-therapist/non-supervisor for that session returns ProblemDetails (403).
* AC-5: PUT /api/notes/{id} updates the note's content fields (PresentingIssue,
  Interventions, Homework, ProgressRating, FreeText). Only the owning therapist
  or a Supervisor can update.
* AC-6: All endpoints require authentication.
* AC-7: Validation: ProgressRating must be 1-10; SessionId must reference an
  existing session.

🛠️ How we'll do it

* Add NotesController (sub-resource of sessions for create/get, top-level for update by ID).
* Add ITreatmentNoteService / TreatmentNoteService.
* Role-based access check in the service layer: compare authenticated user to
  Session.TherapistUserId or check Supervisor role.
* DTOs: CreateNoteRequest, UpdateNoteRequest, TreatmentNoteResponse.

⚠️ Risks & Blockers

* Depends on MN-32 (domain model).
* "Supervisor" access maps to the existing Admin role (no new role needed).

## Decisions and ADRs

* 2026-07-23: "Supervisor" role in ACs maps to existing Admin role (RoleSeeder seeds
  Admin, Therapist, Receptionist; no Supervisor role exists). Access rule: session's
  therapist OR user in Admin role.
* 2026-07-23: Sub-resource + top-level hybrid routing: create/list/get under
  /sessions/{session_id}/notes, update at /notes/{id} (caller already has the note ID).
* 2026-07-23: Forbidden access returns ProblemDetails with Status 403 via HTTP 200 (per C07).

## Artifacts and references

* API contract - specs/notes.openapi.yaml
* Controller - src/MindNova.Api/Controllers/NotesController.cs
* Service interface - src/MindNova.Infrastructure/Services/ITreatmentNoteService.cs
* Service implementation - src/MindNova.Infrastructure/Services/TreatmentNoteService.cs
* DTOs - src/MindNova.Api/Contracts/CreateNoteRequest.cs, UpdateNoteRequest.cs, TreatmentNoteResponse.cs
* DI registration - src/MindNova.Infrastructure/DependencyInjection.cs
* Tests - tests/MindNova.Api.Tests/Notes/NoteEndpointTests.cs
