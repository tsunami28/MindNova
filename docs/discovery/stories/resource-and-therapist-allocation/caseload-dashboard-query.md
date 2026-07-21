---
key: MN-27
type: story
status: in-progress
epic: MN-6
points: 3
priority: minor
labels: [MindNova]
relates:
  - key: MN-26
    why: "requires client-therapist assignments to exist for counting"
  - key: MN-19
    why: "follows the same read-only query endpoint pattern"
---

# Caseload Dashboard Query Endpoint

📌 Background

* Practice managers need visibility into therapist workload: who is at capacity,
  who has availability, and the overall allocation distribution.

🎯 What's the Goal?

* As a practice manager,
* I want to view a caseload summary for all therapists,
* So that I can identify availability and balance workload.

💡 Expected Value

* Instant visibility into therapist capacity. Supports informed assignment decisions.

✅ Success Criteria

* AC-1: GET /api/therapists/caseload returns a list of CaseloadSummary objects.
* AC-2: Each CaseloadSummary includes TherapistProfileId, TherapistName,
  MaxCaseload, CurrentCaseload, and AvailableCapacity.
* AC-3: CurrentCaseload counts clients with AssignedTherapistId matching that
  therapist.
* AC-4: AvailableCapacity = MaxCaseload - CurrentCaseload.
* AC-5: Only active therapists are included by default.
* AC-6: Endpoint requires authentication.

🛠️ How we'll do it

* Add a GetCaseload action to TherapistsController.
* Query joins TherapistProfiles with a count of assigned Clients grouped by therapist.
* CaseloadSummaryResponse DTO in src/MindNova.Api/Contracts/.
* No pagination needed (therapist count per consultancy is small, under 50).

⚠️ Risks & Blockers

* Depends on MN-26 (assignment must exist to count).

## Artifacts and references

* Response DTO - src/MindNova.Api/Contracts/CaseloadSummaryResponse.cs
* Domain model - src/MindNova.Domain/Entities/CaseloadSummary.cs
* Controller action - src/MindNova.Api/Controllers/TherapistsController.cs (GetCaseload)
* Service method - src/MindNova.Infrastructure/Services/TherapistService.cs (GetCaseloadAsync)
* Integration tests - tests/MindNova.Api.Tests/Therapists/CaseloadEndpointTests.cs
* PR - https://github.com/tsunami28/MindNova/pull/21

⚠️ Risks & Blockers

* Depends on MN-22 (assignment must exist to count).
