---
key: MN-12
type: story
status: in-progress
epic: MN-1
points: 3
priority: high
labels: [MindNova]
relates:
  - key: MN-9
    why: "needs the project to exist before CI can build it"
---

# CI Pipeline (GitHub Actions)

📌 Background

* Every PR should be validated automatically: build, test, and static analysis.
  Without CI, broken code can reach main undetected.
* Depends on MN-9 (project must exist to build).

🎯 What's the Goal?

* As a developer,
* I want a GitHub Actions workflow that builds, tests, and runs security analysis on
  every PR to main,
* So that regressions, vulnerabilities, and code quality issues are caught before merge.

💡 Expected Value

* Fast feedback loop. Confidence that main is always green. Security scanning from day one.

✅ Success Criteria

* AC-1: A GitHub Actions workflow file exists at .github/workflows/ci.yml.
* AC-2: The workflow triggers on pull requests targeting main.
* AC-3: The workflow restores, builds, and runs all xUnit tests.
* AC-4: Test results are published as a workflow summary or check annotation.
* AC-5: Coverlet produces a coverage report (cobertura format) and uploads it as a
  workflow artifact.
* AC-6: CodeQL analysis runs against the C# codebase and reports findings as code
  scanning alerts.
* AC-7: Trivy scans dependencies for known vulnerabilities and fails the workflow on
  critical/high severity CVEs.
* AC-8: The workflow completes in under 5 minutes for the current scaffold.
* AC-9: A failing test causes the workflow to fail (the PR cannot merge green with a
  broken test).

Test trait mapping:
- AC-1: build-time verification (file exists in repo); not unit-tested.
- AC-2: build-time verification (trigger config in YAML); not unit-tested.
- AC-3: verified by the workflow itself running successfully on a PR.
- AC-4: verified by inspecting PR check output; not unit-tested.
- AC-5: verified by checking workflow artifacts after a run; not unit-tested.
- AC-6: verified by GitHub code scanning tab showing the CodeQL check; not unit-tested.
- AC-7: verified by a Trivy step producing output; not unit-tested.
- AC-8: verified by observing workflow duration; not unit-tested.
- AC-9: `[Trait("Story","MN-12")]` + `[Trait("AC","AC-9")]` - a deliberately failing test
  on a test branch confirms the workflow reports failure (manual verification, then revert).

🛠️ How we'll do it

* Create .github/workflows/ci.yml with a single job targeting ubuntu-latest.
* Use actions/checkout, actions/setup-dotnet, then dotnet restore/build/test.
* Configure Coverlet via test project settings (cobertura output).
* Add github/codeql-action (initialize, autobuild, analyze) for C#.
* Add aquasecurity/trivy-action scanning the repo in fs mode for vulnerabilities.
* Upload coverage report via actions/upload-artifact.

⚠️ Risks & Blockers

* CodeQL and Trivy add build time; monitor the 5-minute budget.
* Initial Trivy scan may flag transitive dependency CVEs with no fix available;
  configure a .trivyignore for acknowledged findings if needed.

## Artifacts and references

* CI workflow - .github/workflows/ci.yml
* Branch - MN-12
