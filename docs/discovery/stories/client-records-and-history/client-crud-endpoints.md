---
key: MN-14
type: story
status: backlog
epic: MN-2
points: 5
priority: high
labels: [MindNova]
relates:
  - key: MN-13
    why: "depends on the Client entity and migration"
---

# Client CRUD API Endpoints

📌 Background

* With the Client model in place, the API needs endpoints to create, read, update,
  and archive client records. These are the primary day-to-day operations for
  office administrators and therapists.

🎯 What's the Goal?

* As an office administrator,
* I want to create, view, update, and archive client records via the API,
* So that client information is managed digitally and kept current.

💡 Expected Value

* Core CRUD capability. Every other client-facing feature (sessions, notes,
  allocation) depends on clients existing in the system.

✅ Success Criteria

* POST /api/clients - creates a new client, returns the created resource.
* GET /api/clients/{id} - returns a single client by ID.
* GET /api/clients - returns a paginated list of clients.
* PUT /api/clients/{id} - updates client details.
* DELETE /api/clients/{id} - soft-archives the client (sets IsArchived = true).
* Archived clients excluded from default list but retrievable by ID.
* Validation errors return ProblemDetails (400).
* All endpoints require [Authorize] (any authenticated user).
* Tests: creation, retrieval, update, archive, validation failure, not-found.

🛠️ How we'll do it

* Add ClientsController (thin) in MindNova.Api/Controllers/.
* Add IClientService / ClientService in MindNova.Infrastructure/Services/.
* Use request/response DTOs (CreateClientRequest, ClientResponse, UpdateClientRequest).
* Map entities to DTOs via a static mapper or extension methods (no AutoMapper).
* Validation via DataAnnotations on request DTOs or FluentValidation.

⚠️ Risks & Blockers

* Depends on MN-13 (domain model).
* Role-based filtering (therapist sees only assigned clients) is out of scope
  here; that comes with the allocation epic (MN-6).
