---
key: MN-8
type: epic
status: done
priority: minor
labels: [MindNova]
relates:
  - key: MN-1
    why: "UI consumes the API built on the platform foundation"
---

# Web Application UI

📌 Background

* MindNova has a complete REST API covering clients, sessions, therapists,
  availability, calendar, treatment notes, and reporting. Practice staff
  currently have no way to interact with the system without API tooling.

🎯 What's the Goal?

* As a practice manager, therapist, or receptionist,
* I want a web-based interface to manage clients, schedule sessions, write
  treatment notes, and view reports,
* So that daily operations can be performed without technical API knowledge.

💡 Expected Value

* Makes the system usable by non-technical staff. Enables real-world adoption.

✅ Success Criteria

* Login and role-based navigation (Admin, Therapist, Receptionist).
* Client management: list, search, create, edit, view treatment timeline.
* Session scheduling: create, view calendar, conflict feedback.
* Treatment notes: create, view by session and by client.
* Reports dashboard: practice stats, therapist utilisation, CSV download.
* Responsive layout for desktop and tablet use.

🛠️ How we'll do it

* Technology decision needed (spike): Blazor Server, Blazor WASM, React, or
  another SPA framework. Recommend a spike before committing.
* Consume the existing API endpoints via HTTP client.
* JWT-based auth flow (login page, token storage, role-gated navigation).
* Component-per-domain architecture (Clients, Sessions, Calendar, Notes, Reports).

⚠️ Risks & Blockers

* Technology choice should be made via a spike before slicing stories.
* No design system or mockups exist yet; UI stories will need wireframe-level
  detail in their ACs.
* Accessibility requirements (WCAG) not yet defined; V1 targets functional
  correctness, accessibility as a follow-up.

## Decisions and ADRs

* 2026-07-29: UI technology decided as Blazor Server with MudBlazor - see docs/adrs/0010-blazor-server-for-web-ui.md
