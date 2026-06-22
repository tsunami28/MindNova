# Architecture Decision Records

This folder records load-bearing technical decisions for MindNova. An ADR captures *why* a decision was made, the alternatives that were rejected, and how to verify a change in that area. It is the durable memory that stops the team (and AI agents) from relitigating or silently reverting a settled decision.

## When to write one

Write an ADR when a decision is **load-bearing**: getting it wrong would make dependent code wrong, and the reasoning is not obvious from the code alone. Typical triggers:

- A framework or runtime behaviour forces a non-obvious structure (e.g. ADR 0001: Blazor render mode forcing two authorization layers).
- You chose one approach over a plausible alternative and the next person would reasonably pick the other.
- A past attempt failed in a way that is not visible in the current code.

Do **not** write an ADR for routine work covered by the conventions in `docs/conventions/`, for reversible local choices, or for anything a code comment or good naming already makes clear.

## How to write one

1. Copy `0000-template.md` to `NNNN-short-title.md`, where `NNNN` is the next number in sequence.
2. Fill in every section. The `Alternatives considered` and `Verification` sections are the ones future readers rely on most; do not skip them.
3. Open it as `Proposed`, move to `Accepted` when the decision is agreed.

## Conventions

**Append-only.** An accepted ADR is a point-in-time record. Do not rewrite its conclusions. If a decision changes, write a new ADR that supersedes the old one, set the old one's `Superseded by` field, and set the new one's `Supersedes` field. The history matters.

**Use stable anchors, not volatile pointers.** This is the rule we learned the hard way:

- **Avoid line numbers.** "`App.razor` line 30" rots the moment anyone edits above it. Reference the `<Routes>` element instead - a reader can grep it regardless of line drift.
- **Avoid unmerged or dropped commit hashes.** A commit that never reached `main` (an abandoned experiment, a reset-away branch state) becomes unrecoverable once local reflogs expire. Describe the approach instead.
- **Merged PR numbers and merged commit hashes are fine.** They are immutable and survive branch deletion. Prefer the PR number.
- **Prefer symbol and element names** (`<Routes>`, `IsAuthorizedFor`, `CloudSpaceAccessHandler`) and **file paths** over anything position-based.

**Link, don't paste.** Reference other artefacts (PRs, docs, diagrams) rather than copying their content in.

**Keep it concise.** One decision per ADR. Split if a record is trying to cover two.

## Index

| ADR | Title | Status |
|---|---|---|
| [0008](0008-azure-sql-database-serverless.md) | Azure SQL Database (serverless) as the MindNova backend database | Accepted |
| [0009](0009-avm-registry-modules-for-bicep.md) | Use Azure Verified Modules from the Bicep public registry | Accepted |
