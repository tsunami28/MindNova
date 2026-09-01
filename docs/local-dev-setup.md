# Local development setup

Prerequisites and configuration for running and testing MindNova on a developer workstation.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Podman](https://podman.io/) (container runtime for local SQL Server and integration tests)
- [GitHub CLI](https://cli.github.com/) (`gh`) for PR creation
- WSL 2 (required by Podman on Windows)
- `dotnet-ef` CLI: `dotnet tool install --global dotnet-ef`

Verify with:

```powershell
dotnet --version    # expect 10.0.x
podman --version
dotnet ef --version
```

## Podman setup (Windows)

MindNova uses Podman as its container runtime. Integration tests use Testcontainers, which starts SQL Server containers automatically via Podman.

### First-time setup

```powershell
podman machine init
podman machine start
```

### Environment variables for Testcontainers

Testcontainers needs to know where the container runtime socket is. Set these before running tests:

```powershell
$env:TESTCONTAINERS_RYUK_DISABLED = "true"
```

Ryuk (the container cleanup sidecar) is disabled because Podman's rootless mode does not support the privileged operations Ryuk requires. Testcontainers disposes containers via `IAsyncLifetime.DisposeAsync` instead.

If Testcontainers cannot find the Podman socket automatically, set `DOCKER_HOST` explicitly:

```powershell
$env:DOCKER_HOST = "unix:///run/user/1000/podman/podman.sock"
```

### Verifying Podman is ready

```powershell
podman info
podman run --rm mcr.microsoft.com/mssql/server:2022-latest echo "SQL Server image OK"
```

## Running the application

This is the verified end-to-end runbook. Run every step from the `MindNova` directory unless stated otherwise.

### 1. Start the container runtime and database

```powershell
podman machine start
```

The repository ships a `docker-compose.yml`, but Podman on Windows needs a separate
`podman-compose` or `docker-compose` binary that is not installed by default. Starting the
container directly avoids that dependency and is the supported path:

```powershell
podman run -d --name mindnova-sql `
  -e ACCEPT_EULA=Y `
  -e MSSQL_SA_PASSWORD=MindNova_Dev123! `
  -p 1433:1433 `
  -v sqlserver-data:/var/opt/mssql `
  mcr.microsoft.com/mssql/server:2022-latest
```

On later runs the container already exists, so start it instead of recreating it:

```powershell
podman start mindnova-sql
podman ps --format "{{.Names}} {{.Status}} {{.Ports}}"
```

SQL Server needs roughly 20 seconds before it accepts connections.

### 2. Build

```powershell
dotnet build MindNova.slnx -c Debug
```

### 3. Apply EF Core migrations

```powershell
dotnet ef database update --project src/MindNova.Infrastructure --startup-project src/MindNova.Api
```

The API also migrates and seeds Identity roles on startup, so this step is mainly a way to
confirm database connectivity before launching anything.

### 4. Run the API

Use `dotnet exec` against the built DLL rather than `dotnet run`. `dotnet run` launches a
generated `MindNova.Api.exe` apphost, which endpoint security software commonly blocks (see
troubleshooting below). Debug builds therefore set `UseAppHost=false` in
`MindNova/Directory.Build.props` and emit no `.exe` at all, so `dotnet exec` is the only way to
start a Debug build:

```powershell
$apiContentRoot = (Resolve-Path src/MindNova.Api).Path
dotnet exec src/MindNova.Api/bin/Debug/net10.0/MindNova.Api.dll `
  --contentRoot $apiContentRoot `
  --environment Development `
  --urls http://localhost:5193
```

Port 5193 is not arbitrary: the Blazor front-end defaults to `http://127.0.0.1:5193` for its API
base address, so changing it means also setting `ApiBaseUrl` for the web app.

Verify:

```powershell
Invoke-WebRequest -Uri http://localhost:5193/health -UseBasicParsing | Select-Object StatusCode, Content
```

Expect `200` and `Healthy`.

### 5. Run the Blazor web front-end

In a second terminal:

```powershell
$webContentRoot = (Resolve-Path src/MindNova.Web).Path
dotnet exec src/MindNova.Web/bin/Debug/net10.0/MindNova.Web.dll `
  --contentRoot $webContentRoot `
  --environment Development `
  --urls http://localhost:5080
```

`MindNova.Web` has no `launchSettings.json`, so `--urls` is required to pin the port.
Open `http://localhost:5080` and the MindNova login page should render.

### 6. Create a test user

Only Identity *roles* are seeded on startup, never a user, so a fresh database has no account to
log in with. Register one through the API:

```powershell
$body = @{ Email = "dev@mindnova.local"; Password = "DevPassw0rd!" } | ConvertTo-Json
Invoke-RestMethod -Uri http://localhost:5193/api/auth/register -Method Post -Body $body -ContentType "application/json"
```

Confirm the credentials work:

```powershell
Invoke-RestMethod -Uri http://localhost:5193/api/auth/login -Method Post -Body $body -ContentType "application/json"
```

A `Token` in the response means the full stack (web, API, database, Identity) is working. You can
now log in through the UI at `http://localhost:5080`.

### Shutting down

Stop each app with `Ctrl+C`, then:

```powershell
podman stop mindnova-sql
```

The `sqlserver-data` volume persists, so data and applied migrations survive a restart.

## Running tests

```powershell
# Ensure Podman machine is running (rootful mode required)
podman machine start

# Set Testcontainers env vars
$env:TESTCONTAINERS_RYUK_DISABLED = "true"

# Run all tests
dotnet test MindNova/tests/MindNova.Api.Tests/MindNova.Api.Tests.csproj --configuration Release --verbosity normal
```

Integration tests (in `MindNova.Api.Tests`) use Testcontainers to spin up a SQL Server 2022 container per test class. The first run pulls the image, which takes 1-2 minutes. Tests use a SQL-connection-based readiness check (not exec) to work with Podman.

## Application troubleshooting

### `Access is denied` starting `MindNova.Api.exe` on `dotnet run`

Full error: `An error occurred trying to start process '...\bin\Debug\net10.0\MindNova.Api.exe' ... Access is denied.`

`dotnet run` builds a native apphost `.exe` and launches it. Endpoint protection and application
allowlisting products routinely block newly built, unsigned executables from a user profile
directory, and on a managed workstation you may not be able to add an exemption.

The build itself is not blocked, only the launch of the apphost, so the repository sets
`UseAppHost=false` for Debug builds in `MindNova/Directory.Build.props`. No `.exe` is produced,
and you start the app by running the managed DLL through the shared `dotnet` host:

```powershell
$apiContentRoot = (Resolve-Path src/MindNova.Api).Path
dotnet exec src/MindNova.Api/bin/Debug/net10.0/MindNova.Api.dll `
  --contentRoot $apiContentRoot `
  --environment Development `
  --urls http://localhost:5193
```

The setting is scoped to Debug, so Release builds and published artifacts are unaffected and CI
is unchanged. `dotnet run` still works for Release configuration.

`--contentRoot` matters because the process starts from the DLL output directory under `dotnet exec`.
Pass an absolute path so the app finds its `appsettings*.json`. `--environment` and `--urls` are
needed because `dotnet exec` ignores `launchSettings.json`.

### Web app loads but every API call fails

`MindNova.Web` resolves its API base address from the `ApiBaseUrl` configuration key and falls
back to `http://127.0.0.1:5193`. If the API is running on a different port, either start it on
5193 or set the key: `$env:ApiBaseUrl = "http://localhost:<port>"`.

### `MSB3021: Unable to copy file ... because it is being used by another process`

The API or web app is still running and holding its DLLs. Stop them with `Ctrl+C` before
rebuilding or running `dotnet test`. The error names the locking process, for example
`The file is locked by: ".NET Host (32108)"`.

### Login fails on a fresh database

Expected. Startup seeds Identity roles only, not users. Register an account via
`POST /api/auth/register` as shown in the runbook above.

Note that this API returns HTTP 200 for failed logins with a `ProblemDetails` body carrying
`Status: 401`, per constitution clause C07. Check the response body, not the status code.

## Podman troubleshooting

### `looking up compose provider failed` on `podman compose up`

Full error: `exec: "docker-compose": executable file not found in %PATH%` and the same for
`podman-compose`. Podman delegates `compose` to an external binary that is not bundled.

Either install one (`pip install podman-compose`) or skip compose entirely and use the
`podman run` command in the runbook above, which is the tested path.

### `getpwnam(root) failed` on `podman machine start`

The WSL distribution is corrupted. Fix:

```powershell
podman machine rm podman-machine-default --force
podman machine init
podman machine start
```

If `podman machine rm` does not clear it:

```powershell
wsl --unregister podman-machine-default
podman machine rm podman-machine-default --force
podman machine init
podman machine start
```

### `VM already exists` after `wsl --unregister`

Podman's internal state was not cleared. Use `podman machine rm --force` to remove the config files under `~/.config/containers/podman/machine/`, then `podman machine init` again.

### `WSL_E_DISTRO_NOT_FOUND` on start

The WSL distribution was unregistered but Podman still has config for it. Same fix: `podman machine rm --force`, then `init` and `start`.

### `API forwarding for Docker API clients is not available`

This warning is expected in rootless mode. Testcontainers connects via the WSL socket directly, not the Windows named pipe. If you need Docker-compatible API forwarding (e.g. for other tools):

```powershell
podman machine set --rootful
podman machine stop
podman machine start
```

### Testcontainers `HttpRequestException` / cannot connect

Causes:
1. Podman machine is not running - run `podman machine start`.
2. The `DOCKER_HOST` environment variable is not set or points to the wrong socket.
3. Ryuk is trying to start in rootless mode - set `$env:TESTCONTAINERS_RYUK_DISABLED = "true"`.

### Testcontainers `cannot hijack chunked or content length stream`

Seen with older Testcontainers releases. Podman does not support Docker's stream hijack protocol
that `ExecOperations.StartContainerExecAsync` relies on, so any wait strategy using exec
(including `UntilPortIsAvailable` and `MsSqlBuilder`'s built-in readiness check) fails.

Testcontainers 4.14.0 and later work against Podman, and the suite passes locally. The fixtures
still use a raw `ContainerBuilder` with a custom SQL connection wait strategy rather than an
exec-based one, which avoids the problem regardless of runtime. See `SqlServerFixture.cs` and
`TestcontainersHelper.cs` for the pattern.

If you hit this error, check that `Testcontainers.MsSql` has not been downgraded in
`MindNova/Directory.Packages.props`.

### SQL Server container fails to start (Podman)

The `mcr.microsoft.com/mssql/server:2022-latest` image requires at least 2 GB of RAM in the Podman machine. Check with:

```powershell
podman machine inspect | Select-String Memory
```

If below 2048 MB, recreate with more memory:

```powershell
podman machine rm podman-machine-default --force
podman machine init --memory 4096
podman machine start
```

## CI vs local

The CI pipeline (`.github/workflows/ci.yml`) runs on `ubuntu-latest` with Docker. Locally, Podman provides Docker-compatible container execution. Testcontainers detects the runtime automatically when `DOCKER_HOST` or the default socket is available.

## Related

- [MindNova README](../MindNova/README.md) - project structure and quick start
- [Constitution](constitution.md) - engineering principles
- [ADR 0008](adrs/0008-azure-sql-database-serverless.md) - database technology decision
