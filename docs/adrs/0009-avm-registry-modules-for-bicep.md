# ADR 0009: Use Azure Verified Modules from the Bicep public registry

**Status:** Accepted
**Date:** 2026-06-22
**Supersedes:** none
**Superseded by:** none

## Context

MN-11 (Azure Infrastructure - Bicep/azd) requires Bicep modules for App Service, Azure SQL Server, and Key Vault. Two approaches exist:

1. Write local modules under `infra/modules/` that define each resource from scratch.
2. Reference Azure Verified Modules (AVM) directly from the Bicep public registry (`br/public:avm/res/...`) with pinned version tags.

AVM modules are Microsoft-maintained, WAF-aligned, tested in CI with e2e deployments, and cover the full parameter surface of each resource type. Writing local equivalents would duplicate that work and take on the maintenance burden of tracking API version changes, security defaults, and best-practice evolution.

The project's story (MN-11) already states "using modules based on Azure Verified Modules"; this ADR records the concrete decision on how they are consumed.

## Decision

Reference AVM modules directly from the Bicep public registry with pinned version tags. No local wrapper modules.

The `main.bicep` orchestrator calls each AVM module by registry reference:

```bicep
module webApp 'br/public:avm/res/web/site:<version>' = { ... }
module sqlServer 'br/public:avm/res/sql/server:<version>' = { ... }
module keyVault 'br/public:avm/res/key-vault/vault:<version>' = { ... }
```

Version pinning rules:

- Every registry reference includes an explicit version tag (e.g. `0.12.0`), never `latest` or an unpinned reference.
- Version bumps are deliberate: update the tag, review the changelog, and test before merging.
- The pinned version is recorded in `main.bicep`; no separate version manifest is needed.

## Consequences

**Positive:**

- No local module code to author, review, or maintain for standard resource types.
- AVM modules ship with WAF-aligned defaults (TLS 1.2, RBAC authorization on Key Vault, TDE on SQL) reducing the risk of insecure configuration.
- Upstream improvements (new API versions, security patches, parameter additions) are available by bumping the version tag.
- AVM modules are tested with end-to-end deployments in CI by the AVM team, reducing the verification burden on this project.

**Negative:**

- External dependency: a registry outage blocks `bicep build` and `azd up` for first-time resolution (cached modules are unaffected).
- Breaking changes between AVM versions require reading the upstream changelog and adapting parameters. The pinned version mitigates surprise breaks but means upgrades are manual.
- Less control over internal resource naming and structure than a hand-written module. If an AVM module does not expose a needed parameter, a feature request or a local override is required.

**Neutral:**

- The App Service Plan (`Microsoft.Web/serverfarms`) is not an AVM module with a separate registry reference; it will be defined as a resource directly in `main.bicep` or via the site module's server farm configuration, depending on the AVM site module's capabilities.

## Alternatives considered

1. **Local wrapper modules under `infra/modules/`.** Full control over every parameter and resource property. Rejected because: the three resource types (App Service, SQL Server, Key Vault) are well-covered by AVM, and the maintenance cost of tracking API versions and security defaults outweighs the marginal control benefit for a single-tenant app.

2. **AVM modules with local thin wrappers.** Import AVM via registry but wrap each in a local `.bicep` file that constrains the parameter surface to project needs. Rejected because: the indirection adds files without adding value when `main.bicep` already constrains which parameters are passed. If a future resource needs customisation beyond AVM's surface, a local module can be added for that resource alone.

## Verification

- `az bicep build --file MindNova/infra/main.bicep` succeeds, confirming registry references resolve and the template compiles.
- `az bicep restore --file MindNova/infra/main.bicep` pulls the pinned versions into the local module cache, confirming the version tags are valid.
- Review the AVM changelog for each pinned version before merging a version bump PR.

## References

- `docs/discovery/stories/platform-foundation/azure-infrastructure-bicep.md` - MN-11, the story consuming this decision.
- `docs/adrs/0008-azure-sql-database-serverless.md` - ADR 0008, the SQL tier decision that AC-5/AC-6 depend on.
- [Azure Verified Modules registry](https://github.com/Azure/bicep-registry-modules/tree/main/avm/res) - the upstream source.
- [AVM module index](https://azure.github.io/Azure-Verified-Modules/indexes/bicep/) - searchable index of all published AVM modules.
