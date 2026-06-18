# Plan: AI-SDLC workflow for Application.MindNova (holistic)

## Context

Grow the personal Jira-ticket skill into a full, team-owned **AI-SDLC workflow**: a set of
phase/step skills shared by Claude Code and GitHub Copilot, a governing constitution + ADRs,
and an **artifact graph** that links every work product (Jira, specs, MADRs, docs, designs)
so any contributor or tool can navigate "what exists and what's next." Target reference: the
AI-SDLC Maturity Model deck (L1 prompt-driven -> L5 autonomous; the L3 hinge is gating, the
contract must live in the repo, keep it vendor-neutral markdown).

## What is already done (this effort)

- Consolidated agent instructions into a single `AGENTS.md` (lean, ~65 lines) with a Constitution
  core; `CLAUDE.md` = `@AGENTS.md` stub; `.github/copilot-instructions.md` points to it. (PR #75836, open -> main.)
- `docs/constitution.md` (clauses C01-C09), MADRs 0003/0004/0005 (all now Accepted), Bicep guide
  moved to `docs/conventions/bicep.md`, Portal UI reference doc.
- Swept ~36 docs of source line-number citations and added `MindNova/toolsCheck-DocLineCitations.ps1`
  + a pipeline gate enforcing C02. (PR #75839, **merged into ai-sdlc**.)
- Six skills authored in `~/.claude/skills/` (personal, to be moved): `[PLACEHOLDER]-jira-ticket`,
  `constitution`, `author-adr`, `enrich-ticket`, `update-docs-dual`, `dev-cycle`.

## Verified cross-tool facts (these drive the design)

- **`SKILL.md` is cross-tool.** GitHub Copilot (CLI, VS Code agent mode, cloud agent) supports the
  Agent Skills standard and loads project skills from **`.claude/skills/`**, `.github/skills/`, and
  `.agents/skills/`; personal from `~/.copilot/skills` or `~/.agents/skills`. Claude Code loads
  `.claude/skills/`. So **one committed `.claude/skills/<name>/SKILL.md` is read by both tools.**
  (Sources: GitHub Docs "About agent skills" + Copilot CLI "Add skills"; GitHub changelog 2025-12-18.)
- **`AGENTS.md`** is read by Copilot natively and by Claude via the `@AGENTS.md` import in `CLAUDE.md`
  (Claude does not read `AGENTS.md` directly).
- **On-edit, path-scoped guidance is NOT shared by a single format:** Claude uses `.claude/rules/*.md`
  with `paths:`; Copilot uses `.github/instructions/*.instructions.md` with `applyTo:`. Skills, by
  contrast, are description-triggered (the model loads them when relevant), not glob-triggered.
- Agent Skills are also read by Codex CLI and Gemini CLI, so the same skills are portable beyond these two tools.

## Decisions taken (from the user)

1. **Commit everything for Claude.** Remove the `.claude/` components and `CLAUDE.md` from
   `.gitignore`; the team contract (AGENTS, CLAUDE stub, skills, rules) is versioned in the repo.
2. **Skills live in repo `.claude/skills/`** (shared by Claude + Copilot), moved out of `~/.claude/skills/`.
   Delete the personal copies after moving (personal overrides project and would shadow them).
3. **Adopt the user's phase taxonomy** (Discovery, Elaboration, Create, Mature & Maintain) plus a
   cross-cutting Governance track. Testing is not a separate phase (TDD in Create:Code Generation,
   coverage/AC gate in Create:QE).
4. **Build the artifact-graph layer in this go** (it is the basis), brainstormed and planned here first.
5. **Path-scoped conventions are delivered as shared `SKILL.md` (Option A):** one description-triggered
   skill per convention domain in `.claude/skills/`, read by both tools; hard enforcement stays in the
   CI gates. Fall back to generated glob-scoped files (Option B) only where a convention under-triggers.

## Cross-tool sharing: the three layers and how each is shared

| Layer | Mechanism | Shared across Claude + Copilot? |
|---|---|---|
| **Always-on contract** | `AGENTS.md` (+ `CLAUDE.md` = `@AGENTS.md`) | Yes, single file |
| **Invokable procedures (skills)** | `.claude/skills/<name>/SKILL.md` | **Yes, single file** (both tools read `.claude/skills/`) |
| **On-edit conventions (glob-scoped)** | Claude `.claude/rules/*` (`paths:`) vs Copilot `.github/instructions/*` (`applyTo:`) | No single format, see options below |

### Part 1 options: sharing path-scoped conventions (e.g. Bicep on `*.bicep`)
- **Option A (recommended, simplest, truly shared): express the convention as a shared `SKILL.md`**
  in `.claude/skills/<domain>-conventions/` with a description like "Use when creating or editing
  Azure Bicep (`*.bicep`) files." Both tools load it by description when doing that work. One file,
  both tools. Trade-off: description-triggered (model judgment), not glob-deterministic, acceptable
  because conventions are advisory and the hard enforcement is the CI gate, not the prompt.
- **Option B (deterministic, generated): one source `docs/conventions/<topic>.md` + a small committed
  generator** that emits both `.claude/rules/<topic>.md` (`paths:`) and
  `.github/instructions/<topic>.instructions.md` (`applyTo:`). No drift, no symlink fragility; more machinery.
- **Option C (rejected): symlink a single dual-frontmatter file** into both locations, Windows symlink
  fragility (same reason we used `@AGENTS.md` over a symlink for `CLAUDE.md`).
- Recommendation: Option A for most conventions; Option B only where deterministic on-every-edit
  application matters. The convention text stays single-sourced regardless.

## The artifact graph (the heart of Part 3)

Artifacts live across systems (Jira, repo, Confluence, Figma, Dovetail). Do not store the graph in
any one tool; make it an emergent, navigable set of edges anchored on a stable key.

### Anchor: the Jira key, created at the earliest actionable point
- **The Jira work item is the spine.** Every artifact carries its key. Use the hierarchy: Epic
  (initiative) -> Discovery/Spike issue -> Stories (slices). Anchor at the right level, not one
  ticket per micro-step (ticket-per-step is the deck's "gate maximalism" anti-pattern).
- **The Dovetail problem (anchor before a ticket exists, and devs lack Dovetail access):** resolve by
  making the Jira **Discovery/Spike** item the anchor for the research step itself (the user's
  proposal). Research runs in Dovetail; the Jira item **links to** the Dovetail study and carries a
  short synthesis summary. The graph stores the *edge (link) and summary*, not the content, so a
  developer navigates from Jira without needing Dovetail access. Where an upstream tool cannot store
  the Jira key (Dovetail, Figma), the Jira item is the authoritative holder of that edge
  (one-directional is acceptable when the far side cannot reciprocate).

### Edges: front-matter on every repo artifact
Every repo artifact (PRD, spec, MADR, doc) carries machine-readable front-matter:
```yaml
---
story: AZURE-1234          # the anchor
phase: elaboration
step: upsert-api-contract
relates:
  spec: specs/cloudspace-compare.openapi.yaml
  adr: docs/adrs/0007-...md
  design: <repo path or a design-tool link>
  research: <a research-tool, Confluence, or repo link>
  docs: docs/api/cloud-spaces.md
---
```
The Jira side is maintained by the `governance-enrich-ticket` skill (outbound links + why). Repo artifacts carry
the inbound `story:` + `relates:`. Traversal works both directions.

**Story-to-story edges** are first-class and live **in Jira** as native issue links (e.g. the development
Stories link back to the Discovery/Spike story they came from), maintained by `governance-enrich-ticket`
with a one-line reason. Jira is the source of truth for these; `/run-sdlc` follows them when traversing.
See `docs/ai-sdlc/artifact-graph.md` for the rules and the deferred Jira-side integrity check.

### Enforcement: a shared linter (local skill + pipeline gate)
- A single committed script (e.g. `MindNova/toolsCheck-ArtifactGraph.ps1`) validates the required edges:
  a Story in progress links a machine-readable spec; every MADR is backlinked from its story; every
  spec references its story; etc. Repo+Jira edges are validated; external links (Dovetail/Figma) are
  checked for **presence**, not content.
- The **same script** is invoked two ways (the pattern proven by `Check-DocLineCitations.ps1`): a
  local skill `governance-check-graph` for authoring, and a **pipeline gate** for enforcement. One
  rule, one script, shared.

## Skill convention: end with the next step

Every framework skill ends by **suggesting the next step(s)** - which skill(s) to run next and which
persona owns them - so the flow is self-navigating and matches the persona hand-offs (the way BMAD roles
pass work along). Examples: `elaboration-upsert-user-story` ends by pointing to `governance-enrich-ticket`
(link artifacts) and `elaboration-upsert-user-story-acceptance-criteria`; `governance-author-adr` ends by pointing to
`governance-enrich-ticket` (backlink the ADR to its story). This is the local, per-skill version of what
`run-sdlc` does globally, and the two agree because both read the same phase/step map and artifact graph.
The convention is retrofitted into every existing skill in the restructure pass (step 4) and is required of
all new skills.

## Naming and packaging (decided)

Skills are **flat, hyphenated, phase-prefixed folders** in `.claude/skills/`:
`.claude/skills/<phase>-<step>/SKILL.md`, invoked `/<phase>-<step>`. Verified constraint: both Claude Code
and Copilot discover skills only at `.claude/skills/<name>/SKILL.md` (one level deep); Copilot does **not**
recurse into Claude plugin folders or honour `.claude-plugin/plugin.json`. A true plugin colon-namespace
(`/phase:step`) would nest skills where Copilot cannot find them, so we use the hyphenated convention to keep
both tools working. Phase grouping is by name, not a hard namespace.

## Personas (generic role titles)

Adopted from BMAD's role decomposition, re-authored under **generic titles** (not BMAD's character names);
BMAD/ECC are credited as inspiration and any lifted template is attributed per its licence (no vendored code).
A persona is a role + responsibilities + owned steps + artifacts, expressed as the role-framing of its phase/step
skill(s), so personas are cross-tool.

| Persona | Owns |
|---|---|
| Analyst | Discovery: research synthesis, journey/opportunity, hypothesis |
| Designer / UX | Discovery prototype, Elaboration MVP design |
| Product Manager | Elaboration upsert-user-story, PRD, acceptance criteria; Mature feedback |
| Architect | Elaboration architecture + API design; Governance ADRs |
| Scrum Master | Create planning |
| Developer | Create code, refactoring; Mature ticket resolution |
| QA | Create QE/testing; acceptance criteria (with PM) |
| Tech Writer | Create documentation |
| Lead (cross-cutting) | constitution, the contract, graph integrity |

## Multi-agent orchestration

The orchestrator (`run-sdlc` / `create-code-generation`) dispatches the persona step-skills. On Claude Code,
independent steps run in **parallel** via subagents (Task tool) or Agent Teams; on Copilot / other agents they
run **sequentially**. Personas are cross-tool (skills, read by both); the parallelism is a Claude accelerator that
degrades gracefully, and it never relaxes a confirmation gate. The authoritative dispatch contract - the independence
rule, the cross-tool mechanism, the gates, and the "not an autopilot" posture - is in [orchestration.md](./orchestration.md).
This is the ECC "true multi-agent" capability adopted as an orchestration pattern, not vendored.

## Phase / step / artifact / persona / skill map

Skill folder = `<phase>-<step>` in `.claude/skills/`. "exists (`x`)" = rename/extend the existing skill `x` into this slot.

| Phase-step (skill) | Persona | Primary artifact | Owning tool | Status |
|---|---|---|---|---|
| `governance-constitution` | Lead | AGENTS.md core + docs/constitution.md | repo | exists (`constitution`) |
| `governance-author-adr` | Architect | docs/adrs/NNNN | repo | exists (`author-adr`); also serves the architecture step |
| `governance-enrich-ticket` | any | Jira links + front-matter edges | Jira+repo | exists (`enrich-ticket`) |
| `governance-check-graph` | Lead | linter result | repo/CI | exists (`check-graph`) |
| `discovery-synthesize-research` | Analyst | synthesis MD + study link | repo/Confluence+Jira | done |
| `discovery-map-journey` | Analyst | map/diagram | drawio/mermaid | done (reuse `drawio`) |
| `discovery-upsert-hypothesis-canvas` | Analyst/PM | canvas MD | repo | done |
| `elaboration-upsert-user-story` | PM | Jira epics/stories | Jira | exists (`[PLACEHOLDER]-jira-ticket`) |
| `elaboration-upsert-prd-document` | PM | PRD MD | repo | done (reuse `plan-prd`) |
| `elaboration-upsert-user-story-acceptance-criteria` | PM/QA | Jira AC + test traits | Jira+repo | done |
| architecture (elaboration step) | Architect | MADRs | repo | no separate skill; served by `governance-author-adr` |
| `elaboration-upsert-api-contract` | Architect | machine-readable spec (OpenAPI) | repo | done (L3 keystone) |
| `create-sprint-planning` | Scrum Master | sprint plan | Jira | done (light) |
| `create-code-generation` | Developer | code + story-traited TDD tests | repo | exists (`dev-cycle`) |
| `create-refactoring` | Developer | code | repo | done (reuse `refactor-clean`) |
| `create-verify-test-coverage` | QA | tests + coverage/AC results | repo/CI | done (reuse `test-coverage`) |
| `create-upsert-documentation` | Tech Writer | docs/ + docs-functional/ + sales | repo/Confluence | exists (`update-docs-dual`) |
| `mature-collect-release-feedback` | PM | feedback notes | various | done |
| `mature-analyze-ticket` | PM/Support | triage notes | Jira | done |
| `mature-resolve-ticket` | Developer | fix (mini-cycle) | repo+Jira | done (reuse `create-code-generation`) |
| `run-sdlc` | Orchestrator | navigation report | - | done |

### `run-sdlc <artifact>` (the navigator)
Given any artifact (a Jira key, an MD path, a spec), it resolves the anchor, traverses the graph (front-matter +
Jira links), computes which phases/steps are complete vs missing, and reports the current phase + the next
`/<phase>-<step>` to run and which persona owns it. A guide for humans, not an autopilot.

## Build sequence (proposed, incremental, not big-bang)

The full map is ~20 skills; do not build all at once. Order by foundation-first and the deck's
"adopt before automate" rule:

1. **Land + adopt the contract.** Merge PR #75836 (`ai-sdlc` -> `main`) after team review of the
   constitution clauses; socialise `AGENTS.md`. (Highest leverage; nothing below matters if unadopted.) [in PR]
2. **Repo-commit + un-gitignore the Claude layer + tool-neutral skills.** [done, PR #75907]
3. **Graph contract.** Schema + `Check-ArtifactGraph` script + `governance-check-graph` skill + pipeline gate. [done, PR #75914]
4. **Restructure to hyphenated `<phase>-<step>` names + persona framing (one pass).** Rename the existing skills
   (`constitution`->`governance-constitution`, `author-adr`->`governance-author-adr`, `enrich-ticket`->`governance-enrich-ticket`,
   `check-graph`->`governance-check-graph`, `[PLACEHOLDER]-jira-ticket`->`elaboration-upsert-user-story`,
   `update-docs-dual`->`create-upsert-documentation`, `dev-cycle`->`create-code-generation`), update their cross-references,
   and add each persona's role-framing. [done]
5. **`run-sdlc` skeleton** over the graph (read + report; no new step-skills yet).
6. **Fill step-skills incrementally**, highest leverage first (`elaboration-upsert-api-contract`, `elaboration-upsert-user-story-acceptance-criteria`,
   `discovery-synthesize-research`), each consuming/emitting graph front-matter, dispatched by the orchestrator
   (parallel on Claude, sequential elsewhere).

## Verification
- Per skill: the draft-only eval harness used earlier (with-skill vs baseline, static viewer).
- Cross-tool: confirm a committed `.claude/skills/<x>/SKILL.md` triggers in both Claude Code and
  Copilot CLI on a representative prompt.
- Graph: `Check-ArtifactGraph` passes locally and in the pipeline on a fully-linked sample story, and
  fails (with the missing edge named) on a story missing its spec/ADR backlink.

## Risks / open questions
- **Scope.** ~20 skills + a graph + an orchestrator is large. Sequence it; do not block on completeness.
- **Adoption is the real gate** (deck's #1 thesis). Build on the contract only after the team uses it.
- **Front-matter discipline.** The graph is only as good as the edges people fill; tie it to artifacts
  people already touch (Jira stories, PRs) so it is not extra bookkeeping, and let the linter catch gaps.
- **External-tool edges** (Dovetail, Figma) can only be presence-checked, not content-validated, from CI.
- **Naming resolved:** hyphenated `<phase>-<step>` folders in `.claude/skills/` (not colon plugins), because
  Copilot discovers skills only one level deep and does not read plugin-nested skills. Personas use generic role titles.
- **Path-scoped vs skill triggering:** Option A (shared skill) is description-triggered; if the model
  under-triggers a convention, fall back to Option B (generated glob-scoped files) for that one.
