# Local development setup

Prerequisites and configuration for running and testing MindNova on a developer workstation.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Podman](https://podman.io/) (container runtime for local SQL Server and integration tests)
- [GitHub CLI](https://cli.github.com/) (`gh`) for PR creation
- WSL 2 (required by Podman on Windows)

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

```powershell
cd MindNova
podman compose up -d          # Start SQL Server container
dotnet ef database update --project src/MindNova.Infrastructure --startup-project src/MindNova.Api
dotnet run --project src/MindNova.Api
```

Health check: `GET http://localhost:5000/health`

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

## Podman troubleshooting

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

This is a Podman + Docker.DotNet incompatibility with the exec API. Podman does not support Docker's stream hijack protocol used by `ExecOperations.StartContainerExecAsync`.

The fix is to avoid any Testcontainers wait strategy that uses exec (including `UntilPortIsAvailable` and `MsSqlBuilder`'s built-in readiness check). Use a raw `ContainerBuilder` with a custom SQL connection wait strategy instead. See `SqlServerFixture.cs` and `TestcontainersHelper.cs` for the working pattern.

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
