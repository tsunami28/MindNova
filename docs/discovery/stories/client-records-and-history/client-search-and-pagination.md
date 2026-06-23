---
key: MN-15
type: story
status: in-progress
epic: MN-2
points: 3
priority: minor
labels: [MindNova]
relates:
  - key: MN-14
    why: "extends the client list endpoint with search and pagination"
---

# Client Search and Pagination

📌 Background

* A practice with hundreds of clients needs fast lookup. The basic GET /api/clients
  list is insufficient without search-by-name and filtering capabilities.

🎯 What's the Goal?

* As a therapist or receptionist,
* I want to search for clients by name or identifier and page through results,
* So that I can quickly find a client record without scrolling through the full list.

💡 Expected Value

* Sub-second client lookup. Supports the front-desk workflow of finding a client
  at check-in.

✅ Success Criteria

* AC-1: GET /api/clients?search=<term> filters by first name, last name, or email
  (case-insensitive, partial match).
* AC-2: GET /api/clients?search=nonexistent returns an empty Items list with
  TotalCount = 0 (not an error).
* AC-3: Default pagination is page = 1, page_size = 20 when no query parameters
  are provided.
* AC-4: GET /api/clients?page=2&page_size=5 returns the correct slice and
  metadata (TotalCount, Page, PageSize).
* AC-5: Archived clients are excluded by default; include_archived=true includes
  them in results.
* AC-6: Response shape is PagedResponse with Items, TotalCount, Page, PageSize.
* AC-7: Requests without a valid authentication token receive a 401 response.

(Test traits: each AC covered by xUnit integration tests tagged
[Trait("Story","MN-15")] + [Trait("AC","AC-n")] using the SqlServerFixture
pattern.)

🛠️ How we'll do it

* Extend the existing GET /api/clients endpoint in ClientsController with query
  parameters.
* Add a specification or query builder in ClientService for composing filters.
* Return a PagedResponse<ClientResponse> wrapper with metadata.

⚠️ Risks & Blockers

* Depends on MN-14 (CRUD endpoints and service layer).
* Full-text search (SQL Server FTS) is out of scope; LIKE-based matching is
  sufficient for V1 scale.

## Artifacts and references

* API contract - specs/clients.openapi.yaml (listClients operation)
* Integration tests - tests/MindNova.Api.Tests/Clients/ClientSearchTests.cs
* PR - https://github.com/tsunami28/MindNova/pull/8
