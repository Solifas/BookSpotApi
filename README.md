# BookSpot Monorepo

A single Git workspace containing the BookSpot booking platform.

## Layout

- `backend/` — ASP.NET Core 8 API, application/domain layers, DynamoDB persistence, LocalStack development infrastructure.
- `frontend/` — Vite + React + TypeScript user and provider application.
- `docs/` — full-stack QA assessment and staged remediation brief.
- `AGENTS.md` — cross-project instructions for AI coding agents.

## Local development

### Backend infrastructure and API

```bash
cd backend
docker compose up -d
dotnet restore BookSpot.sln
dotnet run --project BookSpot.API
```

Expected API URL: `http://localhost:5000`.

> The QA report documents current LocalStack setup defects. Consult `docs/bookspot-full-stack-assessment.md` before relying on the existing Compose setup.

### Frontend

```bash
cd frontend
npm ci
npm run dev
```

Expected frontend URL: `http://localhost:8080`.

## Verification

```bash
cd backend && dotnet build BookSpot.sln && dotnet test BookSpot.sln
cd ../frontend && npm run lint && npm run build
```

## Original repository remotes

The monorepo preserves both complete histories through Git subtree merges:

- `backend-origin` → `https://github.com/Solifas/BookSpotApi.git`
- `frontend-origin` → `https://github.com/Solifas/ubuntu-bookings-spot.git`

Example subtree synchronization:

```bash
git subtree pull --prefix=backend backend-origin master
git subtree pull --prefix=frontend frontend-origin main
```

This parent repository does not yet have its own `origin`. Add one when you are ready to publish the monorepo.
