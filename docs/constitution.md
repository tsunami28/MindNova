# Constitution - Application.MindNova

Non-negotiable engineering principles for this repository. These are the rules a change
must satisfy regardless of who (or which AI tool) wrote it. The short statement of each
clause also lives in the `## Constitution` section of `AGENTS.md` (always loaded by Claude
Code and GitHub Copilot); this file holds the rationale, enforcement, and history.

A clause earns its place here only if violating it causes real harm and the rule is not
already self-evident from the code. Everything else belongs in `docs/conventions/` (detailed
conventions) or `docs/adrs/` (one-off decisions), not here. Keep this list small.

**Enforcement legend:** `mechanism - status`. mechanism = CI gate | linter | hook | skill |
code review (manual). status = active | deferred (planned target). Deferred clauses are
tracked debt for the CI gate phase, not aspirations.

**Version:** 1.3  **Last updated:** 2026-06-03

---

### C01: Load-bearing architectural decisions require an ADR before merge

- **Statement:** A change that makes a load-bearing architectural decision must add or update an ADR in `docs/adrs/` (per `docs/adrs/README.md`) in the same change.
- **Rationale:** Stops the team and AI agents from relitigating or silently reverting settled decisions. The ADR is the durable "why".
- **Enforcement:** code review (manual) - active; deferred (planned: `adr-check` job in the ADO PR pipeline).
- **Added:** 2026-06-02  **Supersedes:** none

### C02: No source line numbers in documentation

- **Statement:** No documentation (markdown, ADRs, READMEs, wikis) may reference source by line number. Reference by file path and symbol name instead.
- **Rationale:** Line numbers rot on the next edit and silently mislead. Stable anchors survive.
- **Enforcement:** code review (manual) - active; deferred (planned: doc lint that flags `line \d+` / `:\d+` citations). Known debt: several `docs/conventions/*` files currently cite `CLAUDE.md` line numbers and must be fixed.
- **Added:** 2026-06-02  **Supersedes:** none

### C03: No hardcoded secrets

- **Statement:** No credentials, tokens, connection strings, or keys in source. Use App Configuration / Key Vault / environment as the codebase already does.
- **Rationale:** A leaked secret is a security incident and is hard to fully revoke.
- **Enforcement:** Gitleaks in CI - currently advisory (`taskfail: false`); deferred (planned: flip to blocking in the ADO gate phase).
- **Added:** 2026-06-02  **Supersedes:** none

### C04: User input is never interpolated into KQL unvalidated

- **Statement:** Validate format at the service layer (GUID, ARM regex, character allowlist) and escape values with `EscapeKqlString` before interpolating into Azure Resource Graph KQL, even when upstream validation exists.
- **Rationale:** Defense-in-depth against injection into ARG queries.
- **Enforcement:** code review (manual) plus `Check-KqlEscaping.ps1` (advisory CI gate, heuristic) - active; deferred (planned: flip to blocking once the flagged sites are escaped; full SAST is the longer-term path).
- **Added:** 2026-06-02  **Supersedes:** none

### C05: Logging goes through `LogMindNova`

- **Statement:** Use `_logger.LogMindNova(LogLevel.X, ...)`, never `_logger.LogInformation(...)` or other `LogLevel.*()` extensions.
- **Rationale:** `LogMindNova` adds structured class/method context that the platform's log queries depend on (see `docs/work-instructions/querying-application-insights.md`).
- **Enforcement:** code review (manual) plus `Check-LogMindNova.ps1` (advisory CI gate) - active; deferred (planned: flip to blocking once the legacy `LogInformation`/`LogError`/etc. call sites migrate to `LogMindNova`).
- **Added:** 2026-06-02  **Supersedes:** none

### C06: JSON wire format is PascalCase

- **Statement:** Serialized JSON uses PascalCase. Do not add `[JsonPropertyName]` unless the serialized name genuinely must differ (e.g. CosmosDB `id`, `ttl`, partition key paths).
- **Rationale:** All existing API and Portal consumers are locked to PascalCase; changing the wire format is a breaking change across the surface. Deliberate deviation from the Zalando snake_case MUST (see `docs/conventions/api.md`).
- **Enforcement:** code review (manual) plus `Check-ApiPascalCase.ps1` (advisory CI gate; flags non-PascalCase `[JsonPropertyName]` wire names, excludes external-integration DTOs) - active; deferred (planned: flip to blocking after the flagged slips are resolved). Code-vs-spec OpenAPI drift is NOT covered (it needs build-time swagger generation).
- **Added:** 2026-06-02  **Supersedes:** none

### C07: Controllers are thin, one domain each, errors-only non-200

- **Statement:** Controllers compose IDs, call services, return responses; business logic lives in services. One domain per controller. Non-error business outcomes return 200 with the outcome in the body; non-200 is reserved for real errors via `ProblemDetails`.
- **Rationale:** Keeps the API surface consistent and the Blazor error pipeline (`SendRequest`) reliable.
- **Enforcement:** code review (manual) - active. Detail in `docs/conventions/api.md`.
- **Added:** 2026-06-02  **Supersedes:** none

### C08: Private DNS hub-spoke boundary is respected

- **Statement:** Private DNS zones live in shared hub subscriptions; never validate that a zone's subscription matches the landing zone subscription. Only hub-peered VNets are relevant for IP/CIDR attribution.
- **Rationale:** Zones and landing zones are intentionally in different subscriptions; a "same subscription" check would be wrong and break attribution.
- **Enforcement:** code review (manual) - active.
- **Added:** 2026-06-02  **Supersedes:** none

### C09: Test changed code, mirroring the test layout

- **Statement:** New and changed behaviour ships with xUnit tests (Moq + AutoFixture), in a test project mirroring `src/`. Target line coverage on changed projects is >= 80%.
- **Rationale:** Coverage protects against regressions and is the basis for the planned AC-coverage gate.
- **Enforcement:** code review (manual) plus `Check-Coverage.ps1` (advisory CI gate) and SonarCloud - active for "tests exist"; the >= 80% threshold is deferred (planned: blocking via the SonarCloud quality gate on new code, or the script threshold, once real coverage reaches 80%). NOTE: not yet enforced as blocking, so treat >= 80% as the target we are moving toward, not a satisfied state.
- **Added:** 2026-06-02  **Supersedes:** none

### C10: Documentation is maintained as a linted wiki

- **Statement:** `docs/` and `docs-functional/` are maintained as one interlinked wiki: every page is reachable from its layer index, cross-links use relative markdown links (not `[[wikilinks]]`), and there are no orphan pages or broken relative links.
- **Rationale:** Documentation that drifts or dead-ends silently misleads. Treating the docs as a linted, LLM-maintained knowledge base (the llmwiki pattern, see `docs/ai-sdlc/docs-wiki.md`) keeps them navigable and trustworthy as the system grows.
- **Enforcement:** `create-upsert-documentation` + `governance-check-graph` skills (`Check-DocsWiki.ps1` + `Build-DocsIndex.ps1 -Check`) - active locally; deferred (planned: CI gate in `code-analysis-pipeline.yml`).
- **Added:** 2026-06-02  **Supersedes:** none

### C11: Documentation contains no em-dash characters

- **Statement:** Documentation (markdown in `AGENTS.md`, `docs/`, and `docs-functional/`) must not contain the em-dash character (U+2014). Use a spaced hyphen ' - ', commas, colons, or parentheses.
- **Rationale:** Em-dashes drift in from pasted or model-generated prose and are hard to spot by eye; a mechanical gate keeps the documentation house style consistent regardless of which agent or person writes it. Scope is documentation only: source files (`.cs`, `.razor`, assets, generated files) carry em-dashes and are not swept, so the clause stays enforceable rather than aspirational.
- **Enforcement:** `Check-DocEmDashes.ps1` via the `governance-check-graph` skill - active locally; deferred (planned: CI gate in `code-analysis-pipeline.yml`, sibling to the C02 doc lint).
- **Added:** 2026-06-03  **Supersedes:** none

---

## History

- 2026-06-02 - v1.0 - Initial constitution. Clauses C01-C09 lifted from the repo's existing standards in `CLAUDE.md` and `docs/conventions/*` during the AGENTS.md consolidation. No new rules invented; C02 and C09 carry explicit known-debt notes.
- 2026-06-02 - v1.1 - Added C10 (documentation maintained as a linted wiki) alongside the docs-wiki / llmwiki tooling (`create-upsert-documentation` maintainer, `Check-DocsWiki.ps1`, `Build-DocsIndex.ps1`). Enforced locally by skills; CI gate deferred.
- 2026-06-03 - v1.2 - Added C11 (documentation contains no em-dash characters), enforced by `Check-DocEmDashes.ps1` via `governance-check-graph` (active locally; CI gate deferred). Scope is documentation only; source files are out of scope. Added after the validation pass found em-dashes to be a recurring documentation failure mode.
- 2026-06-03 - v1.3 - Wired advisory CI gates for C04 (`Check-KqlEscaping.ps1`), C05 (`Check-LogMindNova.ps1`), C06 (`Check-ApiPascalCase.ps1`), and C09 (`Check-Coverage.ps1`) into `code-analysis-pipeline.yml`; each reports but does not block, because the codebase has pre-existing findings (C05: 29, C04: 5, C06: 2, C09: coverage below target). To be flipped to blocking after cleanup. Enforcement fields updated from deferred to advisory-active; no clause statements changed.
