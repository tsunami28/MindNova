---
key: MN-41
type: story
status: backlog
epic: MN-8
points: 5
priority: minor
labels: [MindNova]
relates:
  - key: MN-40
    why: "depends on the Blazor project scaffold and auth"
  - key: MN-14
    why: "consumes the client CRUD API endpoints"
  - key: MN-16
    why: "consumes the client treatment timeline endpoint"
---

# Client Management Pages

📌 Background

* Receptionists and therapists need to manage client records through the UI:
  search, view details, create new clients, and review treatment history.

🎯 What's the Goal?

* As a receptionist or therapist,
* I want to manage clients through the web interface,
* So that I can find, create, and review client records without API tooling.

💡 Expected Value

* Core workflow for front-desk staff. First real domain page in the UI.

✅ Success Criteria

* AC-1: Client list page with search by name and paginated results (MudTable).
* AC-2: Create client form with validation (required fields, email format).
* AC-3: Edit client form pre-populated with current data.
* AC-4: Client detail page showing all fields and assigned therapist.
* AC-5: Treatment timeline tab on the client detail page showing session history.
* AC-6: Navigation link visible to all authenticated roles.

🛠️ How we'll do it

* Add Pages/Clients/ with List.razor, Create.razor, Edit.razor, Detail.razor.
* Use MudTable with ServerData for paginated search.
* Use MudForm with validation for create/edit.
* HttpClient service calling GET/POST/PUT /api/clients and GET /api/clients/{id}/timeline.

⚠️ Risks & Blockers

* Depends on MN-40 (scaffold must exist first).
