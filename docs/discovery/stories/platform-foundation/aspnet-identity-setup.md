---
key: MN-10
type: story
status: backlog
epic: MN-1
points: 5
priority: high
labels: [MindNova]
relates:
  - key: MN-9
    why: "depends on project scaffold and DbContext"
---

# ASP.NET Identity Setup

📌 Background

* MindNova uses local accounts (username/password) for authentication. ASP.NET Identity
  with JWT bearer tokens provides registration, login, and role-based access control.
* Depends on MN-9 (project scaffold and DbContext exist).

🎯 What's the Goal?

* As a therapist or administrator,
* I want to register, log in, and have my access controlled by role,
* So that the system is secure and role-appropriate from the start.

💡 Expected Value

* Secure authentication from day one. Roles (Admin, Therapist, Receptionist) enable
  endpoint-level authorization for all domain features.

✅ Success Criteria

* AC-1: An EF Core migration adds the ASP.NET Identity schema (AspNetUsers, AspNetRoles,
  AspNetUserRoles, and related tables) and applies cleanly to the existing database.
* AC-2: POST /api/auth/register with a valid email and password returns HTTP 200 and
  persists the user.
* AC-3: POST /api/auth/register with a duplicate email returns a ProblemDetails error.
* AC-4: POST /api/auth/register with a password that violates the minimum-length or
  complexity policy returns a ProblemDetails error listing the violations.
* AC-5: POST /api/auth/login with valid credentials returns HTTP 200 with a JWT bearer token.
* AC-6: POST /api/auth/login with invalid credentials returns a ProblemDetails error.
* AC-7: The returned JWT contains the user's assigned roles as claims.
* AC-8: A request to an [Authorize]-protected endpoint without a token returns HTTP 401.
* AC-9: A request to an [Authorize(Roles = "Admin")]-protected endpoint with a token
  lacking the Admin role returns HTTP 403.
* AC-10: Default roles (Admin, Therapist, Receptionist) exist in the database after
  startup seeding.

Test trait mapping:
- AC-1: `[Trait("Story","MN-10")]` + `[Trait("AC","AC-1")]` - integration test applying
  the migration to a test database; asserts Identity tables exist.
- AC-2: `[Trait("Story","MN-10")]` + `[Trait("AC","AC-2")]` - integration test via
  WebApplicationFactory; asserts 200 and user persisted.
- AC-3: `[Trait("Story","MN-10")]` + `[Trait("AC","AC-3")]` - integration test; asserts
  ProblemDetails on duplicate email.
- AC-4: `[Trait("Story","MN-10")]` + `[Trait("AC","AC-4")]` - integration test; asserts
  ProblemDetails with violation details on weak password.
- AC-5: `[Trait("Story","MN-10")]` + `[Trait("AC","AC-5")]` - integration test; asserts
  200 and a valid JWT in response.
- AC-6: `[Trait("Story","MN-10")]` + `[Trait("AC","AC-6")]` - integration test; asserts
  ProblemDetails on wrong credentials.
- AC-7: `[Trait("Story","MN-10")]` + `[Trait("AC","AC-7")]` - unit test; decodes token
  and asserts role claims present.
- AC-8: `[Trait("Story","MN-10")]` + `[Trait("AC","AC-8")]` - integration test; asserts
  401 when no Authorization header sent.
- AC-9: `[Trait("Story","MN-10")]` + `[Trait("AC","AC-9")]` - integration test; asserts
  403 when token lacks required role.
- AC-10: `[Trait("Story","MN-10")]` + `[Trait("AC","AC-10")]` - integration test; queries
  roles from seeded test database; asserts all three exist.

🛠️ How we'll do it

* Add Microsoft.AspNetCore.Identity.EntityFrameworkCore to Infrastructure project.
* Extend DbContext with IdentityDbContext<ApplicationUser>.
* Add AuthController with Register and Login actions (thin controller, logic in a service).
* Configure JWT bearer authentication in Program.cs.
* Seed roles in a data seeder run at startup (dev) or via migration (prd).

⚠️ Risks & Blockers

* Token refresh and revocation are out of scope for this story (future enhancement).
* Password reset flow (email-based) is out of scope.
