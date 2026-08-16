# BookSpot integrated parity and release-readiness report

**Task:** `t_dd5528dc`
**Tested code revision:** `3c2d6c6` on `integration/t_dd5528dc`
**Frontend integration revision:** `4c0ed38` (source implementation `dc1541d`)
**Decision:** **NOT RELEASE READY**

## Executive decision

The frontend, backend remediation, infrastructure, and architecture artifacts now exist in one committed monorepo candidate. This resolves the previous “no integrated candidate” blocker, and the deterministic component/unit gates pass: backend build succeeds, all 40 backend tests pass, all 32 frontend tests pass, TypeScript passes, and the frontend production build succeeds.

The application is not contract-aligned or release-ready. Live OpenAPI comparison proves that the frontend calls canonical self-scoped routes and PATCH methods which the integrated backend does not expose. LocalStack could not run because Docker Desktop's Linux engine is unavailable, so datastore-backed journeys, adversarial tests, and the required real concurrency test remain unexecuted. Frontend lint and dependency audits also fail release gates.

## Repository and candidate

- Monorepo: `C:\Repository`
- Integrated worktree: `C:\Users\Optimus\AppData\Local\hermes\kanban\workspaces\t_dd5528dc\integration`
- Branch: `integration/t_dd5528dc`
- Base: monorepo `main` at `f65e003`
- Integrated frontend commit: `4c0ed38`
- Integrated backend/infrastructure commit: `3c2d6c6`
- Protected user change excluded: `backend/BookSpot.Application/BookSpot.Application.csproj`
- Out of scope and unchanged: `frontend/payment-reconciliation-mvp/**`

## Deterministic gates

| Gate | Result | Evidence |
|---|---|---|
| Candidate integrity | PASS | Frontend, backend, infrastructure, tests, and design artifacts committed on one branch; clean worktree after code commit. |
| Backend restore/build | PASS with warnings | `dotnet restore`; `dotnet build --no-restore`: 0 errors, 7 warnings. Warnings include the protected unresolved configuration reference, four nullable warnings, and two pre-existing header analyzer warnings. |
| Backend tests | PASS, unit/contract scope | 40/40 xUnit tests passed. No live LocalStack contention test. |
| Backend dependency audit | FAIL | Test project has two High transitive advisories: `System.Net.Http 4.3.0` and `System.Text.RegularExpressions 4.3.0`. Application projects reported none. |
| Local schema verifier | PASS, static | 10 exact DynamoDB table/key declarations validated; canonical region `us-east-1`; endpoint `http://localhost:4566`; Bash syntax passed. |
| Frontend install | PASS with audit failure | `npm ci` completed. Full install reported 1 Critical, 14 High, 4 Moderate, 2 Low across production/dev dependencies. |
| Frontend tests | PASS | 9 files, 32/32 Vitest tests. |
| Frontend TypeScript | PASS | `npx tsc -p tsconfig.app.json --noEmit`. |
| Frontend build | PASS with warnings | 830.98 kB JS chunk; stale Browserslist database. |
| Frontend lint | FAIL | 8 errors, 10 warnings. |
| Frontend production audit | FAIL | `npm audit --omit=dev`: 10 High, 2 Moderate, including direct `react-router-dom` and `postcss` findings. |
| Docker/LocalStack | BLOCKED | Docker Linux engine pipe absent; no LocalStack or DynamoDB runtime. |
| API startup | PASS, no datastore | Integrated API listened on `127.0.0.1:5000`. Swagger HTML and JSON returned 200. |
| Frontend startup | PASS on alternate port | Exact integrated frontend returned 200 at `127.0.0.1:8081`. Port 8080 was occupied by a stale Vite process from frontend task `t_6cf4f26b`; that process also returned 200 but is not the exact integrated worktree. |
| Anonymous profile read/update | PASS for bounded smoke | `GET /profiles/me`, `PATCH /profiles/me`, and `GET /profiles/other-user` returned 401 before datastore access. |
| Invalid registration | PASS for validation smoke | Empty `POST /auth/register` returned structured HTTP 400 validation errors. |
| Public service runtime | BLOCKED | `GET /services` timed out because LocalStack was unavailable. |
| Integrated E2E/security/concurrency | NOT RUN / BLOCKER | Datastore unavailable and API contract mismatch prevents complete journeys. |

## Proven frontend/backend contract mismatches

The frontend endpoint inventory was compared against the running candidate's Swagger JSON.

| Frontend contract | Backend OpenAPI | Result |
|---|---|---|
| `GET /businesses/mine` | absent | BLOCKER: provider Settings cannot resolve canonical business IDs. |
| `GET /bookings/client/me` | only `GET /bookings/client/{clientId}` | BLOCKER: self-scoped client list absent. |
| `GET /bookings/provider/me` | only `GET /bookings/provider/{providerId}` | BLOCKER: self-scoped provider list absent. |
| `GET /dashboard/me` | only legacy ID/role dashboard routes | BLOCKER: frontend dashboard route absent. |
| `GET /services/{serviceId}/availability` | absent | BLOCKER: live availability journey cannot work. |
| `PATCH /businesses/{businessId}` | backend exposes `PUT` | method mismatch. |
| `PATCH /services/{serviceId}` | backend exposes `PUT` | method mismatch. |
| `PATCH /reviews/{reviewId}` | backend exposes `PUT` | method mismatch. |
| `POST /bookings/{bookingId}/actions` | present | aligned at route/method level; live datastore behavior not verified. |
| Auth registration/recovery routes | present | aligned at route/method level; complete recovery delivery/security topology remains open. |
| `GET/PATCH/DELETE /profiles/me` | present | aligned at route/method level. |

These are not cosmetic differences. They break API-backed Settings, dashboards, live availability, booking lists, and update operations in the integrated application.

## Reconciliation of all 26 assessment findings

`CLOSED` requires independent behavior evidence against the integrated candidate. `PARTIAL` means remediation exists but the original flow/exploit was not fully reproduced. `OPEN` means implementation or a required gate remains missing.

| ID | Severity | Status | Integrated evidence and residual risk |
|---|---|---|---|
| C1 | Critical | PARTIAL — blocks release | Registration route, validation, frontend integration, and tests exist. No successful datastore-backed client/provider registration E2E ran. HD-01 behavior remains a human production gate. |
| C2 | Critical | PARTIAL — blocks release | Self-profile routes and safe DTO tests exist; anonymous self probes returned 401. Full anonymous CRUD, horizontal access, role escalation, and sensitive-field matrix did not run. |
| C3 | Critical | PARTIAL — blocks release | Generic booking mutation was replaced by an explicit action route and tests, but self-scoped booking list routes are absent and no live transition/reschedule test ran. |
| C4 | Critical | OPEN | Frontend uses `/businesses/mine`; backend does not expose it. Canonical provider-profile/business resolution is therefore not end-to-end. |
| C5 | Critical | OPEN | Recovery routes and single-use repository logic exist, but encrypted delivery/outbox/audit/abuse controls and complete reset-login E2E are absent. |
| H1 | High | PARTIAL — blocks release | Transactional slot code and tests exist; no real two-request LocalStack/AWS proof ran. |
| H2 | High | PARTIAL — blocks release certification | Static 10-table schema verification passes, but Docker/LocalStack provisioning and restart persistence were not rerun against this candidate. |
| H3 | High | PARTIAL | Startup rejects a missing/short JWT secret and checks security version. Production-like secret/rotation behavior was not executed; HD-02 remains a human policy gate. |
| H4 | High | OPEN | Frontend now requests service availability, but the backend availability route is absent. |
| H5 | High | PARTIAL | Frontend awaits booking persistence and handles conflicts; no datastore-backed browser/network round trip ran. |
| H6 | High | OPEN | Settings uses canonical business IDs and PATCH, but `/businesses/mine` is absent and backend exposes PUT rather than PATCH. |
| H7 | High | OPEN | Backend has no service-availability projection or complete schedule/timezone eligibility enforcement. |
| H8 | High | PARTIAL — blocks release | Ownership checks were added to multiple mutations, but the full adversarial matrix did not run and canonical mutation method parity is incomplete. |
| H9 | High | OPEN | Frontend calls `/dashboard/me`; backend does not expose it. |
| H10 | High | PARTIAL | Frontend tests cover JSON, malformed, text, empty, and 204 handling; integrated browser/network negative cases were not run. |
| H11 | High | PARTIAL — blocks release | Minimal booking intent and server-derived field tests pass; live persistence/history behavior was not verified. |
| M1 | Medium | OPEN | This review found route/method drift through live OpenAPI comparison; no automated generated-client parity gate exists. |
| M2 | Medium | OPEN | Scan-heavy access patterns and production-scale validation remain unresolved. |
| M3 | Medium | PARTIAL | Dashboard/settings frontend mocks were replaced in primary flows, but backend canonical routes are absent and mock services remain in the tree. |
| M4 | Medium | PARTIAL | Adapter tests pass; real datastore DTO presentation was not exercised. |
| M5 | Medium | PARTIAL | Role-specific component tests pass; integrated client/provider browser navigation did not run. |
| M6 | Medium | PARTIAL | Production diagnostic suppression and reset body validation exist; complete production-like verification did not run. |
| M7 | Medium | OPEN | Automated unit/component coverage improved, but lint fails and no E2E/concurrency suite ran. |
| M8 | Medium | OPEN | Production dependency audit contains 10 High/2 Moderate vulnerabilities; bundle is 830.98 kB. |
| L1 | Low | OPEN | Logging/noise/redaction was not fully remediated or independently verified. |
| L2 | Low | PARTIAL | Setup and design documentation are integrated; clean runtime reproduction is blocked by Docker availability. |

### Severity disposition

- Critical: 0 CLOSED, 3 PARTIAL, 2 OPEN.
- High: 0 CLOSED, 7 PARTIAL, 4 OPEN.
- Medium: 0 CLOSED, 4 PARTIAL, 4 OPEN.
- Low: 0 CLOSED, 1 PARTIAL, 1 OPEN.

## Human decisions required

Only two product/security decisions remain explicitly human-owned before production approval:

- HD-01: registration enumeration/verification behavior.
- HD-02: final production password policy and related risk acceptance.

These decisions do not justify the current route drift; canonical API parity can proceed independently.

## Remediation pipeline created

- `t_2639c3ed` — Mugayi: canonical backend API surface and live projections.
- `t_555207bb` — Nova: frontend lint and production dependency advisories.
- `t_64fd51ae` — Titan: restore LocalStack integrated runtime gate.
- `t_c136c54b` — Sentinel: independent implementation review.
- `t_3f88d61f` — Cerberus: adversarial security validation.
- `t_030e9a73` — Tesla: final integrated E2E/parity/release gate.

## Reproduction commands

```bash
cd C:/Users/Optimus/AppData/Local/hermes/kanban/workspaces/t_dd5528dc/integration/backend
dotnet restore BookSpot.sln
dotnet build BookSpot.sln --no-restore
dotnet test BookSpot.Tests/BookSpot.Tests.csproj --no-build
dotnet list BookSpot.sln package --vulnerable --include-transitive
python scripts/verify-local-setup.py
bash -n localstack-init/01-create-dynamodb-tables.sh scripts/local-env.sh scripts/run-api.sh

cd ../frontend
npm ci
npm test -- --run
npx tsc -p tsconfig.app.json --noEmit
npm run build
npm run lint
npm audit --omit=dev --json
```

Runtime (requires Docker/LocalStack for complete evidence):

```bash
cd ../backend
bash ./scripts/local-env.sh start
bash ./scripts/local-env.sh init
bash ./scripts/run-api.sh

cd ../frontend
npm run dev -- --host 127.0.0.1 --port 8080 --strictPort
```

## Recommendation

**NOT RELEASE READY.** The cross-stack candidate is now reproducible at the source level, but the live API proves foundational frontend/backend route and method drift. Critical/High findings remain open, LocalStack E2E/concurrency/security gates did not run, lint fails, and High dependency advisories remain. Do not promote this revision to UAT or production approval.
