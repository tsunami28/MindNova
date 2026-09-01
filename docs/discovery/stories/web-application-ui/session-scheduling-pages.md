---
key: MN-42
type: story
status: in-progress
epic: MN-8
points: 5
priority: minor
labels: [MindNova]
relates:
  - key: MN-40
    why: "depends on the Blazor project scaffold and auth"
  - key: MN-18
    why: "consumes the session CRUD API endpoints"
  - key: MN-31
    why: "consumes the calendar query endpoint for the calendar view"
---

# Session Scheduling Pages

📌 Background

* Therapists and receptionists need to schedule, view, and manage therapy
  sessions. The UI must show conflict errors from the API clearly.

🎯 What's the Goal?

* As a therapist or receptionist,
* I want to schedule and manage sessions through the web interface,
* So that I can book appointments and see the calendar without API tooling.

💡 Expected Value

* Core scheduling workflow. Calendar view replaces manual schedule management.

✅ Success Criteria

* AC-1: Session list page with filtering by therapist, client, status, and date range.
* AC-2: Create session form with therapist and client selectors, date/time picker.
* AC-3: Conflict errors (session overlap, outside availability) displayed as
  clear user-facing messages from the API ProblemDetails response.
* AC-4: Calendar view for a selected therapist showing merged availability and
  sessions (consuming GET /api/therapists/{id}/calendar).
* AC-5: Edit session form for rescheduling and status transitions.
* AC-6: Navigation link visible to all authenticated roles.

🛠️ How we'll do it

* Add Pages/Sessions/ with List.razor, Create.razor, Edit.razor.
* Add Pages/Calendar/ with CalendarView.razor using MudCalendar or a custom
  day/week grid component.
* Parse ProblemDetails Type URIs for conflict-specific error messages.
* HttpClient service calling sessions and calendar API endpoints.

⚠️ Risks & Blockers

* Depends on MN-40 (scaffold) and MN-41 (client pages for client selector reuse).
* MudBlazor does not have a built-in calendar component; may need a custom grid
  or a third-party Blazor calendar library.

## Artifacts and references

* Models - src/MindNova.Web/Models/SessionModels.cs
* API service - src/MindNova.Web/Services/SessionApiService.cs
* List page - src/MindNova.Web/Pages/Sessions/List.razor
* Create page - src/MindNova.Web/Pages/Sessions/Create.razor
* Edit page - src/MindNova.Web/Pages/Sessions/Edit.razor
* Calendar view - src/MindNova.Web/Pages/Calendar/CalendarView.razor
* PR - https://github.com/tsunami28/MindNova/pull/41

## Triage

**Report:** Session creation asks the user to enter a therapist user ID and client ID,
rather than selecting a therapist and client.

**Confirmed:** Source inspection of `Pages/Sessions/Create.razor` found two
`MudTextField` controls for `TherapistUserId` and `ClientId`. This conflicts with
AC-2, which requires therapist and client selectors. `ClientApiService` and
`TherapistApiService` already expose list operations that can supply selector data.

**Classification:** Confirmed bug - MN-42 was marked done although AC-2 is incomplete.

**Impact and priority:** Minor. The scheduling workflow remains usable only with opaque
identifiers, which blocks ordinary UI users from creating sessions without API tooling.

**Duplicate check:** No duplicate work item found under `docs/discovery/`.

**Recommended next step:** `/mature-resolve-ticket` to replace the identifier fields with
client and therapist selectors and add focused UI coverage.
