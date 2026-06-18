---
key: MN-2
type: epic
status: backlog
priority: high
labels: [MindNova]
relates:
  - key: MN-8
    why: "data model depends on database technology selection"
---

# Client Records and History

📌 Background

* Psychotherapy consultancies need a central, secure store for client demographic
  data and treatment history. Today this is typically paper-based or in
  disconnected spreadsheets.

🎯 What's the Goal?

* As a therapist or office administrator,
* I want to create, view, update, and search client records with their full
  treatment history,
* So that client information is always accessible, accurate, and auditable.

💡 Expected Value

* Eliminates manual record-keeping. Provides a single source of truth for every
  client interaction. Enables downstream features (sessions, notes, reporting).

✅ Success Criteria

* CRUD operations for client demographic records (name, contact, date of birth,
  emergency contact).
* Client search by name or identifier.
* Treatment history timeline visible per client.
* Access restricted to authenticated users with appropriate roles.

🛠️ How we'll do it

* Define the client domain model and EF Core entity configuration.
* Build client CRUD API endpoints following MindNova controller conventions.
* Add search endpoint with filtering and pagination.
* Wire treatment history as a read-only aggregation of session and note data from
  downstream epics.

⚠️ Risks & Blockers

* Data model depends on MN-8 (database selection).
* Privacy/GDPR requirements may drive additional encryption or consent tracking,
  not yet scoped.
