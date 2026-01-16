# Repository Guidelines

## Project Structure & Module Organization
- `BookSpot.sln` is the root solution file.
- `BookSpot.API/` hosts the ASP.NET Core Lambda entry point (Controllers, Models, Services, `Program.cs`).
- `BookSpot.Application/` contains use cases and orchestration, `BookSpot.Domain/` holds entities/value objects, and `BookSpot.Infrastructure/` provides DynamoDB/AWS integrations.
- `scripts/` includes local dev and deployment helpers; `localstack-init/` provisions DynamoDB tables; `terraform/` defines infrastructure; `docs/` holds design notes.
- API request collections live in `BookSpot.http` and `BookSpot.API/Tests/auth-endpoints.http`.

## Build, Test, and Development Commands
- Start LocalStack: `.\scripts\start-localstack.ps1` (Docker required; uses `docker-compose.yml`).
- Run the API: `.\scripts\run-api.ps1` or `cd BookSpot.API; dotnet run` (Swagger at `http://localhost:5000/swagger`).
- Create tables/seed data: `.\scripts\create-tables.ps1`, `.\scripts\seed-data.ps1`.
- Build Lambda package: `.\scripts\build-lambda.ps1` (outputs `bookspot-api.zip`).
- Deploy: `.\scripts\deploy.ps1` or `dotnet lambda deploy-function` from `BookSpot.API/`.
- Tests: `dotnet test` (no automated test projects yet); use `.http` files for manual endpoint checks.

## Coding Style & Naming Conventions
- `.editorconfig` enforces 4-space indents for C#, 2-space for JSON/JS/YAML, CRLF endings, and trimmed trailing whitespace.
- Follow standard C# conventions: PascalCase for types/methods, camelCase for locals/params, `I`-prefixed interfaces, and `Async` suffix for async methods.
- Add XML docs for public APIs when behavior is not obvious.

## Testing Guidelines
- Manual API verification is expected with LocalStack plus `.http` files or Swagger UI.
- When adding automated tests, keep names descriptive (e.g., `BookingServiceTests`, `CreateBooking_InvalidSlot_ReturnsError`) and run via `dotnet test`.

## Commit & Pull Request Guidelines
- Git history favors Conventional Commits: `feat(scope): ...`, `fix: ...`, `docs: ...`, `ci: ...`, `refactor: ...` with imperative subjects.
- PRs should include a clear description, linked issues (e.g., `Fixes #123`), test notes (LocalStack/HTTP checks), and any doc updates.

## Configuration & Security Notes
- LocalStack defaults: endpoint `http://localhost:4566`, region `eu-west-1`, credentials `test`/`test`.
- For AWS deployments, use environment variables or your credential provider; avoid committing secrets.
