---
name: governance-author-adr
description: >
  Suggest and author Architecture Decision Records (MADR-style) in docs/adrs/.
  Use when a load-bearing decision is being made or has just been made ("we
  decided to...", "let's go with X over Y", "should this be an ADR?", "record
  this decision", "write an ADR", "supersede ADR NNNN"), or when reviewing a
  plan/change that implies an unrecorded architectural choice. Also use to
  detect and flag decisions that ought to be recorded. Do NOT use for the
  project-wide constitution (that is the governance-constitution skill), for
  ordinary feature docs, or for tracker tickets.
---

# Author ADR

Owned by the **Architect** persona (also serves the architecture step in elaboration). Invoked as `/governance-author-adr`.

Detect, suggest, and author **Architecture Decision Records**. An ADR captures *why* a
load-bearing choice was made, so a future reader (or agent) does not re-litigate it or
unknowingly violate it. ADRs are decision history: append-only, superseded but not
deleted, read on demand (never preloaded into context).

## Use the repo's own template and rules

Before writing, **read the repo's ADR template and index** so output matches what is
already there:
- Template: `docs/adrs/0000-template.md` (or the repo's equivalent). Mirror its exact
  sections and frontmatter. Do not invent a different format.
- Index/README: `docs/adrs/README.md`. Follow its conventions for listing and status.
If the repo has no `docs/adrs/`, propose creating it with a template first (and say so).

Respect the repo's documentation rules, which for these repos include: **no source line
numbers anywhere** (reference code by file path + symbol name), and **no em-dashes** (use
commas, colons, parentheses, or " - "). Link to artefacts; do not paste them in.

## What counts as "load-bearing" (when to suggest an ADR)

Suggest an ADR when a change does any of these, and there is no existing ADR covering it:
- introduces, replaces, or splits an architectural pattern or boundary (e.g. a new
  handler/service split, a persistence choice, an auth model);
- makes a cross-cutting tradeoff a future reader would ask "why" about;
- **deviates from an existing convention or a constitution clause** (this especially
  needs a record, or a constitution amendment);
- chooses one technology/approach over a viable alternative with lasting consequences.

Do not propose ADRs for routine, reversible, or local choices. Noise erodes the corpus.
When unsure, ask: "would someone six months from now be confused or annoyed if this
choice were undocumented?"

## Modes

- **suggest** - state that a decision looks load-bearing, summarise it in one or two
  lines, and ask whether to author an ADR. Do not write anything yet.
- **author** - draft a new ADR and (on confirmation) write it.
- **supersede** - author a new ADR that replaces an older one.

## author workflow

1. **Allocate the number.** List `docs/adrs/`, find the highest `NNNN`, increment, zero-pad to 4 digits. Filename: `NNNN-<kebab-short-title>.md`.
2. **Fill the template from reality.** Complete every section of the repo template:
   - **Context** - the forces and constraints *as they are now*; verify load-bearing facts against the code before stating them (read the file/symbol). If a prior attempt failed, name it by merged PR number or description, never an unmerged commit hash.
   - **Decision** - what was decided, plainly; include a short code/config shape only if it removes ambiguity; name the files/types/patterns it touches.
   - **Consequences** - Positive, Negative (be honest; this is the section readers trust most), Neutral.
   - **Alternatives considered** - each with a one-sentence "rejected because".
   - **Verification** - how someone confirms a change in this area is correct; what tests do and do not cover.
   - **References** - files by path + symbol, merged PR numbers, external links.
3. **Set status and dates.** New decisions are usually `Proposed` (or `Accepted` if the team has already agreed). Set the date.
4. **Confirm the draft with the user**, then write the file.
5. **Update the index** (`docs/adrs/README.md`): add the new ADR with its status.
6. **Offer the backlinks.** Suggest running `governance-enrich-ticket` to link this ADR onto the originating story, and `governance-constitution` if the decision implies a new or changed non-negotiable. Do not perform those writes from here.

## supersede workflow

1. Author the new ADR as above; in its frontmatter set `Supersedes: ADR NNNN`.
2. In the old ADR, set `Status: Superseded` and `Superseded by: ADR <new>`. Keep the old ADR's body intact - it is history.
3. Update the index to reflect both statuses.
4. Confirm both diffs before writing.

## Guardrails

- **Verify before asserting.** Every load-bearing fact in Context/Decision must be checked against the codebase first. If you cannot confirm it, write it as an open question, not a fact.
- **Confirm before writing**, and never overwrite an existing ADR's history when superseding - only flip its status fields.
- **One decision per ADR.** If a change carries two independent decisions, write two.
- **No line numbers, no em-dashes, no pasted artefacts.** Match the repo template exactly.

## Next steps

- `/governance-enrich-ticket` (any) - backlink the new ADR onto its originating story and add a dated decision-log line.
- `/governance-constitution` (Lead) - only if the decision implies a new or changed non-negotiable.
- `/governance-check-graph` (Lead) - once the ADR is linked, confirm its `story:` and `relates:` front-matter resolves.
