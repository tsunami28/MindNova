# AI-SDLC orchestration

How the two orchestrator skills dispatch the persona step-skills: in parallel on Claude Code,
sequentially everywhere else, always behind the same human gates. This is the operational companion
to the lifecycle map in [plan.md](./plan.md) - read that for *what* the phases and steps are, and this
for *how* they are run.

## Two orchestrators, two shapes

| Skill | Role | Dispatch shape |
|---|---|---|
| `run-sdlc` | Navigator | Reports where a work item sits and what is next; on explicit request, hands the next step (or a few genuinely independent next steps) off to the owning step-skill. Read-only itself. |
| `create-code-generation` | Conductor | Runs the known create-phase sequence (story, plan, TDD, docs, ADR, enrich, review, PR) with hard checkpoints; fans out independent slices within a step. |

Both **delegate the actual writing to the step-skill that owns it** - the orchestrators sequence and
gate, they do not author. `run-sdlc` answers "where are we and what is next" and can *start* the next
step on request; `create-code-generation` drives one change through a fixed sequence.

## Parallel vs sequential: the independence rule

Two steps may run in parallel only when they are genuinely independent:

- neither consumes the other's output, and
- they do not write the same artifact (file, spec, or ticket).

Independent (parallel-safe), for example:

- `discovery-synthesize-research` and `discovery-map-journey` - different artifacts, no data dependency.
- implementing two non-overlapping vertical slices of one story.
- reviewing or documenting several files at once.

Dependent (must serialize), for example:

- `elaboration-upsert-api-contract` then `create-code-generation` - the build consumes the spec.
- `create-code-generation` then `create-verify-test-coverage` when the tests exercise the new code.
- any two steps that write the same file or the same Jira issue.

The phase order (discovery, elaboration, create, mature) is itself a sequence; parallelism happens
*within* a phase or across genuinely independent branches, never by skipping a dependency.

## Mechanism, cross-tool (parallelism is an accelerator, never a requirement)

- **Claude Code:** fan out independent steps as subagents (the Agent/Task tool) or a workflow. Each
  subagent runs one step-skill in its own context and returns a result, which keeps the orchestrator's
  main context clean. The orchestrator waits for all, then aggregates.
- **Copilot / Codex / Gemini / other agents:** no subagent fan-out - run the same step-skills
  sequentially in one context. Identical skills, identical artifacts, just slower.

The framework must never *depend* on parallelism for correctness. A sequential run produces the same
result; the Claude fan-out only makes independent work faster and keeps context tidy. This is the
"degrade gracefully" rule: personas are cross-tool skills, the parallelism is a Claude accelerator.

## Gates (parallelism does not relax them)

Dispatch never silently performs a side-effectful or outward action. Each leaf step stops for the same
confirmations it would stop for when run alone:

- **Always confirmed, per action and per session:** creating or enriching a ticket, transitioning
  Jira, opening or pushing a PR, writing to any external system. One approval does not carry to the
  next action.
- **Normal editing discipline:** local artifacts (code, tests, docs, ADRs) are authored under the
  usual verify-before-assert rules, with no extra gate.

Running ten steps in parallel means ten independently-gated leaves, not one blanket approval. This is
the maturity model's L3 "gating" posture, not L5 autonomy.

## Not an autopilot (adopt before automate)

The orchestrator surfaces the plan - which steps, in what shape (parallel or serial), writing what -
and the human approves that shape before the fan-out. It does not chain the whole lifecycle unattended.
This is deliberate: the contract is still being adopted, the pre-merge gates are deferred, and
unattended end-to-end autonomy is a later maturity level, earned once the gates exist and the team
trusts the flow. Until then the orchestrators accelerate a human-driven flow; they do not replace the
human.

## Aggregation and failure

When steps run in parallel, the orchestrator waits for all of them, then reports each one's outcome
(done, blocked, or needs-a-decision) and the single merged next step. A blocked or failed parallel
step is surfaced with its reason, never silently dropped; the human decides whether to retry, reroute,
or stop.

## Related

- [plan.md](./plan.md) - the phase / step / persona / skill map this dispatches over.
- [artifact-graph.md](./artifact-graph.md) - the edges `run-sdlc` reads to decide what is next.
- [roadmap.md](./roadmap.md) - build sequence and status.
