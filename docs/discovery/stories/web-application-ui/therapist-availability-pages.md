---
key: MN-45
type: story
status: backlog
epic: MN-8
points: 5
priority: minor
labels: [MindNova]
relates:
  - key: MN-40
    why: "depends on the Blazor project scaffold and auth"
  - key: MN-25
    why: "consumes the therapist profile CRUD API endpoints"
  - key: MN-29
    why: "consumes the availability CRUD API endpoints"
  - key: MN-27
    why: "consumes the caseload dashboard endpoint"
---

# Therapist and Availability Management Pages

📌 Background

* Admins manage therapist profiles, and therapists define their availability
  slots. The caseload view shows current allocation vs. capacity.

🎯 What's the Goal?

* As an admin or therapist,
* I want to manage therapist profiles and availability through the web interface,
* So that the practice schedule is maintained digitally.

💡 Expected Value

* Admin can onboard/deactivate therapists. Therapists manage their own availability.

✅ Success Criteria

* AC-1: Therapist list page with pagination and include_inactive toggle (Admin).
* AC-2: Create/edit therapist profile form (specialisations, max caseload).
* AC-3: Deactivate therapist (soft-delete) with confirmation.
* AC-4: Availability management sub-page per therapist: list slots, create
  recurring or one-off slots, delete slots.
* AC-5: Caseload view showing current sessions vs. max caseload per therapist.
* AC-6: Admin-only navigation for therapist management; therapists see only
  their own profile and availability.

🛠️ How we'll do it

* Add Pages/Therapists/ with List.razor, Create.razor, Edit.razor, Detail.razor.
* Add Pages/Therapists/Availability/ with Slots.razor, CreateSlot.razor.
* Caseload display using MudProgressLinear or MudChart.
* Role-based visibility: Admin sees all, Therapist sees own profile only.

⚠️ Risks & Blockers

* Depends on MN-40 (scaffold).
