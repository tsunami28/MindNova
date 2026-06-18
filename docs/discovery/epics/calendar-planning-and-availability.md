---
key: MN-4
type: epic
status: backlog
priority: minor
labels: [MindNova]
relates:
  - key: MN-3
    why: "calendar displays booked sessions from the sessions epic"
---

# Calendar Planning and Availability

📌 Background

* Therapists manage complex schedules with recurring slots, leave, and variable
  availability across locations.

🎯 What's the Goal?

* As a therapist or scheduler,
* I want to define therapist availability windows and view a shared calendar,
* So that sessions can only be booked into genuinely open slots.

💡 Expected Value

* Prevents double-booking. Gives front-desk staff a real-time view of openings.

✅ Success Criteria

* Therapists can set recurring and one-off availability blocks.
* Calendar view displays availability and booked sessions.
* Conflict detection prevents overlapping bookings.

🛠️ How we'll do it

* Define availability model (recurring rules + exception overrides).
* Build availability CRUD endpoints per therapist.
* Add conflict-detection logic that checks proposed sessions against availability
  and existing bookings.
* Provide a calendar query endpoint returning merged availability + session data
  for a given date range.

⚠️ Risks & Blockers

* Depends on MN-3 (sessions) for booked-slot data.
* External calendar sync (Outlook/Google) is out of scope for V1.
