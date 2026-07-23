---
key: MN-35
type: story
status: backlog
epic: MN-5
points: 3
priority: minor
labels: [MindNova]
relates:
  - key: MN-33
    why: "soft-delete is exercised through the CRUD endpoints"
  - key: MN-34
    why: "query endpoints must respect soft-delete by default"
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
