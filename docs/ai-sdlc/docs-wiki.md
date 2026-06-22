# AI-SDLC documentation wiki

The `docs/` (technical) and `docs-functional/` (functional) layers are maintained as a persistent,
interlinked, **LLM-maintained knowledge base** - a wiki in the sense of Andrej Karpathy's LLM Wiki
pattern (https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f): the agent compiles
understanding into cross-linked pages once, rather than re-deriving it from the code every time.

This is the **knowledge** layer. It is complementary to the **traceability** layer in
`docs/ai-sdlc/artifact-graph.md`, not a replacement: the graph answers "for AZURE-1234, what spec /
ADR / docs exist", the wiki answers "how does the platform actually work". A single page can be
both - a wiki page (index entry + cross-links) that also carries `story:` front-matter (a graph node).

## The three layers (Karpathy's model, mapped)

| llmwiki layer | here |
|---|---|
| Raw sources (read, never the wiki's own content) | the codebase, `docs/adrs/`, Jira, PRs |
| The wiki (LLM-owned, synthesised, cross-linked) | `docs/` (technical) and `docs-functional/` (functional) |
| The schema (defines structure and conventions) | this file, `AGENTS.md`, and the `create-upsert-documentation` skill |

## Conventions

### Index, read first
Each layer has a catalog the agent reads before answering or editing, so it finds the right page
instead of spawning duplicates or orphans:
- `docs/index.md` and `docs-functional/index.md`.
- One entry per page: a relative link plus a one-sentence summary, grouped by area.
- **Generated, not hand-curated.** `MindNova/toolsBuild-DocsIndex.ps1` emits them from the pages, so
  the index cannot drift from reality. Regenerate after adding, removing, or retitling a page.

### Cross-links, portable (not Obsidian)
Pages link related pages with **relative markdown links** (e.g. `[lifecycle](../lifecycle.md)`), not
`[[wikilinks]]`. Our docs render in GitHub and the IDE, where `[[...]]` and Dataview front-matter
queries do not resolve; relative links render everywhere and are validated by the lint. This is the
one deliberate deviation from Karpathy's Obsidian-based original.

### Front-matter
A page that joins the artifact graph carries the graph front-matter (`story:` and friends, per
`artifact-graph.md`). A wiki page may also carry a lightweight `summary:` line for the index. Keep
front-matter minimal and portable; do not add Dataview-only fields.

## Maintenance principles (the create-upsert-documentation skill embodies these)

- **Single-pass synthesis.** When a change lands, read it once and update *all* affected pages plus
  the index together, not one page in isolation.
- **Synthesis over storage.** A page holds evolved understanding, not a raw dump; link to the source
  (code symbol, ADR, ticket) for the detail rather than pasting it.
- **Query-filing.** A good answer to a real question becomes (or updates) a page, so knowledge
  compounds in the wiki instead of disappearing into chat history.
- **Concept and entity pages.** `docs-functional/` already holds concept pages (domain model,
  glossary, lifecycle, personas); grow that set as concepts emerge, each cross-linked into the web.

## Lint (the integrity check)

`MindNova/toolsCheck-DocsWiki.ps1` validates wiki health (Karpathy's "periodic linting"):
- **Orphans:** a page absent from its layer's index, or that nothing links to.
- **Broken links:** a relative markdown link that does not resolve to a file.
- **Index drift:** the committed index does not match what the generator would emit.

A sibling script, `MindNova/toolsCheck-DocEmDashes.ps1`, enforces the **no-em-dash house style**: it
flags any em-dash (U+2014) character in `AGENTS.md`, `docs/`, or `docs-functional/`. The house style
uses a spaced hyphen ' - ', commas, colons, or parentheses instead.

These exit non-zero on any violation and are run locally by the `governance-check-graph` skill (which
runs the artifact-graph, docs-wiki, and em-dash checks). The pre-merge CI gate is **deferred**, wired
the same way as the C02 and artifact-graph gates when it lands.

## Automation (Skills now, CI later)

| Concern | Mechanism | Status |
|---|---|---|
| Maintain the wiki (synthesis, cross-links, query-filing) | `create-upsert-documentation` skill | active |
| Generate the indexes | `Build-DocsIndex.ps1` | active |
| Check wiki integrity locally | `governance-check-graph` skill (runs `Check-DocsWiki.ps1`) | active |
| Enforce no-em-dash house-style | `Check-DocEmDashes.ps1` (via `governance-check-graph`) | active locally (CI gate deferred) |
| Enforce wiki integrity pre-merge | CI gate in `code-analysis-pipeline.yml` | deferred |
| Make "docs maintained as a linted wiki" non-negotiable | constitution clause C10 | active (CI gate deferred) |
