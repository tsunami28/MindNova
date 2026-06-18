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

* ASP.NET Identity tables created via EF migration (AspNetUsers, AspNetRoles, etc.).
* POST /api/auth/register creates a user account.
* POST /api/auth/login returns a JWT bearer token.
* Endpoints can be decorated with [Authorize(Roles = "...")] and enforcement works.
* Seed data creates default roles: Admin, Therapist, Receptionist.
* Password policy enforced (minimum length, complexity).
* Tests cover: registration, login, invalid credentials, role-based access denial.

🛠️ How we'll do it

* Add Microsoft.AspNetCore.Identity.EntityFrameworkCore to Infrastructure project.
* Extend DbContext with IdentityDbContext<ApplicationUser>.
* Add AuthController with Register and Login actions (thin controller, logic in a service).
* Configure JWT bearer authentication in Program.cs.
* Seed roles in a data seeder run at startup (dev) or via migration (prd).

⚠️ Risks & Blockers

* Token refresh and revocation are out of scope for this story (future enhancement).
* Password reset flow (email-based) is out of scope.
