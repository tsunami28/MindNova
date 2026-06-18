---
key: MN-5
type: epic
status: backlog
priority: minor
labels: [MindNova]
relates:
  - key: MN-3
    why: "notes are attached to session records"
---

# Treatment Notes and Progress

📌 Background

* Therapists document session observations, treatment plans, and client progress.
  These notes are clinically sensitive and must be secured accordingly.

🎯 What's the Goal?

* As a therapist,
* I want to write, store, and retrieve treatment notes linked to individual sessions,
* So that clinical documentation is complete, searchable, and secure.

💡 Expected Value

* Continuity of care across therapist handoffs. Audit trail for compliance.

✅ Success Criteria

* Create/edit treatment notes attached to a session record.
* Notes support structured fields (presenting issue, interventions, homework,
  progress rating) and free text.
* Notes are visible only to the treating therapist and authorised supervisors.
* Notes are soft-deletable (audit trail preserved).

🛠️ How we'll do it

* Define treatment note domain model linked to session entity.
* Build note CRUD endpoints with role-based access (therapist + supervisor only).
* Implement soft-delete with audit columns (deleted by, deleted at).
* Add query endpoints for notes by client and by session.

⚠️ Risks & Blockers

* Depends on MN-3 (sessions).
* Encryption-at-rest requirements may need a spike if regulatory guidance demands
  field-level encryption beyond database-level TDE.
