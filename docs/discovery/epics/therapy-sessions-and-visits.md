---
key: MN-3
type: epic
status: backlog
priority: minor
labels: [MindNova]
relates:
  - key: MN-2
    why: "sessions link to client records"
  - key: MN-6
    why: "sessions reference therapist profiles from allocation"
---

# Therapy Sessions and Visits

📌 Background

* Tracking which therapist saw which client, when, for how long, and under what
  treatment type is core operational data for any consultancy.

🎯 What's the Goal?

* As a therapist,
* I want to schedule, record, and review therapy sessions linked to client records,
* So that every visit is documented and billable time is tracked.

💡 Expected Value

* Accurate session records for billing, compliance, and continuity of care.

✅ Success Criteria

* Create/edit/cancel session records linked to a client and therapist.
* Session includes: date/time, duration, session type, status (scheduled,
  completed, cancelled, no-show).
* Session history viewable per client and per therapist.

🛠️ How we'll do it

* Define session domain model with foreign keys to client and therapist entities.
* Build session CRUD API endpoints.
* Add query endpoints for session history filtered by client or therapist.
* Include status transitions with validation (e.g. cannot complete a cancelled session).

⚠️ Risks & Blockers

* Depends on MN-2 (client records) and MN-6 (therapist allocation) for linked
  entities.
