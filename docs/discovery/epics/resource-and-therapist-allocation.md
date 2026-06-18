---
key: MN-6
type: epic
status: backlog
priority: minor
labels: [MindNova]
relates:
  - key: MN-2
    why: "therapist allocation targets client records"
---

# Resource and Therapist Allocation

📌 Background

* Consultancies assign therapists to clients based on specialisation, availability,
  and workload. Without tooling, this is ad-hoc and error-prone.

🎯 What's the Goal?

* As a practice manager,
* I want to manage therapist profiles (specialisations, caseload capacity) and
  allocate them to clients,
* So that workload is balanced and clients are matched to the right therapist.

💡 Expected Value

* Reduces scheduling conflicts and therapist overload. Improves client-therapist
  matching.

✅ Success Criteria

* Therapist profile CRUD (specialisations, max caseload, active/inactive).
* Assign/reassign therapists to clients.
* Caseload dashboard showing current allocation vs. capacity.

🛠️ How we'll do it

* Define therapist profile domain model (specialisations as a value collection,
  caseload capacity as an integer).
* Build therapist profile CRUD endpoints.
* Add client-therapist assignment endpoints with validation against max caseload.
* Create a caseload summary query endpoint.

⚠️ Risks & Blockers

* Depends on MN-2 (clients) for the assignment target.
