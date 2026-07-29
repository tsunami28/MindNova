---
key: MN-36
type: story
status: done
epic: MN-7
points: 5
priority: minor
labels: [MindNova]
relates:
  - key: MN-27
    why: "follows the same aggregation query pattern (caseload dashboard)"
  - key: MN-18
    why: "aggregates session data (counts, statuses)"
  - key: MN-14
    why: "aggregates client intake data (new clients per period)"
  - spec: specs/reports.openapi.yaml
    why: "contract for practice statistics endpoint"
---

# Practice Statistics Endpoint

📌 Background

* Practice managers need a single API call that returns key operational metrics
  for a date range: session volumes, no-show rates, cancellations, new client
  intake, and therapist utilisation.

🎯 What's the Goal?

* As a practice manager,
* I want to query aggregate practice statistics for a date range,
* So that I can make data-driven decisions about staffing and scheduling.

💡 Expected Value

* Actionable operational insights without manual spreadsheet work.

✅ Success Criteria

* AC-1: GET /api/reports/practice-stats with date_from and date_to returns a
  PracticeStatsResponse object.
* AC-2: Response includes TotalSessions, CompletedCount, CancelledCount,
  NoShowCount, NoShowRate (percentage), NewClientsCount.
* AC-3: TherapistUtilisation is included as a list of per-therapist session
  counts within the date range.
* AC-4: An empty date range (no sessions) returns zero counts (not an error).
* AC-5: Date range is required; missing params return ProblemDetails.
* AC-6: Requires authentication.

🛠️ How we'll do it

* Add ReportsController with a GetPracticeStats action.
* Add IReportService / ReportService that queries Sessions (grouped by Status),
  Clients (CreatedAt in range), and TherapistProfiles for utilisation.
* DTOs: PracticeStatsResponse, TherapistUtilisationEntry.
* SessionStatus enum members: Scheduled, Completed, Cancelled, NoShow (verified
  in src/MindNova.Domain/Entities/SessionStatus.cs).

⚠️ Risks & Blockers

* None - all source data entities are in place.

## Artifacts and references

* API contract - specs/reports.openapi.yaml
* Domain model - src/MindNova.Domain/Entities/PracticeStats.cs
* Service interface - src/MindNova.Infrastructure/Services/IReportService.cs
* Service implementation - src/MindNova.Infrastructure/Services/ReportService.cs
* Controller - src/MindNova.Api/Controllers/ReportsController.cs
* DTOs - src/MindNova.Api/Contracts/PracticeStatsResponse.cs, TherapistUtilisationEntry.cs
* DI registration - src/MindNova.Infrastructure/DependencyInjection.cs
* Tests - tests/MindNova.Api.Tests/Reports/ReportEndpointTests.cs
* PR - https://github.com/tsunami28/MindNova/pull/34
