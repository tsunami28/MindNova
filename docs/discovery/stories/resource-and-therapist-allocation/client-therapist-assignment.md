---
key: MN-22
type: story
status: backlog
epic: MN-6
points: 5
priority: minor
labels: [MindNova]
relates:
  - key: MN-20
    why: "requires TherapistProfile entity to exist"
  - key: MN-21
    why: "requires therapist profiles to be created via CRUD"
  - key: MN-14
    why: "extends the Client entity with an assignment field"
---

# Client-Therapist Assignment

📌 Background

* Clients are assigned to therapists for ongoing treatment. The assignment must
  respect the therapist's max caseload. This is the core of the allocation epic.

🎯 What's the Goal?

* As a practice manager,
* I want to assign and reassign therapists to clients,
* So that workload is balanced and clients have a designated therapist.

💡 Expected Value

* Formalises client-therapist relationships. Enables caseload visibility and prevents
  therapist overload.

✅ Success Criteria

* AC-1: POST /api/clients/{clientId}/therapist with a valid TherapistProfileId assigns
  the therapist and returns the updated client.
* AC-2: Assigning a therapist whose current caseload equals MaxCaseload returns a
  ProblemDetails error indicating capacity exceeded.
* AC-3: Reassigning (client already has a therapist) replaces the previous assignment.
* AC-4: DELETE /api/clients/{clientId}/therapist removes the assignment (unassigns).
* AC-5: GET /api/clients/{id} includes the assigned TherapistProfileId (null if
  unassigned).
* AC-6: A non-existent client ID returns ProblemDetails (404).
* AC-7: A non-existent or inactive TherapistProfileId returns ProblemDetails error.
* AC-8: All endpoints require authentication.

🛠️ How we'll do it

* Add AssignedTherapistId (Guid?) property to Client entity
  (src/MindNova.Domain/Entities/Client.cs). Add a migration.
* Add assign/unassign actions to ClientsController (sub-resource pattern).
* Validation in service layer: count clients where AssignedTherapistId matches,
  compare against TherapistProfile.MaxCaseload.
* Update ClientResponse DTO to include AssignedTherapistId.

⚠️ Risks & Blockers

* Depends on MN-20 and MN-21 (therapist profiles must exist to assign).
* Race condition on caseload check under concurrent requests; acceptable for V1
  (single-tenant, low concurrency).
