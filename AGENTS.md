# AI Agent Guide — BookSpot Monorepo

## Scope

This repository contains one product split into two applications:

- `backend/`: BookSpot ASP.NET Core 8 API with DynamoDB/LocalStack.
- `frontend/`: Ubuntu Bookings Spot React/Vite/TypeScript application.

Treat API and frontend changes as one cross-project contract. Do not inspect or modify `frontend/payment-reconciliation-mvp`; it is unrelated to BookSpot.

## Required context

Before making changes, read:

1. `docs/bookspot-full-stack-assessment.md`
2. `docs/bookspot-remediation-agent-prompt.md`
3. Any more specific `AGENTS.md` inside the target subtree.

The assessment records confirmed runtime/security defects. Re-verify current behavior before changing code.

## Non-negotiable rules

- Preserve unrelated working-tree changes. At migration time, `backend/BookSpot.Application/BookSpot.Application.csproj` had a pre-existing unstaged edit replacing a machine-specific assembly reference with a NuGet package reference.
- Never conflate profile, provider, business, service, and booking identifiers.
- Do not return persistence entities or password hashes from API controllers.
- Enforce authentication plus resource ownership server-side; frontend route guards are not security controls.
- Never fabricate successful bookings, dashboard metrics, ratings, availability, or provider details.
- Use test-first vertical slices for behavior changes.
- Keep commits phase-focused; avoid broad rewrites across both trees.
- Do not claim completion without real test/build/API/browser output.

## Contract workflow

For a cross-stack feature:

1. Define desired behavior and ownership rules.
2. Add a failing backend API/contract test.
3. Implement backend behavior and update OpenAPI.
4. Update or generate frontend API types/client from that contract.
5. Add frontend component/E2E coverage.
6. Run backend and frontend quality gates.
7. Exercise the real flow through the browser and inspect network/console errors.

## Commands

Backend:

```bash
cd backend
dotnet restore BookSpot.sln
dotnet build BookSpot.sln --no-restore
dotnet test BookSpot.sln --no-build
dotnet list BookSpot.sln package --vulnerable --include-transitive
```

Frontend:

```bash
cd frontend
npm ci
npm run lint
npm run build
npm audit --omit=dev
```

## Git topology

This is the authoritative working monorepo. Original histories remain reachable through subtree merge parents and remotes `backend-origin` and `frontend-origin`. Do not reinitialize Git inside either subtree or add nested `.git` directories.
