---
key: MN-14
type: story
status: in-progress
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

* AC-1: POST /api/clients with valid data returns the created client with a
  generated Id, CreatedAt set, and IsArchived = false.
* AC-2: POST /api/clients with missing required fields (FirstName, LastName,
  or Email) returns a ProblemDetails error.
* AC-3: POST /api/clients with an invalid email format returns a
  ProblemDetails error.
* AC-4: GET /api/clients/{id} with a valid ID returns that client's full
  data.
* AC-5: GET /api/clients/{id} with a non-existent ID returns a
  ProblemDetails error indicating not found.
* AC-6: GET /api/clients returns a list of non-archived clients.
* AC-7: PUT /api/clients/{id} with valid data updates the client fields and
  returns the updated resource with UpdatedAt refreshed.
* AC-8: PUT /api/clients/{id} with a non-existent ID returns a
  ProblemDetails error indicating not found.
* AC-9: DELETE /api/clients/{id} sets IsArchived = true on the client
  without deleting the record.
* AC-10: An archived client is excluded from GET /api/clients but is still
  retrievable via GET /api/clients/{id}.
* AC-11: All five endpoints require authentication; requests without a valid
  token receive a 401 response.

(Test traits: each AC covered by xUnit integration tests tagged
[Trait("Story","MN-14")] + [Trait("AC","AC-n")] using the existing
SqlServerFixture pattern. AC-11 verified by a single unauthenticated
request test per endpoint.)

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

## Artifacts and references

* API contract - specs/clients.openapi.yaml (covers MN-14, MN-15, MN-16)
* Controller - src/MindNova.Api/Controllers/ClientsController.cs
* DTOs - src/MindNova.Api/Contracts/CreateClientRequest.cs, UpdateClientRequest.cs, ClientResponse.cs
* Service interface - src/MindNova.Infrastructure/Services/IClientService.cs
* Service implementation - src/MindNova.Infrastructure/Services/ClientService.cs
* Integration tests - tests/MindNova.Api.Tests/Clients/ClientEndpointTests.cs
* PR - https://github.com/tsunami28/MindNova/pull/7

## Decisions and ADRs

* 2026-06-22: API contract designed - PascalCase JSON bodies (C06), all-200 with ProblemDetails errors (C07), Bearer JWT auth, snake_case query params
* 2026-06-18: Azure SQL Database (serverless) selected as backend - see docs/adrs/0008-azure-sql-database-serverless.md (via MN-8)
