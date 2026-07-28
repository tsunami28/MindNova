---
key: MN-35
type: story
status: done
epic: MN-5
points: 3
priority: minor
labels: [MindNova]
relates:
  - key: MN-33
    why: "soft-delete is exercised through the CRUD endpoints"
  - key: MN-34
    why: "query endpoints must respect soft-delete by default"
  - spec: specs/notes.openapi.yaml
    why: "contract for soft-delete and include_deleted additions (v1.2.0)"
---

# Treatment Note Soft-Delete and Audit

📌 Background

* Clinical notes must never be permanently destroyed for compliance reasons.
  Soft-delete preserves the audit trail while hiding deleted notes from normal views.

🎯 What's the Goal?

* As a supervisor or compliance officer,
* I want deleted notes to be preserved with an audit trail (who deleted, when),
* So that the system maintains a complete clinical record for regulatory compliance.

💡 Expected Value

* Compliance: no data loss on deletion. Supervisors can review deleted notes if needed.

✅ Success Criteria

* AC-1: DELETE /api/notes/{id} sets IsDeleted=true, DeletedAt=now, DeletedByUserId
  to the authenticated user. Does not remove the row.
* AC-2: Only the owning therapist or a Supervisor can delete.
* AC-3: Deleted notes are excluded from GET /api/sessions/{id}/notes and
  GET /api/clients/{id}/notes by default.
* AC-4: GET /api/sessions/{id}/notes?include_deleted=true (Supervisor only) returns
  all notes including soft-deleted ones.
* AC-5: A deleted note can still be retrieved by ID (GET /api/notes/{id}) by a
  Supervisor, with a flag indicating it is deleted.
* AC-6: Attempting to update a deleted note returns ProblemDetails.

🛠️ How we'll do it

* Add Delete action to NotesController that soft-deletes.
* Add include_deleted query param to list endpoints (Supervisor-gated).
* Filter out IsDeleted=true in default queries.
* Block updates on deleted notes in the service layer.

⚠️ Risks & Blockers

* Depends on MN-33 (CRUD) and MN-34 (query endpoints).

## Decisions and ADRs

* 2026-07-28: "Supervisor" maps to Admin role (consistent with MN-33 decision).
* 2026-07-28: Soft-delete returns the note with IsDeleted=true in the response body (so
  the caller sees confirmation of what happened).
* 2026-07-28: GET /notes/{id} for Admin returns deleted notes with IsDeleted flag visible;
  non-Admin users get a 404 for deleted notes.
* 2026-07-28: include_deleted param is Admin-gated: non-Admin users always get filtered results
  regardless of the param value.
* 2026-07-28: AC-3 (exclude deleted from queries) and AC-6 (block update on deleted) are
  already implemented in MN-33/MN-34; this story adds the DELETE action and include_deleted param.

## Artifacts and references

* API contract - specs/notes.openapi.yaml (v1.2.0, soft-delete and include_deleted additions)
* Service methods - src/MindNova.Infrastructure/Services/TreatmentNoteService.cs (DeleteAsync, GetByNoteIdAsync, ListBySessionAsync overload)
* Interface additions - src/MindNova.Infrastructure/Services/ITreatmentNoteService.cs
* Controller actions - src/MindNova.Api/Controllers/NotesController.cs (Delete, GetByNoteId, ListBySession include_deleted)
* Tests - tests/MindNova.Api.Tests/Notes/NoteSoftDeleteTests.cs
* PR - https://github.com/tsunami28/MindNova/pull/32
