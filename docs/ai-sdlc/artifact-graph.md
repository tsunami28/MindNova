# AI-SDLC artifact graph

Work products live across systems (Jira, this repo, Confluence, Figma, Dovetail). To make them
navigable as one graph, we anchor everything on a stable work-item key and declare links in
machine-readable front-matter. A linter validates the links; `/run-sdlc` (later) traverses them.

This is incremental: an artifact joins the graph by **opting in** (adding the front-matter below).
The linter only validates artifacts that have opted in, so existing docs are untouched until they do.

## Anchor: the Jira work-item key

Every artifact references a Jira key (e.g. `AZURE-1234`). The Jira item is the spine and the hub
that the PO/PM and developers share. Anchor at the right level of the hierarchy:

- **Epic** for an initiative, **Story** for a slice, **Spike/Discovery** issue for a research step.
- Research can start in a tool without a ticket (e.g. Dovetail). When it becomes actionable, create
  a Discovery/Spike Jira item and link the study from it (the Jira item holds the link and a short
  synthesis summary). The graph stores the **edge and summary, not the content**, so a developer
  navigates from Jira without needing access to the upstream tool. Where the upstream tool cannot
  store the Jira key, the Jira item is the authoritative holder of that edge.
- Do not create a ticket per micro-step. Use the hierarchy; over-ticketing is bureaucracy, not traceability.

## Story-to-story links (in Jira)

Work items relate to each other, and that is a first-class edge of the graph. Examples: the
development Stories that came out of a research Spike link back to that Discovery/Spike story; a
Story that depends on another links it. These edges are maintained **in Jira** as native issue links
(Relates / Blocks / Depends on), each with a one-line reason, by the `governance-enrich-ticket` skill
(`createIssueLink` with the why in the link comment).

- The canonical store for story-to-story edges is **Jira issue links**, not repo front-matter, so the
  PO/PM see them in the board and developers see them on the ticket.
- A repo artifact may optionally mirror the most relevant ones in front-matter as
  `relates.stories: [AZURE-1200, AZURE-1300]`, but Jira is the source of truth for these edges.
- `/run-sdlc` follows Jira issue links when traversing, so navigating from a dev story reaches its
  originating Spike (and its synthesis + upstream-tool link) without leaving the graph.
- Integrity of these edges needs the Jira API and is therefore part of the **deferred Jira-side check**
  (gate phase, with a PAT); until then they are upheld by `governance-enrich-ticket` discipline.

## Front-matter convention (repo artifacts)

Add YAML front-matter to a repo artifact (PRD, spec, MADR, doc) to put it in the graph:

```yaml
---
story: AZURE-1234            # required: the anchor work-item key
phase: elaboration           # required: discovery | elaboration | create | mature | governance
step: upsert-api-contract             # optional: the process step
relates:                     # optional: edges to other artifacts (repo paths or URLs)
  spec: specs/cloudspace-compare.openapi.yaml
  adr: docs/adrs/0007-cloudspace-compare.md
  design: <repo path or a design-tool link>
  research: <a research-tool, Confluence, or repo link>
  docs: docs/api/cloud-spaces.md
---
```

- `relates` values are either **repo-relative paths** (validated to exist) or **URLs** (presence only;
  external tools cannot be content-validated from CI).
- The Jira side of the edges is maintained by the `governance-enrich-ticket` skill (outbound links + the why).
  Repo artifacts carry the inbound `story:` and `relates:`. Traversal works both directions.

## Phases

`discovery` | `elaboration` | `create` | `mature` | `governance` (cross-cutting). See
`docs/ai-sdlc/plan.md` for the phase/step map.

## What the linter enforces (v1)

`MindNova/toolsCheck-ArtifactGraph.ps1` scans `docs/`, `docs-functional/`, and `specs/`, and
validates every artifact there that declares `story:` (so functional docs are first-class graph
nodes, not just technical ones):

1. `story` is a valid work-item key (`^[A-Z][A-Z0-9]+-\d+$`).
2. `phase` is present and one of the known phases.
3. Each `relates.*` repo path resolves to a file in the repo; each URL is well-formed.

It exits non-zero (fails the build) on any violation, and is a no-op on artifacts that have not opted in.

## Intended required edges (target, tightened as adoption grows)

These are the edges the graph should eventually require per phase (not all enforced in v1):

- A Story entering `create` links a machine-readable spec (`relates.spec`).
- An architectural decision is recorded as an ADR and backlinked from its story.
- A design-phase artifact links the design source (`relates.design`).
- Every spec and ADR references its `story`.
- A development Story is linked in Jira to the Discovery/Spike story it originated from (story-to-story edge).

## Enforcement is shared

The same script runs two ways (one rule, one source of truth):
- **Locally:** the `governance-check-graph` skill (Claude Code / Copilot CLI both run the script).
- **In CI:** a gating step in `MindNova/pipelines/Common/code-analysis-pipeline.yml`.
