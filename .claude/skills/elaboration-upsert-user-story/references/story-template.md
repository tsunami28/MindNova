# [PLACEHOLDER] Jira Story Template

The AZURE project uses this 6-section markdown template. The emoji headers are house
style and automation keys off them, so keep them exactly as written. Supply a real
description on create so the automation does not overwrite it with a blank default.

## The template

```
📌 Background

* [What happened, the current state, why this ticket exists. Reference the originating
  ticket key if this extends prior work.]

🎯 What's the Goal?

* As a [persona],
* I want [capability],
* So that [reason].

💡 Expected Value

* [Measurable or concrete impact: faster review, fewer incidents, cost, UX.]

✅ Success Criteria

* [Conditions that make this done. Make each one checkable, not vague.]

🛠️ How we'll do it

* [Approach, dependencies, the components touched. Name real files/classes for dev work.]

⚠️ Risks & Blockers

* [What might slow this down, what needs alignment, what is explicitly out of scope.]
```

## Optional: Technical Implementation section

For dev-ready stories (especially MindNova/platform work), add a `🔧 Technical
Implementation` section after `🛠️ How we'll do it`. List the files reused as-is, the
new/changed components with their paths, the data sources (API methods), and the tests
to add. This is what makes a story pick-up-and-build ready. Cite real symbols, for
example `Services/CloudConfigurationJsonDifferenceService.cs` and the method that does
the work, so the implementer does not have to rediscover them.

## Worked example

This is AZURE-2537, created from this pattern. Note: concrete files and methods in the
technical section, checkable success criteria, explicit out-of-scope, and a points
estimate justified against comparators (5, reusing an existing diff engine and only
adding a version-picker + wiring layer).

```
📌 Background

* Today the portal can diff cloud environment configurations, but only a fixed,
  automatic pair: on the configuration page (`CloudConfigurationDetails.razor`), the
  "Configuration Changes Overview (Latest vs New)" panel compares the version being
  viewed against the config derived from its `TemplateConfigurationId`. Built in AZURE-2328.
* The diff engine (`CloudConfigurationJsonDifferenceService.GenerateAlignedDiff`) and the
  viewer (`CloudConfigurationJsonDifferenceViewer`) are already version-agnostic.
* Users have no way to pick two arbitrary versions and compare them. Related: AZURE-2489.

🎯 What's the Goal?

* As a platform admin / cloudspace approver,
* I want to select any two versions of a cloud environment configuration and see the differences,
* So that I can audit how a configuration evolved, not just the auto-selected pair.

💡 Expected Value

* Reviewers and auditors can inspect change history on demand.
* Reuses the existing, proven diff rendering, so behaviour stays consistent.

✅ Success Criteria

* The user can choose a left and a right version from the list of existing versions.
* Selecting two versions renders the existing viewer with the aligned diff of those two.
* Selecting the same version on both sides shows a clear "identical version" empty state.
* The existing automatic "Latest vs New" panel is unaffected.

🛠️ How we'll do it

* Add a version-selection UI listing versions via `GetCloudEnvironmentConfigurationsAsync`.
* Fetch both configs with `GetCloudEnvironmentConfigurationByIdAsync`, serialize, and call
  `CloudConfigurationJsonDifferenceService.GenerateAlignedDiff(leftJson, rightJson)`.
* Feed the result into `CloudConfigurationJsonDifferenceViewer`.

🔧 Technical Implementation

* Reused as-is: `Services/CloudConfigurationJsonDifferenceService.cs`,
  `CloudConfigurationJsonDifferenceViewer.razor` (+ `.razor.css` / `.razor.js`).
* New: a small `CloudConfigurationVersionCompare.razor` with two pickers, mirroring the
  fetch + serialize + diff wiring in `CloudConfigurationDetails.OnInitializedAsync`.
* Tests (`MindNova/tests/Portal.MindNova.Tests`): picker mapping, same-version short-circuit.

⚠️ Risks & Blockers

* Scope creep on placement (new route + deep-linking + auth) could push 5 toward 8. Keep V1 in-page.
* Overlaps with AZURE-2489; linked as related.
* Out of scope: cross-cloudspace comparison, exporting the diff.
```

## Fields set on creation (for the worked example)

- `issueTypeName`: `Story`
- `projectKey`: `AZURE`
- `additional_fields`: `{"priority": {"name": "Minor"}, "labels": ["MindNova", "Refined"], "customfield_10101": 5, "customfield_10014": "AZURE-2518"}`
- After create: `createIssueLink` (type `Relates`) to the originating story and any placeholder.
