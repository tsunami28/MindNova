---
key: MN-44
type: story
status: done
epic: MN-8
points: 3
priority: minor
labels: [MindNova]
relates:
  - key: MN-40
    why: "depends on the Blazor project scaffold and auth"
  - key: MN-36
    why: "consumes the practice statistics endpoint"
  - key: MN-37
    why: "consumes the therapist statistics endpoint"
  - key: MN-38
    why: "consumes the CSV export endpoints"
---

# Reports Dashboard

📌 Background

* Practice managers need a visual dashboard showing operational metrics and
  the ability to download CSV exports for further analysis.

🎯 What's the Goal?

* As a practice manager,
* I want to view practice statistics and therapist utilisation in the UI,
* So that I can make data-driven staffing and scheduling decisions.

💡 Expected Value

* Visual insights without leaving the app. CSV download for offline analysis.

✅ Success Criteria

* AC-1: Reports page with date range picker (MudDateRangePicker).
* AC-2: Practice stats card showing TotalSessions, CompletedCount, NoShowCount,
  NoShowRate, CancelledCount, NewClientsCount.
* AC-3: Therapist utilisation table (MudTable) with per-therapist breakdown.
* AC-4: CSV download buttons for practice stats and therapist stats.
* AC-5: Empty state message when no data exists for the selected range.
* AC-6: Navigation link visible to all authenticated roles.

🛠️ How we'll do it

* Add Pages/Reports/ with Dashboard.razor.
* Use MudCard for stats display, MudTable for therapist breakdown.
* CSV download via JavaScript interop to trigger file download from the
  export endpoints.

⚠️ Risks & Blockers

* Depends on MN-40 (scaffold).

## Artifacts and references

* Models - src/MindNova.Web/Models/ReportModels.cs
* API service - src/MindNova.Web/Services/ReportApiService.cs
* Dashboard page - src/MindNova.Web/Pages/Reports/Dashboard.razor
* PR - https://github.com/tsunami28/MindNova/pull/43
