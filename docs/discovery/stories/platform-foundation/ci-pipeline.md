---
key: MN-12
type: story
status: backlog
epic: MN-1
points: 3
priority: high
labels: [MindNova]
relates:
  - key: MN-9
    why: "needs the project to exist before CI can build it"
---

# CI Pipeline (Azure Pipelines)

📌 Background

* Every PR should be validated automatically: build, test, and static analysis.
  Without CI, broken code can reach main undetected.
* Depends on MN-9 (project must exist to build).

🎯 What's the Goal?

* As a developer,
* I want a CI pipeline that builds, tests, and runs SonarCloud analysis on every PR,
* So that regressions and code quality issues are caught before merge.

💡 Expected Value

* Fast feedback loop. Confidence that main is always green. SonarCloud enforces
  quality gate.

✅ Success Criteria

* Azure Pipelines YAML triggers on PR to main and develop.
* Steps: restore, build, test (with coverage via Coverlet), publish results.
* SonarCloud analysis integrated and quality gate enforced.
* Pipeline completes in under 5 minutes for the initial scaffold.
* Test results and coverage report visible in PR.

🛠️ How we'll do it

* Create azure-pipelines.yml at repo root.
* Use .NET SDK tasks: UseDotNet, DotNetCoreCLI (restore, build, test).
* Configure Coverlet for coverage output (cobertura format).
* Add SonarCloud prepare/analyze/publish tasks.
* Publish test results (TRX) and coverage to Azure DevOps.

⚠️ Risks & Blockers

* SonarCloud project must be created and service connection configured.
* Initial quality gate may flag zero-coverage on scaffold code; configure exclusions
  for Program.cs and similar bootstrap files.
