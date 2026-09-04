# ADR 0011: RFC 7807 error payloads are exempt from the PascalCase wire format

**Status:** Accepted
**Date:** 2026-08-31
**Supersedes:** none
**Superseded by:** none

## Context

Constitution clause C06 requires the JSON wire format to be PascalCase. Two facts made the API
violate it silently, and a third made the clause impossible to satisfy everywhere.

First, `Program.cs` registered controllers with a bare `AddControllers()`. MVC configures
System.Text.Json with `JsonSerializerDefaults.Web`, which sets
`PropertyNamingPolicy = JsonNamingPolicy.CamelCase`. Nothing overrode it, so every response on
every endpoint was camelCase, against C06 and against the committed OpenAPI documents in `specs/`.

Second, the C06 gate `Check-ApiPascalCase.ps1` did not detect this. Part A scans only for
`[JsonPropertyName]` attributes whose value is not PascalCase, on the documented assumption that
"System.Text.Json serialises by property name (PascalCase) by default". That assumption is false
for MVC. With no such attributes in the codebase, the gate reported clean while the whole surface
was non-compliant. The gate's own help text already noted that code-vs-spec drift is out of scope.

Third, and the reason this needs a decision rather than a fix: the framework's `ProblemDetails`
type carries hardcoded `[JsonPropertyName("type")]`, `("title")`, `("status")`, `("detail")` and
`("instance")` attributes so that it conforms to RFC 7807 (now RFC 9457), the specification behind
the `application/problem+json` media type. Attributes take precedence over the naming policy, so
these members serialise lowercase no matter what policy is configured. The API returns
`new ProblemDetails { ... }` from 45 call sites across 8 controllers, and C07 mandates
`ProblemDetails` as the error shape. C06 and RFC 7807 cannot both be satisfied for error payloads.

A related inconsistency existed alongside this: `SessionsController.MapConflictError` returned an
anonymous object rather than a `ProblemDetails`, so conflict errors carried PascalCase members and
a different shape from every other error in the API.

## Decision

RFC 7807 wins for error payloads. C06 governs the domain surface only.

- Success payloads are PascalCase. `AddControllers()` now sets
  `options.JsonSerializerOptions.PropertyNamingPolicy = null`, which serialises property names
  verbatim.
- Error payloads follow RFC 7807: standard members (`type`, `title`, `status`, `detail`,
  `instance`) are lowercase, and extension members use lowerCamelCase alongside them.
- `MapConflictError` returns a real `ProblemDetails` and carries the conflicting session as
  extension members (`conflictingSessionId`, `conflictingStart`, `conflictingEnd`) via
  `Extensions`, so every error in the API has one shape.

```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
```

The `ProblemDetails` schemas in all six documents under `specs/` were corrected to lowercase to
match what the API emits, and `Check-ApiPascalCase.ps1` Part B now skips schemas whose name ends
in `ProblemDetails`.

## Consequences

**Positive:**

- The domain surface actually satisfies C06 for the first time; previously nothing did.
- Error responses are standards-compliant, so generic RFC 7807 clients and middleware parse them.
- All error responses now share one shape, including session conflicts.
- The committed specs match the runtime, removing drift the gate could never have caught.

**Negative:**

- The wire format is mixed: PascalCase for domain payloads, lowercase for errors. A reader seeing
  only one kind of response could reasonably infer the wrong rule for the other, which is precisely
  why this ADR exists.
- Consumers written against the old all-camelCase output break on domain payloads. The Blazor
  front-end was the only known consumer and has been updated.
- The `ProblemDetails` exemption in the gate is name-based (`*ProblemDetails`). A differently named
  error schema would be checked as if it were a domain type.

**Neutral:**

- Request binding is unaffected: `PropertyNameCaseInsensitive` remains `true` under the Web
  defaults, so camelCase request bodies from the Blazor client still bind.
- `MindNova.Web` detects errors through the shared `ProblemDetailsDetection.IsProblemDetails`
  helper, which matches either casing. It previously tested for `"Status"` and `"Title"`
  case-sensitively and so never recognised an error response at all.

## Alternatives considered

1. **Force PascalCase onto `ProblemDetails`.** Override the framework attributes with a custom type
   or a `JsonTypeInfo` modifier so errors are PascalCase too. Rejected because it produces a payload
   labelled `application/problem+json` that does not conform to the RFC, breaking any standards-aware
   client for the sake of internal consistency, and it requires maintaining a shadow of a framework
   type across 45 call sites.
2. **Leave the camelCase default and amend C06 to say camelCase.** Rejected because C06's rationale
   records that existing API and Portal consumers are locked to PascalCase, so this would be a
   breaking change across the surface, not a clarification.
3. **Keep the conflict responses as anonymous objects.** Rejected because it leaves two error shapes
   with different casing, and the anonymous object cannot carry the `application/problem+json`
   contract that C07 assumes.

## Verification

- `pwsh MindNova/tools/Check-ApiPascalCase.ps1` passes both parts. Part B needs the
  `powershell-yaml` module (`Install-Module powershell-yaml -Scope CurrentUser`); without it Part B
  reports that it could not validate and fails.
- Confirm a domain payload is PascalCase and an error payload is lowercase:

  ```powershell
  $b = @{ Email = "dev@mindnova.local"; Password = "DevPassw0rd!" } | ConvertTo-Json
  Invoke-WebRequest -Uri http://localhost:5193/api/auth/login -Method Post -Body $b -ContentType "application/json" -UseBasicParsing
  ```

  A correct password returns `{"Token":"..."}`; a wrong one returns
  `{"title":"Login failed","status":401,"detail":"..."}`.
- `dotnet test MindNova/tests/MindNova.Api.Tests/MindNova.Api.Tests.csproj` covers the conflict
  extension member names in `SessionConflictTests`.

**Not covered by tests:** no test asserts the casing of the domain surface as a whole. A future
regression of the naming policy would be caught only by the conflict test and by manual inspection,
because the C06 gate inspects `[JsonPropertyName]` attributes and is structurally blind to the
serializer's naming policy. Closing that blind spot is follow-up work.

## Related

- [Constitution](../constitution.md) - clauses C06 and C07
- [ADR index](README.md)
