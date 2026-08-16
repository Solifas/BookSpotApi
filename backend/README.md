# BookSpot API

ASP.NET Core 8 API backed by DynamoDB. The supported local datastore is LocalStack at `http://localhost:4566`; the API runs at `http://localhost:5000`.

## Local prerequisites

- Docker Desktop with Docker Compose v2 (`docker compose version`)
- .NET 8 SDK (`dotnet --version`)
- Git Bash, WSL, Linux, or macOS Bash
- `curl`

AWS CLI and LocalStack CLI are not required on the host. The scripts run `awslocal` inside the pinned LocalStack container.

## Canonical local configuration

| Setting | Local value |
|---|---|
| DynamoDB endpoint | `http://localhost:4566` |
| Region | `us-east-1` |
| Access key / secret | `test` / `test` (LocalStack only) |
| API URL | `http://localhost:5000` |
| LocalStack image | `localstack/localstack:3.8.1` |
| Persistent volume | `bookspot-localstack-data` |

`BookSpot.API/appsettings.Development.json` contains local-only values. `Program.cs` reads those settings and supports standard .NET overrides such as `AWS__Region` and `AWS__ServiceURL`. Non-development startup uses the AWS SDK role/credential chain; production credentials are not stored in this repository.

## Clean startup

Run these commands from `backend/`:

```bash
bash ./scripts/local-env.sh start
bash ./scripts/run-api.sh
```

The first command starts LocalStack, waits for health, creates missing tables, and inserts deterministic sample records only when their IDs do not already exist. The second command rechecks initialization and starts the API with:

```bash
dotnet run --no-launch-profile --project BookSpot.API/BookSpot.API.csproj
```

PowerShell equivalents:

```powershell
.\scripts\start-localstack.ps1
.\scripts\run-api.ps1
```

Swagger is available at `http://localhost:5000/swagger`. A seeded public service is available through `GET http://localhost:5000/services`.

## Tables and keys

The schema is derived from the DynamoDB entity annotations used by the API.

| Table | Partition key | Seeded |
|---|---|---|
| `profiles` | `Id` (string) | `local-provider-001` |
| `businesses` | `Id` (string) | `local-business-001` |
| `services` | `Id` (string) | `local-service-001` |
| `business_hours` | `Id` (string) | no |
| `bookings` | `Id` (string) | no |
| `reviews` | `Id` (string) | no |
| `password_reset_tokens` | `Token` (string) | no |

No global secondary indexes are currently required by repository queries. Password reset code currently scans by email; the historical `EmailIndex` variable was not used by the actual scan.

Provisioning is idempotent: existing tables are described instead of recreated, and seed records use conditional writes. Rerunning setup does not overwrite developer changes or clear tables.

## Health and verification

```bash
# LocalStack health plus table list
bash ./scripts/local-env.sh status

# Re-run static configuration/schema regression checks
python scripts/verify-local-setup.py

# Verify a DynamoDB item survives a LocalStack restart; test item is removed
bash ./scripts/local-env.sh smoke

# API health/surface (both commands print the response and fail on non-2xx status)
curl --fail --silent --show-error http://localhost:5000/swagger/index.html
curl --fail --silent --show-error http://localhost:5000/services
```

The persistence smoke test writes a uniquely named temporary item, restarts LocalStack, reads the item back, and deletes it. It does not modify seed or developer records.

## Stop and reset

Normal stop preserves the named volume and all developer data:

```bash
bash ./scripts/local-env.sh stop
```

Reset is intentionally explicit and destructive only to local BookSpot data:

```bash
bash ./scripts/local-env.sh reset --confirm
```

Reset runs `docker compose down --volumes`, recreates the environment, and reapplies the idempotent schema and seed. Never use reset against AWS or a non-local endpoint.

## Common failure recovery

- `port 4566 is already allocated`: stop the other LocalStack/container using the port, then rerun `bash ./scripts/local-env.sh start`. Inspect with `docker ps`.
- Docker daemon unavailable: start Docker Desktop and verify `docker info` succeeds.
- LocalStack unhealthy: inspect `docker compose logs localstack`; retry `bash ./scripts/local-env.sh start`. A normal stop/start preserves the volume.
- Tables appear missing: confirm endpoint `http://localhost:4566` and region `us-east-1`, then run `bash ./scripts/local-env.sh init` and `bash ./scripts/local-env.sh status`.
- API cannot reach DynamoDB: ensure `ASPNETCORE_ENVIRONMENT=Development`, LocalStack is healthy, and no shell override points `AWS__ServiceURL` or `AWS__Region` elsewhere.
- Initialization interrupted: rerun `bash ./scripts/local-env.sh init`; all operations are safe to repeat.
- Corrupt disposable local state: use `bash ./scripts/local-env.sh reset --confirm` only after accepting local data loss.

## Build

```bash
dotnet restore BookSpot.sln
dotnet build BookSpot.sln --no-restore
```

Deployment to AWS is separate from this local workflow and requires human-approved credentials and configuration.
