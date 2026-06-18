---
key: MN-7
type: epic
status: backlog
priority: minor
labels: [MindNova]
relates:
  - key: MN-3
    why: "reports aggregate session data"
  - key: MN-2
    why: "reports aggregate client intake data"
---

# Reporting and Analytics

📌 Background

* Practice managers need operational visibility: session volumes, no-show rates,
  therapist utilisation, revenue indicators.

🎯 What's the Goal?

* As a practice manager,
* I want dashboards and exportable reports on key operational metrics,
* So that I can make data-driven decisions about staffing, scheduling, and growth.

💡 Expected Value

* Actionable insights without manual spreadsheet work. Identifies trends early.

✅ Success Criteria

* Dashboard with: sessions per period, no-show rate, therapist utilisation, new
  client intake rate.
* Date-range filtering.
* CSV export for further analysis.

🛠️ How we'll do it

* Define reporting query endpoints that aggregate session, client, and allocation
  data.
* Build summary/statistics endpoints with date-range parameters.
* Add CSV export via content negotiation or a dedicated export endpoint.

⚠️ Risks & Blockers

* Depends on most other epics for source data.
* BI tool integration (Power BI embedded) is out of scope for V1.
