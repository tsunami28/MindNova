---
key: MN-38
type: story
status: done
epic: MN-7
points: 3
priority: minor
labels: [MindNova]
relates:
  - key: MN-36
    why: "exports the same data as practice statistics"
  - key: MN-37
    why: "exports the same data as therapist statistics"
  - spec: specs/reports.openapi.yaml
    why: "contract for CSV export endpoints"
---

# Report CSV Export

📌 Background

* Practice managers need to download report data as CSV for further analysis
  in spreadsheets or BI tools.

🎯 What's the Goal?

* As a practice manager,
* I want to export reporting data as CSV,
* So that I can analyse metrics in Excel or other tools.

💡 Expected Value

* Bridges the gap to external analysis tools without building a full BI integration.

✅ Success Criteria

* AC-1: GET /api/reports/practice-stats/export with date_from and date_to returns
  a CSV file (Content-Type: text/csv) with the practice statistics.
* AC-2: GET /api/reports/therapist-stats/export with date_from and date_to returns
  a CSV file with the per-therapist breakdown.
* AC-3: The CSV includes a header row with column names matching the JSON property
  names (PascalCase per C06).
* AC-4: The response includes Content-Disposition: attachment with a descriptive
  filename (e.g. practice-stats-2026-07-01-to-2026-07-28.csv).
* AC-5: Requires authentication.

🛠️ How we'll do it

* Add export actions to ReportsController that call the same service methods as
  MN-36/MN-37 and format the result as CSV.
* Use a simple CSV formatter (StringBuilder or a lightweight library).
* Set Content-Type to text/csv and Content-Disposition to attachment.

⚠️ Risks & Blockers

* Depends on MN-36 and MN-37 (the data endpoints must exist first).

## Artifacts and references

* API contract - specs/reports.openapi.yaml
* Controller actions - src/MindNova.Api/Controllers/ReportsController.cs (ExportPracticeStats, ExportTherapistStats, EscapeCsv)
* Tests - tests/MindNova.Api.Tests/Reports/ReportExportTests.cs
* PR - https://github.com/tsunami28/MindNova/pull/36
