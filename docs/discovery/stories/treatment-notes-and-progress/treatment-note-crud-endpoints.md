---
key: MN-33
type: story
status: backlog
epic: MN-5
points: 5
priority: minor
labels: [MindNova]
relates:
  - key: MN-32
    why: "depends on the TreatmentNote entity and migration"
  - key: MN-25
    why: "follows the same CRUD endpoint pattern (controller, service, DTOs)"
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
* Role "Supervisor" must exist in the identity system (seeded by RoleSeeder, already present).
