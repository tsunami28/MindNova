# AGENTS.md - Application.MindNova

Single source of engineering instructions for this repository. GitHub Copilot reads this file
natively; Claude Code reads it via the root `CLAUDE.md` (`@AGENTS.md`). `CLAUDE.md` and
`.github/copilot-instructions.md` only point here, so the guidance never diverges.

This file stays lean and always-loaded: orientation, the non-negotiable constitution, and
pointers. Detailed conventions, architecture, and decisions live in `docs/` and load on demand.

## Communication

Get to the point and keep it dense. Lead with the answer, decision, or result; put the reason after
it, and only if it changes what the reader does next. Cut filler, hedging, courtesy phrasing ("sure,
happy to help"), and restating the question. Compress an explanation to its load-bearing fact rather
than a paragraph around it. State assumptions, risks, and anything unverified plainly and briefly.
High signal per word is the goal, not fewest words: plain, complete sentences, short and direct, not
telegraphic and not curt.

## Project Overview

**MindNova** is a digital solution that supports psychotherapy consultancies by:

Managing client records and history
Scheduling and tracking therapy sessions and visits
Handling calendar planning and availability
Enabling secure documentation of treatment notes and progress
Supporting resource and therapist allocation
Providing reporting and analytics for operational insights

## Build & Test Commands

Run the CI code-analysis gates locally before pushing (catches doc, skill, graph, and house-style violations in seconds, not a pipeline cycle): `pwsh MindNova/toolsInvoke-LocalGates.ps1`. Enable them on every push once per clone with `pwsh MindNova/toolsInstall-GitHooks.ps1` (or `git config core.hooksPath .githooks`): a tool-agnostic pre-push hook; bypass with `git push --no-verify`. See the `create-run-gates` skill.

## System Map

## Tech essentials

- **.NET 10** (`net10.0`), nullable reference types disabled (`<Nullable>disable</Nullable>`) in all project files; do not use `string?`, `int?`, or any nullable annotation on reference types. Nullable value types (`int?`) are permitted only where the domain requires an absent value.
- **Central Package Management** (`MindNova/Directory.Packages.props`); lock files tracked.
- **Testing**: xUnit + Moq + AutoFixture; tests mirror `src/` under `tests/`.
- **Container runtime**: Podman (not Docker). Integration tests use Testcontainers with Podman; set `TESTCONTAINERS_RYUK_DISABLED=true`. See `docs/local-dev-setup.md` for setup and troubleshooting.
- **CI/CD**: GitHub Actions (CodeQL, Trivy, Coverlet).
- **MindNova runs in** dev, prd. Branches: `main` deploys to prd; `develop` (or feature branches merged to develop) deploys to dev. There is no staging environment. Environment-specific config keys are suffixed `-dev` and `-prd` respectively.

Full C# / config / serialization / logging / testing detail: `docs/conventions/csharp.md`.

## Constitution

Non-negotiable for every change (rationale, enforcement, and history in `docs/constitution.md`):

- **C01** Load-bearing architectural decisions require an ADR in `docs/adrs/` before merge.
- **C02** No source line numbers in any documentation; reference by file path + symbol.
- **C03** No hardcoded secrets; use App Configuration / Key Vault / environment.
- **C04** Never interpolate unvalidated user input into ARG KQL; validate and `EscapeKqlString`.
- **C05** Log via `_logger.LogMindNova(...)`, never `LogInformation(...)` or other `LogLevel.*()`.
- **C06** JSON wire format is PascalCase; no `[JsonPropertyName]` unless the name must differ.
- **C07** Controllers are thin, one domain each; all successful responses use HTTP 200 regardless of HTTP method (201/202/204 are not used). Errors are returned as `ProblemDetails`.
- **C08** Private DNS zones live in hub subscriptions; never check zone-sub == landing-zone-sub; only hub-peered VNets count.
- **C09** Changed code ships with mirrored xUnit tests; target >= 80% line coverage on changed projects. If a file or project is structurally untestable (generated code, infrastructure bootstrap, platform interop), add `[ExcludeFromCodeCoverage]` or a `.coverletrc` exclusion and note the exclusion in the PR description. Do not write hollow tests to hit 80%.
- **C10** Docs are maintained as a linted wiki: `docs/` + `docs-functional/` indexed, relative cross-links, no orphans or broken links.
- **C11** Documentation contains no em-dash characters (U+2014); use ' - ', commas, colons, or parentheses.

## Work items and pull requests

### Local work items (no Jira)

Work items (epics, stories, spikes, bugs) live as markdown files under `docs/discovery/`. This is the single source of truth for planning and tracking; no external tracker is connected.

- **Directory layout:**
  - `docs/discovery/epics/<epic-slug>.md` - epic definitions
  - `docs/discovery/stories/<epic-slug>/<story-slug>.md` - stories under their epic
  - `docs/discovery/spikes/<spike-slug>.md` - discovery/spike items (research anchors)
  - `docs/discovery/backlog.md` - ordered backlog (links to story files, status, points)
- **Anchor key convention:** each work item has a local key in its front-matter (`key: MN-<number>`), sequential, assigned on creation. Use `MN-` prefix (MindNova). The key is the stable reference for `story:` edges in artifact front-matter.
- **Keep the work item current.** As a change progresses, route durable enrichment (scope changes, decision links, shipped behaviour) into the work item file through `governance-enrich-ticket`, the single writer for those edits.
- **Status tracking:** each work item front-matter carries `status:` (backlog, refined, in-progress, done). The backlog file references items by key and reflects priority order.

### Creating a PR (canonical path)

- Use the helper `MindNova/tools/New-MindNovaPullRequest.ps1` (entry function `New-MindNovaPullRequest`). It builds the PR body, creates the PR via `gh pr create`, reads it back, and throws on an empty description. Prefer it over hand-rolling `gh pr create`.
- **If you invoke `gh` directly:** `gh pr create --title "..." --body "..." --base main`. The GitHub CLI handles multi-line bodies reliably; pass the body as a single string or pipe from a file with `--body-file`.
- **Always verify the description after create.** Read back: `gh pr view <number> --json body | ConvertFrom-Json` and confirm `.body` is non-empty and complete.
- **Fallback if gh CLI is not authenticated.** Output the pre-filled browser URL `https://github.com/<owner>/<repo>/compare/main...<branch>?expand=1` and stop. Do not proceed with post-PR steps until the user confirms the PR number.

## Where the detail lives

- **Conventions** (how we write code): `docs/conventions/` - `csharp.md`, `clean-code.md`, `api.md`, `comments.md`, `bicep.md`, `portal-ui.md`.
- **Decisions** (why, with alternatives): `docs/adrs/` - start at `docs/adrs/README.md`.
- **Constitution** (the non-negotiables in full): `docs/constitution.md`.
- **Architecture and operations**: `docs/architecture.md`, `docs/deployment.md`, `docs/environments.md`, `docs/troubleshooting.md`, `docs/work-instructions/`.
- **Functional docs** (product behaviour): `docs-functional/`.
