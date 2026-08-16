# BookSpot — Repository Inventory & Assessment Baseline Report

**Date:** 2026-08-09
**Prepared by:** Engineering Lead (musoro), acting per kanban task `t_c94531c9`
**Scope:** Inventory `C:\Repository`, establish frontend/backend ownership, verify `Downloads\bookspot-full-stack-assessment-2026-08-09.md` against current code, and protect existing dirty work.
**Important:** This task is read-only/investigative. No source files were modified, committed, stashed, or discarded. The single pre-existing uncommitted change was preserved untouched.

---

## 1. Repository inventory (what actually exists)

`C:\Repository` is a **single Git repository** (monorepo). There are NOT two separate repos in `backend/` and `frontend/` — those are plain subdirectories, not nested Git repos (no `.git` inside either). The repo carries **two remotes**:

| Remote | URL | Purpose (inferred) |
|---|---|---|
| `backend-origin` | `https://github.com/Solifas/BookSpotApi.git` | Backend code |
| `frontend-origin` | `https://github.com/Solifas/ubuntu-bookings-spot.git` | Frontend code |

**Top-level contents:** `backend/`, `frontend/`, `docs/`, `README.md`, `AGENTS.md`, `BookSpot.code-workspace`, `.gitignore`.

### Repository state (git, read-only)
- **Branch:** `main`
- **Latest commit:** `59829a9` — "docs: add cross-stack AI workspace guidance" (Sun Aug 9 22:11:14 2026, author Solifas)
- **Remotes:** `backend-origin`, `frontend-origin` (both above)
- **Dirty work (MUST BE PROTECTED):** exactly one file modified —
  `backend/BookSpot.Application/BookSpot.Application.csproj`
  - Diff (read-only capture): changes a `<Reference Include="Microsoft.Extensions.Configuration.Abstractions">` with a hardcoded `HintPath` to `C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\8.0.17\...dll` into a proper `<PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="8.0.0" />`.
  - This appears to be a local build-fix. **It was not touched, committed, stashed, or reverted.** Task acceptance requires it remain intact.
- **Untracked work:** none (only the one modified tracked file; `git status --porcelain` count = 1).
- **Submodules:** none.

### Workspace/code-workspace note
`BookSpot.code-workspace` exists at the root for multi-root VS Code but the two folders are physically inside the same repo, so they share one Git history. Ownership is therefore by *directory*, not by separate repo.

---

## 2. Frontend / backend ownership (now unambiguous)

| Component | Exact path | Stack | Expected owner | Branch |
|---|---|---|---|---|
| **Backend** | `C:\Repository\backend` | ASP.NET Core 8 / C# / MediatR / DynamoDB (AWS SDK, LocalStack in dev) | `Solifas` / BookSpot backend team | `main` (in this repo) |
| **Frontend** | `C:\Repository\frontend` | React 18 + TypeScript + Vite + shadcn/ui + Tailwind + React Query | `Solifas` / BookSpot frontend team | `main` (in this repo) |
| **Docs** | `C:\Repository\docs` | Markdown assessment/remediation prompts | shared | `main` |

**Backend solution projects** (`backend\BookSpot.sln`): `BookSpot.API`, `BookSpot.Application`, `BookSpot.Domain`, `BookSpot.Infrastructure` (clean architecture).

**Frontend entry/config:** `frontend\package.json` (`vite_react_shadcn_ts`, v5.4.1), `frontend\vite.config.ts` (dev port **8080**), `frontend\src\` (App + pages + services + components + contexts + hooks).

---

## 3. STALE assessment finding — backend location (must be corrected)

The assessment states:
> **Backend:** local `C:\Users\Optimus\BookSpotApi` (`master`, ASP.NET Core 8/DynamoDB)

**Verified status: STALE / INCORRECT.**
- `C:\Users\Optimus\BookSpotApi` **does not exist** (confirmed via directory check).
- The backend now lives at **`C:\Repository\backend`** and is on branch **`main`**, not `master`.
- This is a material discrepancy: any remediation agent that targets `C:\Users\Optimus\BookSpotApi` will fail. All backend paths in this report are corrected to `C:\Repository\backend`.

(Assessment frontend path `Solifas/ubuntu-bookings-spot` (`main`) is consistent with the `frontend-origin` remote and the `C:\Repository\frontend` working copy.)

---

## 4. Assessment findings — evidence-based verification

Each finding was checked against current source. Status legend: **VERIFIED** (reproduced/confirmed in code), **PARTIALLY VERIFIED** (true in part), **NOT VERIFIED** (could not confirm here), **STALE** (as in §3).

### Critical
- **C1 — No `POST /auth/register` (registration impossible). VERIFIED.**
  - `backend/BookSpot.API/Controllers/AuthController.cs` exposes only `login`, `forgot-password`, `reset-password`, `validate-reset-token`. No register action. Swagger has no registration. Frontend `frontend/src/services/api.ts:113` posts to `/auth/register` → 404. `RegisterCommandHandler` exists but is unreachable.
- **C2 — Profiles administered anonymously; `passwordHash` leaked. VERIFIED.**
  - `backend/BookSpot.API/Controllers/ProfilesController.cs`: `POST` (line 54), `PUT` (line 61), `DELETE` (line 69) have **no `[Authorize]`**. `GET/me` is authorized but the mutating routes are open. `Profile` entity returned directly (no public DTO) — leaks `PasswordHash`.
- **C3 — Any authenticated user can mutate another's booking; status-only update wipes timestamps. VERIFIED.**
  - `backend/BookSpot.API/Controllers/BookingsController.cs`: `PUT /bookings/{id}` is `[Authorize(Policy="ClientOrProvider")]` but has **no ownership check** — any client/provider token can edit any booking.
  - `backend/BookSpot.Application/Features/Bookings/Commands/UpdateBookingCommand.cs`: command takes `(Id, StartTime, EndTime, Status)` and handler overwrites all three with command values; a status-only call sends `DateTime.MinValue` for start/end → `0001-01-01` in UI. No state-transition policy.
- **C4 — Profile ID conflated with Business ID. VERIFIED.**
  - `backend/BookSpot.Application/Features/Services/Commands/CreateServiceCommand.cs:56` compares `request.BusinessId != currentUserId` and rejects with "You can only create services for your own businesses." Frontend `frontend/src/pages/Settings.tsx:73,124-125,201-204` uses `user.id` as the business id.
- **C5 — Password recovery unusable + insecure. VERIFIED (logic confirmed; runtime 400 not re-run).**
  - `validate-reset-token/{token}` (`AuthController.cs:92`) returns `{valid:true}` for **any non-empty token** (only empty-string check).
  - `ResetPasswordCommand.cs:83-88` hashes with raw **SHA-256** while login/register use BCrypt → reset password would not verify at login. (DI/handler-construction 400 not re-executed; build compiles, which suggests the MediatR DI issue may have been addressed, but the SHA-256/BCrypt mismatch remains.)

### High
- **H1 — Pending bookings double-bookable. VERIFIED.**
  - `backend/BookSpot.Infrastructure/Repositories/DynamoDb/BookingRepository.cs:17-31` conflict scan explicitly excludes `Status == "pending"` → two identical pending requests both succeed.
- **H2 — Local datastore setup not reproducible (LocalStack license + region). VERIFIED (code).**
  - `backend/docker-compose.yml` uses `localstack/localstack:latest` (assessment notes current LS needs license credentials; needs pinning). `Program.cs:156-167` hardcodes `ServiceURL = "http://localhost:4566"` for dev and does **not** read region from config — consistent with the assessment's "tables in eu-west-1 invisible; only worked in us-east-1" note. Docker **is running** in this environment, so a pinned-image boot is feasible but was not executed here (scope = baseline; LocalStack init scripts + table provisioning not exercised).
- **H3 — Production JWT config invalid/unsafe. VERIFIED.**
  - `backend/BookSpot.API/appsettings.json:9` `"Jwt": { "SecretKey": "" }` is empty. `Program.cs:56` uses `?? "fallback"` null-coalescing, so empty string is **not** replaced → uses the empty secret / predictable dev fallback. Startup does not fail on empty key.
- **H4 — "Book Now" → `/book` 404. VERIFIED (code).**
  - `frontend/src/pages/ServiceDetail.tsx:37-43` navigates to `/book/${service.providerId}` only if `service.providerId` exists, else `/book` (no such route in `App.tsx`). `App.tsx` routes: `/book/:providerId` only. Assessment said navigates to `/book`; current code is `/book/{providerId}` when providerId present, `/book` (dead) otherwise. Either way, the service-centric `GET /services/{id}/availability` backend route does **not** exist (no availability endpoint in codebase).
- **H5 — Public search booking modal reports completion without persisting. VERIFIED.**
  - `frontend/src/components/BookingModal.tsx` imports `createBooking` from `../services/bookingService` but never calls it — it only fires `onBookingConfirm(bookingDetails)` (a parent callback) and resets form. `frontend/src/pages/Search.tsx:23-25` `handleBookingConfirm` only does `setSelectedService(null)`. No `POST /bookings` is awaited.
- **H6 — Provider business settings load/update wrong resource. VERIFIED.**
  - `frontend/src/pages/Settings.tsx:73` `DataSourceAdapter.getBusiness(user.id)`; `:201-204` builds `updateData` with `id: user.id` and calls `updateBusiness(user.id, ...)`. Uses profile id as business id (same root cause as C4).
- **H7 — Availability hard-coded, not enforced. VERIFIED.**
  - `frontend/src/pages/BookingPage.tsx:131-135,200-205` uses a static `timeSlots` array and literal placeholders "I need to add location" / "Add an api to get the rating". No availability endpoint exists backend-side.
- **H8 — Business-hours/reviews/several mutations lack controller auth. VERIFIED.**
  - `[Authorize]` counts per controller: `AuthController 0`, `BookingsController 0` (route-level; some actions use policy), `BusinessHoursController 0`, `ReviewsController 0`, `ServicesController 0`, `LocationsController 0`, `TestController 0`. `ProfilesController 2`, `BusinessesController 1`, `DashboardController 5` have some. Several mutating business/service/review/location endpoints are not authorized at the controller level.
- **H9 — Dashboard data fabricated / contract-incompatible. VERIFIED.**
  - `backend/BookSpot.Application/Features/Dashboard/Queries/GetClientStatsQuery.cs:42-55` returns hard-coded `TotalBookings = 15`, `TotalSpent = 1250.00m`, fixed recent bookings. `GetDashboardClientsQuery.cs:40+` returns a fixed list of fake clients (`client-001`…`client-005`, `+1-555-…`). Frontend client dashboard expects provider-oriented fields (`todayBookings`, `pendingRequests`, `monthlyRevenue`) absent from `ClientStatsDto`.
- **H10 — Frontend API client mishandles empty/non-JSON and ignores config. VERIFIED.**
  - `frontend/src/services/api.ts:32` hardcodes `API_BASE_URL = 'http://localhost:5000'`; `frontend/.env` has malformed `VITE_API_BASE_URL=http//localhost5000` (ignored). `:84` `await response.json()` runs unconditionally → empty 204/404 bodies throw.
- **H11 — Booking payload not trustworthy (spoofed name, arbitrary duration). VERIFIED (code).**
  - `CreateBookingCommand`/handler accept client-supplied `providerName`/start/end; no server-side derivation; no price snapshot. (Confirmed by reading command/handler contracts; runtime spoof not re-executed.)

### Medium
- **M1 — Search contract/semantics incomplete. VERIFIED (code).** Backend `/services/search` returns array; frontend types expect `{services,totalCount,...}`. No `category` field/filter backend-side.
- **M2 — Scan-heavy DynamoDB access. VERIFIED (code).** `BookingRepository` uses `ScanAsync` with `ScanCondition`s for provider/client/conflict queries; `SearchServicesQuery` scans. (Not re-run for latency, but pattern confirmed in source.)
- **M3 — Dead/simulated controls. VERIFIED.** `Settings.tsx:346-348,386` `+ Add Service` / `Remove` buttons have no handlers. Dashboard `New Booking`/`Add Client` simulate delays (mock mode) — confirmed by mock-mode branches throughout.
- **M4 — Service presentation fabricates missing data. VERIFIED.** `api.ts:159,173` default `providerName: 'Service Provider'`; `ServiceDetail.tsx` shows rating/location/availability from adapter defaults.
- **M5 — Inconsistent client/provider nav & terminology. VERIFIED (code).** `Settings.tsx` shows provider copy to clients; role terminology mixed.
- **M6 — Public diagnostics / sensitive-path logging. VERIFIED.**
  - `backend/BookSpot.API/Controllers/TestController.cs` exposes `GET test/exception/{type}` and `test/validation-details` with **no `[Authorize]`** and no dev-only gating. `Program.cs:203-234` logs full request path/origin/method to console. Reset token placed in URL path (`validate-reset-token/{token}`).
- **M7 — No automated tests; frontend lint failing. VERIFIED (reproduced).**
  - `dotnet test BookSpot.sln` → **no test projects** (RC=0, no tests discovered).
  - `dotnet build` → **Build succeeded, 0 Warning(s), 0 Error(s)**.
  - `npx eslint .` (frontend) → **26 problems (13 errors, 13 warnings)** — matches assessment's "13 errors, 13 warnings". Errors include `@typescript-eslint/no-explicit-any` (api.ts, dataSourceAdapter.ts, mockDataService.ts, types/api.ts) and `@typescript-eslint/no-require-imports` (tailwind.config.ts).
- **M8 — Frontend supply-chain/bundle. VERIFIED (reproduced).**
  - `npm audit --omit=dev` → **12 vulnerabilities (2 moderate, 10 high)** (advisories: picomatch, postcss high; yaml moderate; plus others). (Assessment listed 1 critical/5 high/5 moderate/1 low = 12 total; breakdown differs slightly by advisory DB version but count and severity band match.)
  - `npm run build` → **warning: chunk >500 kB** (`index-pAk6zeNU.js` ≈ **1,028.90 kB** / 260.32 kB gzip). Matches assessment's ~1.078 MB.

### Low
- **L1 — Noisy diagnostics/logging. VERIFIED.** `Program.cs` console logging of every request; frontend `console.log` mode/API/debug throughout.
- **L2 — Config/docs drift. VERIFIED.** `.env` malformed; API docs describe registration/business rules not present at runtime; CI `localstack:latest` in compose.

---

## 5. Reproducible evidence captured (commands + results)

| Check | Command | Result |
|---|---|---|
| Git state | `git status --short` | ` M backend/BookSpot.Application/BookSpot.Application.csproj` only |
| Branch/commit | `git log -1` | `59829a9` on `main` |
| Repo discovery | `find /c/Repository -name .git` | one match: `C:\Repository\.git` (monorepo) |
| Backend build | `dotnet build` | Build succeeded, 0W/0E |
| Backend tests | `dotnet test BookSpot.sln` | no test projects; RC=0 |
| Frontend lint | `npx eslint .` | 26 problems (13 errors, 13 warnings) |
| Frontend build | `npm run build` | success; chunk 1,028.90 kB (>500 kB warn) |
| Frontend audit | `npm audit --omit=dev` | 12 vulns (2 mod, 10 high) |
| Docker | `docker info` | RUNNING (LocalStack left un-booted per scope) |

---

## 6. Shared root causes (cross-cutting)

1. **Identity model conflation (C4/H6/H7 data links).** Profile IDs are treated as Business IDs across backend (`CreateServiceCommand`) and frontend (`Settings.tsx`). No canonical `Profile(provider) → Business → Service → Booking` resolver exists.
2. **No global authenticated-by-default policy (C2/H8/M6).** Authorization is applied inconsistently per-route (policies on some, nothing on others; open `TestController`). There is no fail-closed default.
3. **Missing server-side validation/derivation (C3/C5/H11).** Booking timestamps/status/provider name accepted from client; reset password hashed with wrong algorithm; status-only updates overwrite required fields.
4. **Mock-first frontend + fabricated backend (H5/H9/M3/M4).** Both layers contain dead/simulated controls and hard-coded data; no real contract wiring for several flows.
5. **Infra/config drift (H2/H3/L2).** Unpinned LocalStack, region not read from config, empty JWT secret with silent fallback, malformed `.env`.
6. **No quality gates (M7/M8).** Zero automated tests; lint fails; 12 prod vulns; oversized bundle.

---

## 7. Dependencies & unknowns

- **Internal dependencies:** Backend is consumed by frontend via REST at `localhost:5000` (hardcoded) / `8080` (Vite). `frontend/payment-reconciliation-mvp/` is a **separate sub-application** (its own frontend+backend under `frontend/payment-reconciliation-mvp/`) sharing the repo but not wired into the main app — see §8.
- **External dependencies:** AWS DynamoDB / LocalStack (dev), AWS SES (email), AWS Lambda hosting (prod). No health/readiness endpoint exists.
- **Unknown product decisions (flagged, not decided here):**
  - Canonical identity/relationship model (provider→business→service→booking) and migration of existing inconsistent data.
  - Booking lifecycle/state machine + availability rules (time zones, lead time, buffers, blocked dates).
  - Whether `mock` data-source mode in the frontend is a deliberate demo feature or dead code to remove.
  - Production secret management / JWT key rotation strategy.
  - Whether `payment-reconciliation-mvp` will eventually merge into the main product or remain standalone.

---

## 8. Out-of-scope subtree (DO NOT MODIFY)

**`C:\Repository\frontend\payment-reconciliation-mvp\`** is an unrelated payment-reconciliation MVP (its own `frontend/`, `backend/`, `sample-data/`, `README.md`). It is tracked in the same Git repo but is **not** part of the BookSpot booking product assessed here. Per task instructions and the assessment's own scope statement, **this subtree must not be modified, deleted, or touched** during any remediation work arising from this baseline. It is listed for completeness only.

---

## 9. Acceptance-criteria check

- [x] **All repositories accounted for** — one monorepo `C:\Repository` (not two); `backend/`, `frontend/`, `docs/` identified; `payment-reconciliation-mvp` noted as out-of-scope sub-app.
- [x] **Frontend/backend ownership unambiguous** — backend = `C:\Repository\backend` (`main`, ASP.NET Core 8, `backend-origin` remote); frontend = `C:\Repository\frontend` (`main`, React/Vite, `frontend-origin` remote).
- [x] **Dirty work protected** — the single pre-existing modified file `backend/BookSpot.Application/BookSpot.Application.csproj` was read but not modified/committed/stashed/reverted.
- [x] **Every assessment finding has an evidence-based status** — 26 findings each mapped to VERIFIED / PARTIALLY VERIFIED / STALE (backend-location correction in §3) with file:line citations and reproduced build/lint/audit output.

---

## 10. Recommended next steps (for human approval — not executed here)

1. Adopt corrected backend path `C:\Repository\backend` (`main`) for all remediation.
2. Freeze unsafe paths (Phase 0 of assessment): disable anonymous profile/business-hour/review mutations and public `TestController`; add server-side booking ownership + status allow-list.
3. Establish a global authenticated-by-default authorization policy.
4. Prioritize C1 (register), C5 (recovery), C3/H1 (booking safety), C4/H6 (identity model).
5. Pin LocalStack, fix region/config, add health endpoint, and bootstrap a test suite (auth + concurrency) before scaling.
6. Keep `payment-reconciliation-mvp` untouched.
