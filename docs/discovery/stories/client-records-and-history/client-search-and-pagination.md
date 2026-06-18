---
key: MN-15
type: story
status: backlog
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

* GET /api/clients?search=<term> filters by first name, last name, or email
  (case-insensitive, partial match).
* Pagination via query parameters: page (default 1), pageSize (default 20, max 100).
* Response includes total count, current page, and page size for UI paging controls.
* Archived clients excluded by default; optional includeArchived=true parameter.
* SQL query uses indexed columns (LastName, Email) for performance.
* Tests: search hit, search miss, pagination boundaries, archived filtering.

🛠️ How we'll do it

* Extend the existing GET /api/clients endpoint in ClientsController with query
  parameters.
* Add a specification or query builder in ClientService for composing filters.
* Return a PagedResponse<ClientResponse> wrapper with metadata.

⚠️ Risks & Blockers

* Depends on MN-14 (CRUD endpoints and service layer).
* Full-text search (SQL Server FTS) is out of scope; LIKE-based matching is
  sufficient for V1 scale.
