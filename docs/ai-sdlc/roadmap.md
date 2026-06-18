# AI-SDLC roadmap

The ordered steps to build the workflow designed in `docs/ai-sdlc/plan.md`. The plan is the **design**
(phases, personas, artifact graph, conventions); this is the **sequence + status**. Foundation-first, and
adopt before automate (the maturity model's rule: do not build L4/L5 on an unadopted contract).

Status legend: **[done]** committed to the `ai-sdlc` PR #75836 (not yet on main) · **[merged]** on main · **[deferred]** intentionally postponed · **[todo]** not started.

## State: one branch, one PR (stack collapsed 2026-06-02)

The earlier 3-PR stack was **collapsed into a single PR**. All work now lands on `ai-sdlc` -> **#75836**, and we keep building there before merging to main.

- **#75836** `ai-sdlc -> main` - the whole contract in one PR: `AGENTS.md`, constitution C01-C09, ADRs 0002-0006, the 7 `.claude/skills/`, the artifact-graph contract + gate, and `docs/ai-sdlc/` (plan + roadmap). Current `main` merged in, the bicep duplicate consolidated, C02 + artifact-graph gates green locally. **Draft on purpose** - building more in-branch before merge (user's call).
- **#75907 and #75914** - **abandoned**, collapsed into #75836 (each backlinked to it).

## Step 1 - Adopt the contract  [deferred by choice]
- **[done]** Collapsed the 3-PR stack into the single #75836; resolved the `main` conflict (the bicep doc duplicate); C02 + artifact-graph gates pass locally.
- **[deferred]** Merge #75836 to `main` after team review - postponed to keep building in-branch first (user's call). This is still the real adoption gate; nothing is enforced team-wide until it merges.
- **[deferred]** Add the code-analysis pipeline to the `main` branch policy so the C02 + artifact-graph gates run **pre-merge** (Azure DevOps admin). Best done at/with the merge.
- **[deferred]** Delete the personal `~/.claude/skills/` copies (all 6 confirmed safe) so the repo ones are not shadowed - do right after merge, so any main-based checkout keeps the skills.
- Unblocks the gate phase (Step 6) and team-wide use. Authoring (Steps 2-5) proceeds in-branch meanwhile.

## Step 2 - Restructure to the taxonomy (one pass)  [done]
- **[done]** Renamed the 7 skills to hyphenated `<phase>-<step>`: `governance-constitution`, `governance-author-adr`, `governance-enrich-ticket`, `governance-check-graph`, `elaboration-upsert-user-story`, `create-upsert-documentation`, `create-code-generation`.
- **[done]** Rewrote every interlocking cross-reference to the new names (the orchestrator names all five other skills; also fixed straggler refs in `docs/ai-sdlc/*`).
- **[done]** Added each skill's **persona** role-framing (Lead / Architect / PM / Tech Writer / Developer / any) and a **Next steps** section (the per-skill next-step convention).
- Landed in #75836.

## Step 3 - `run-sdlc` navigator skeleton  [done]
- **[done]** `.claude/skills/run-sdlc/SKILL.md`: resolves the anchor (a Jira key or a front-matter'd artifact), traverses the graph (repo `story:`/`relates:` + Jira issue links incl. story-to-story), and reports the current phase + the next `/<phase>-<step>` + owning persona. Read-only; a guide, not an autopilot.
- Dispatch (running the next step, parallel on Claude) is deliberately deferred to Step 5.
- Pending: eval-validation with the draft-only harness before relying on it (cross-cutting).
- Landed in #75836.

## Step 4 - Highest-leverage new step-skills  [done]
- **[done]** `elaboration-upsert-api-contract` (Architect) - OpenAPI spec under `specs/`, PascalCase per C06, 200/problem+json per C07, joined to the graph via a `relates.spec` edge (the spec file itself stays plain OpenAPI, no front-matter).
- **[done]** `elaboration-upsert-user-story-acceptance-criteria` (PM/QA) - testable ACs in the story's Success Criteria + the `[Trait("Story")]` + `[Trait("AC")]` scheme that hooks the deferred AC-coverage gate.
- **[done]** `discovery-synthesize-research` (Analyst) - upstream study -> Jira Discovery/Spike anchor holding link + summary, feeding elaboration.
- Authored in parallel via subagents (each read the plan + graph schema + a style exemplar); reviewed for accuracy, name/em-dash/line-number checks pass. Pending: eval-validation (cross-cutting).
- Landed in #75836.

## Step 4b - Remaining step-skills (full framework scaffold)  [done]
- **[done]** Scaffolded every remaining step-skill so each phase has an invokable entry point, built in parallel via subagents and reviewed: discovery (`discovery-map-journey`, `discovery-upsert-hypothesis-canvas`, `discovery-link-prototype`), elaboration (`elaboration-link-mvp-design`, `elaboration-upsert-prd-document`), create (`create-sprint-planning`, `create-refactoring`, `create-verify-test-coverage`), mature (`mature-collect-release-feedback`, `mature-analyze-ticket`, `mature-resolve-ticket`).
- `elaboration-architecture` is intentionally NOT a separate skill: `governance-author-adr` serves it (per the plan map and that skill's own persona line).
- **Tradeoff recorded for future sessions:** this was a deliberate "scaffold the whole framework" choice over the plan's incremental "highest-leverage-first" sequencing. The cost is more trigger surface (more skills the model selects among) and skills for phases not yet exercised; several (`map-journey`, `refactoring`, `sprint-planning`) are thin wrappers over tools the agent already has. `link-prototype` and `link-mvp-design` were pruned (2026-06-03) for needing a visual design tool the team does not have. Prune or merge any that under-earn their place once the workflow is in real use.
- All 20 skills now share the taxonomy: `<phase>-<step>` names, a persona line, and a Next steps section. Pending: eval-validation (cross-cutting).
- Landed in #75836.

## Documentation wiki (llmwiki pattern)  [done; CI gate deferred]

Adopted Andrej Karpathy's LLM Wiki pattern for the `docs/` (technical) + `docs-functional/` (functional)
layers - the LLM-maintained knowledge layer, complementary to the artifact graph. Contract:
`docs/ai-sdlc/docs-wiki.md`.

- **[done]** `create-upsert-documentation` rewritten as the wiki-maintainer: index-first, single-pass synthesis,
  portable relative cross-links, query-filing, concept/entity pages (kept the dual-layer routing).
- **[done]** `MindNova/toolsBuild-DocsIndex.ps1` generates `docs/index.md` (60 pages) and
  `docs-functional/index.md` (6 pages) deterministically, with a `-Check` drift mode; indexes committed.
- **[done]** `MindNova/toolsCheck-DocsWiki.ps1` lints orphans + broken relative links (ignores links shown
  inside code spans/fences; exempts `index.md`, `README.md`, templates).
- **[done]** `governance-check-graph` now runs all three link checks (artifact graph + docs-wiki lint +
  index drift) - one link-integrity skill, no new trigger surface.
- **[deferred]** CI gate: add `Check-DocsWiki.ps1` + `Build-DocsIndex.ps1 -Check` to
  `code-analysis-pipeline.yml` once the punch-list below is cleared and the branch policy is wired (the "later CI").
- **[proposed]** constitution clause **C10** ("docs are maintained as a linted wiki") - awaiting sign-off.
- **[done] Punch-list cleared** (the 3 pre-existing broken links the lint caught): repointed
  `domain-model.md` to the existing `glossary.md` (no duplicate created), and added
  `docs-functional/getting-started.md` and `docs-functional/troubleshooting.md` (the latter cross-links
  the technical `docs/troubleshooting.md`). `Check-DocsWiki` now passes (71 pages, no orphans or broken links).
- Adaptation: portable relative markdown links instead of Obsidian `[[wikilinks]]` / Dataview, because our
  docs render in Azure DevOps and the IDE.

## Step 5 - Multi-agent orchestration  [done]
- **[done]** Authored the dispatch contract `docs/ai-sdlc/orchestration.md`: the independence rule (when steps fan out vs must serialize), the cross-tool mechanism (parallel via subagents on Claude, sequential on Copilot and other agents), the gates (each side-effectful leaf still confirms on its own - parallelism never relaxes a gate), the "not an autopilot" posture, and result aggregation.
- **[done]** `run-sdlc` gained an opt-in dispatch step: it stays read-only for navigation and, on explicit request, hands the next step(s) off to the owning step-skill (which writes behind its own gates) - parallel on Claude for genuinely independent steps. It routes; it does not author.
- **[done]** `create-code-generation` now references the contract for parallel fan-out of independent slices, with the gates-don't-relax rule; `plan.md` points at `orchestration.md` as the authoritative contract, which joined the docs wiki (indexed, cross-linked, lint passes).
- **Design choice recorded:** dispatch is **human-gated routing, not an autopilot** (L3 gating posture), consistent with the deferred-adoption stance and `run-sdlc`'s "a guide, not an autopilot" identity. Unattended end-to-end autonomy is intentionally NOT built; it is a later maturity level, earned once the gates exist (Step 6) and the team trusts the flow.
- Pending: eval-validation of the dispatch behaviour (cross-cutting); real exercise on a live story once the contract is adopted (Step 1).
- Landed in #75836.

## Step 6 - The gate phase (L3 -> L4 enforcement, Azure DevOps)  [in progress: gates wired in-branch; full enforcement still blocked on cleanup + PAT + ADO admin]
- **[done] Doc gates blocking** in `code-analysis-pipeline.yml` (trigger on `main` + `ai-sdlc`): C02 line-citations, artifact-graph link resolution, C10 docs-wiki (no orphans/broken links) + index-in-sync, C11 no em-dashes. All pass today, so they enforce now.
- **[done] Code/API/coverage gates wired advisory** (`continueOnError: true` - report but do not fail the build), because the codebase has pre-existing findings: C05 `LogMindNova` (29 raw `LogLevel.*` calls), C04 ARG KQL escaping (5 sites), C06 PascalCase wire names (2 candidates: `userId`, `senderUpn`), C09 coverage (>= 80%, below today). Story-trait coverage is report-only (fails only on a malformed `[Trait("Story")]` key; 169 classes untagged). Scripts: `Check-LogMindNova.ps1`, `Check-KqlEscaping.ps1`, `Check-ApiPascalCase.ps1`, `Check-Coverage.ps1`, `Check-StoryTraits.ps1`. Recorded as advisory-active in `docs/constitution.md` v1.3.
- **[todo] Flip each advisory gate to blocking** after its findings are cleaned up: migrate the 29 log calls, escape or allowlist the 5 KQL sites, resolve the 2 wire names, raise coverage to 80% (or adopt the SonarCloud quality gate on new code), tag tests then run the story-trait gate with `-Strict`.
- **[todo] Flip security scans to blocking** (MSDO Trivy/Checkov, Semgrep, Gitleaks) - currently advisory and main-only; risky to flip before triaging their pre-existing findings.
- **[todo] Jira-side graph integrity** (story <-> spec/ADR backlinks, story-to-story links) via a PAT - the half of the artifact graph CI cannot yet check.
- **[todo] `adr-check` + `constitution-check`** jobs (C01 ADR-before-merge, machine constitution compliance).
- **[todo] Add the pipeline to the `main` branch policy** so every gate runs pre-merge (ADO admin); today CI runs on push to `ai-sdlc`, not as PR build validation.
- **Not covered:** OpenAPI code-vs-spec drift (needs build-time Swagger generation; `Check-ApiPascalCase.ps1` only checks wire-name casing, not drift).
- Depends on: Step 1 (contract adopted) + PAT + ADO admin for the still-blocked items. The advisory wiring is the early-feedback stage; maturity moves to L3/L4 as each gate flips to blocking.

## Step 7 - BMAD / ECC gap extraction  [todo]
- Gap-analyse what BMAD/ECC provide beyond our map; extract only the valuable templates (attributed per licence); drop BMAD/ECC + `/autopilot` references from the committed contract (move the autopilot section to a personal, gitignored `CLAUDE.local.md`).
- Depends on: nothing hard; can run alongside.

## Validation status (2026-06-03)

Validated with off-context subagent evals (the draft-only harness) in three layers; findings were fixed
in-branch on #75836. This is description-level and artifact-level validation, not adoption.

- **Layer 0 - static (scripted).** All 22 skills pass structure + hygiene (name = folder, trigger-rich
  description, persona line, Next-steps section, no em-dashes, no line-number citations); every
  `/<phase>-<step>` cross-reference resolves to a real skill folder. `Check-ArtifactGraph.ps1` was
  exercised on a fixture (pass path plus every fail path: bad key, missing or unknown phase, unresolved
  `relates`). Found and fixed: the story-key check was case-insensitive, now `-cnotmatch` enforces
  uppercase keys.
- **Layer 1 - routing (description discriminability).** Four subagent passes over 80 prompts
  (straightforward, paraphrased, adversarial keyword-bait). Across the passes every one of the 22 skills
  is the best pick for at least one prompt, so each has a discriminable niche; the thin-scaffold "dead
  weight" worry is disproven. Found and fixed: `governance-constitution` omitted its check mode from the
  description (re-routed constitution-compliance checks to `check-graph`), now advertised and
  disambiguated; `elaboration-upsert-user-story` had `disable-model-invocation: true` blocking auto-trigger,
  now removed. The adversarial pass and a later fresh pass confirmed the fixes route correctly with no over-trigger regressions.
- **Layer 2 - value (with-skill vs baseline, blind judges).** Two runs, 7 skill-tasks total, baseline
  given repo access. Combined tally: skills 3 wins, baseline 3 wins, 1 tie. Skills win when they enforce
  a discipline the baseline drops (`governance-author-adr` won both runs on honest Verification and
  accurate grounding). They tie or lose otherwise: `create-upsert-documentation` was a clear win, then a tie
  once C11 moved its main lever (no em-dashes) into a gate; `elaboration-upsert-user-story-acceptance-criteria` lost
  slightly (its trait scheme is real but it produced fewer, less complete ACs); `elaboration-upsert-api-contract`
  lost both runs on query-param casing (first PascalCasing, then over-snake_casing live camelCase
  params). Each upsert-api-contract loss was fixed in turn (C06 is bodies-only; new params snake_case; match a
  shipped parameter's existing wire name).
- **Key conclusion.** The skills are not uniformly better than a competent agent that reads the
  codebase; their value is discipline-enforcement, and it shrinks as a discipline moves into a gate (the
  documentation tie after C11 shows this directly). Durable value lives in the gates (constitution +
  linters), not the prompts. Skills remain useful as scaffolding and for cross-tool consistency.
- **Recurring finding turned into a gate.** Em-dashes were the repeated failure mode (the doc sweep, and
  a baseline emitting 34); now enforced by `Check-DocEmDashes.ps1` and clause **C11**.
- **Not yet validated:** Layer 4 (cross-tool) - confirm the same `SKILL.md` triggers in Copilot CLI on a
  real machine; this cannot be driven from a Claude session. Caveat: the evals are n=1 to 2 tasks and 1 judge
  per skill (signal, not proof), and the eval subagents likely did not inherit the always-on `AGENTS.md`
  context a real session has, so baselines are slightly understated on always-on rules.
- **Incremental re-validation (branch step).** After adding a "Branch" step (create a branch named for
  the bare Jira key before any code) to `create-code-generation` and `mature-resolve-ticket`, re-ran
  every runnable layer. Layer 0: 22/22 still clean, both skills' step numbering consistent. Layer 1:
  routing held and the new `branch` token even helped disambiguate a real build kickoff, but it also made
  `create-code-generation` over-trigger on a bare "branch for AZURE-1234" request; fixed with an explicit
  exclusion in its description (not for "a standalone git or branch request"), and a re-test confirmed
  bare git/branch prompts now route to none while real "begin building" kickoffs still route to
  `create-code-generation`. Layer 2: the with-skill plan beat a baseline that was given repo + `AGENTS.md`
  access, and the whole margin was branch hygiene - the baseline branched late (at PR time, on `main`),
  the exact mistake the step prevents - reconfirming that the skills' durable value is enforcing a
  discipline a competent baseline drops, not general superiority.
- **Incremental re-validation (enrich-ticket description-first rewrite, 2026-06-03).** After reworking
  `governance-enrich-ticket` so the ticket DESCRIPTION is the default target (splice shipped behaviour,
  scope, and decisions into the matching section), forbidding the downgrade of a description-worthy change
  to a comment-only, and requiring every run to end either written-and-verified or explicitly
  blocked/declined (no "proposed, nothing written" limbo), re-ran the runnable layers. Layer 0: 20/20
  skills clean (name=folder, trigger-rich description, persona line, Next-steps, no em-dashes, no
  line-number citations, cross-references resolve). Layer 1: three independent routing passes over 12
  boundary prompts agree 12/12; the skill wins all five of its niche prompts (add-to-ticket, "say what we
  actually shipped", link-with-why, fix-stale-title, record-decision-on-story) and is correctly rejected
  on the shipped-but-create-a-story, existing-ticket-but-triage, decision-as-ADR, and now-behaves-as-docs
  bait, so the broadened triggers added no over-trigger regression. Layer 2 (blind judge, with-skill vs a
  baseline given the house template and conventions): with-skill 12/12 vs baseline 7/12; the baseline
  clobbered the description (rebuilt it to the full template, reordered sections, invented Expected Value
  and Risks content) and stalled on a multi-question confirmation, the two failures the rewrite is built
  to prevent. Caveats unchanged (n=1 to 3, single Layer 2 judge, dry-run with no live Jira writes). Layer 4
  (Copilot cross-tool) still pending, not runnable from a Claude session.

## Cross-cutting / ongoing
- Validate each new or changed skill with the eval harness before relying on it. The harness is now a
  committed artifact, not a chat-thread "draft-only" one: Layer 0 is the deterministic
  `MindNova/toolsCheck-SkillHygiene.ps1` gate (with `Check-SkillHygiene.Tests.ps1`), and Layers 1-2
  (routing discriminability, value vs baseline) plus the method, prompt taxonomy, and scoring rubric
  live in the `governance-validate-skills` skill. Layers 0-2 done 2026-06-03 (see Validation status
  above); Layer 4 (Copilot cross-tool) still pending, not runnable from a Claude session.
- The gate scripts under `MindNova/tools` now carry Pester tests with pass and fail fixtures
  (`Check-*.Tests.ps1`), so the verifiers are themselves verified; run them with
  `Invoke-Pester -Path MindNova/tools`. Previously only the doc/graph gates were exercised ad-hoc on
  uncommitted fixtures.
- Keep the `AGENTS.md` core small (the always-on token budget).
- Tighten the artifact-graph rules (per-phase required edges) only as adoption grows; do not force front-matter everywhere at once.
