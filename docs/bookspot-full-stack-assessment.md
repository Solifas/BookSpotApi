# BookSpot / Ubuntu Bookings Spot — Full-Stack Assessment

**Date:** 2026-08-09  
**Frontend:** `Solifas/ubuntu-bookings-spot` (`main`, Vite/React/TypeScript)  
**Backend:** local `C:\Repository\backend` (`master` history imported into the monorepo, ASP.NET Core 8/DynamoDB)  
**Runtime tested:** frontend `http://localhost:8080`; API `http://localhost:5000`; LocalStack/DynamoDB `http://localhost:4566`  
**Scope:** setup, code/contract analysis, API probes, auth/authorization, booking integrity, provider/client browser flows, builds, linting, and dependency audits. The unrelated payment-reconciliation subtree was excluded.

---

## Executive summary

| Severity | Count |
|---|---:|
| Critical | 5 |
| High | 11 |
| Medium | 8 |
| Low | 2 |
| **Total** | **26** |

**Assessment:** The frontend can display API-backed services and login against BookSpot, but the product is not release-ready. Registration and recovery are broken; provider/business identifiers are inconsistent; booking authorization and state integrity are unsafe; pending slots can be double-booked; multiple screens are mock or dead UI; and the repository’s documented local datastore setup no longer works without manual intervention.

### Highest-priority release blockers

1. Add and test `POST /auth/register`; fix the complete password-recovery path.
2. Apply authentication, ownership authorization, and DTO boundaries to every mutation and private read.
3. Define one canonical relationship: `Profile(provider) -> Business -> Service -> Booking`; stop treating profile IDs as business IDs.
4. Implement an atomic availability/booking transaction and a booking status state machine.
5. Replace dashboard/settings mock/dead behavior with real API contracts.
6. Pin and repair local infrastructure, then add automated integration and E2E tests.

---

## What is implemented

| Area | Current backend surface | Current frontend surface | Readiness |
|---|---|---|---|
| Authentication | Login, forgot/reset password, reset-token validation | Login, register, reset-password screens | **Broken:** no registration route; recovery path defective |
| Profiles | Create/get/update/delete, `/profiles/me` | Auth context and profile/settings | **Unsafe:** unauthenticated administration and domain entity exposure |
| Businesses | CRUD, business services | Provider settings | **Broken contract:** frontend uses profile ID as business ID |
| Services | CRUD, list, search | Homepage/search/detail/provider services | Partly works for listing; creation/management and enrichment broken |
| Bookings | CRUD, by client/provider | Three-step booking, dashboard/calendar | **Unsafe:** IDOR/state corruption; no reliable availability |
| Business hours | CRUD by record ID | Availability settings UI | UI is not persisted; no usable business schedule query |
| Reviews | CRUD by review ID | Ratings shown in cards/detail | No service review listing/aggregation; ratings are defaults |
| Dashboard | Provider stats/insights/clients; client stats | Provider/client dashboard | Mixed real, mock, and incompatible DTOs |
| Locations | City aggregation | Search/location UI | Limited; city mapping is hard-coded and scan-based |
| Operations | Swagger, Lambda support, exception middleware | Vite build | No health endpoint; test/debug routes exposed; local setup drift |

---

## Detailed findings

## Critical

### C1. User registration is impossible

- **Observed:** frontend submits `POST /auth/register`; API returns `404` with an empty body. The live registration flow could not complete.
- **Evidence:** frontend `src/services/api.ts:114`; backend Swagger contains no registration action; `BookSpot.API/Controllers/AuthController.cs` exposes login/recovery only. A `RegisterCommandHandler` exists but is unreachable.
- **Secondary failure:** the frontend always attempts `response.json()`, so the empty 404 is converted into a generic parsing/network failure rather than an actionable registration error.
- **Impact:** no normal user can become a client or provider.
- **Fix:** expose an `[AllowAnonymous] POST /auth/register`, validate role/email/password, return the documented `AuthResponse`, and add contract/E2E tests.

### C2. Profiles can be administered anonymously and leak persistence fields

- **Observed:** without a token:
  - `POST /profiles` returned `201`;
  - `PUT /profiles/{providerId}` returned `200` and changed a provider into a client;
  - responses included `passwordHash`.
- **Evidence:** `BookSpot.API/Controllers/ProfilesController.cs`; handlers do not enforce ownership. `Profile` is returned directly instead of a public DTO.
- **Impact:** account takeover/destruction, role escalation/demotion, email alteration, and password-hash disclosure.
- **Fix:** remove public profile CRUD or enforce admin/self policies; prohibit role changes through the profile endpoint; return explicit response DTOs that never include `PasswordHash`.

### C3. Any authenticated client/provider can mutate another user’s booking, and partial updates erase data

- **Observed:** a client token updated a provider/client booking it did not own. A provider token changed a client booking to `arbitrary-status`. A status-only update reset `startTime` and `endTime` to `.NET DateTime.MinValue` (`0001-01-01`), which appeared in the UI as malformed calendar/request data.
- **Evidence:** `BookingsController.cs:45-83`; `UpdateBookingCommand.cs:6-11`; update handler saves command defaults and has no ownership/state-transition policy.
- **Impact:** cross-account tampering, schedule corruption, nonsensical statuses, and loss of booking timestamps.
- **Fix:** separate commands (`Accept`, `Decline`, `Cancel`, `Reschedule`), authorize actor and ownership, use nullable patch fields or PUT-complete semantics, validate transitions, and reject unknown statuses.

### C4. Provider profile IDs and business IDs are conflated, breaking service management

- **Observed:** provider created a real business with ID `17e02a3f-...`; creating a service for that business returned `400 "You can only create services for your own business"`. Supplying the provider profile ID as `businessId` returned `201`, creating an orphan service that references no real business.
- **Evidence:** service creation compares `command.BusinessId` to authenticated user ID in `BookSpot.Application/Features/Services/Commands/CreateServiceCommand.cs`. Frontend settings fetch/update business with `user.id` (`src/pages/Settings.tsx:73,124-125,201-204`).
- **Impact:** the normal provider onboarding -> business -> service flow cannot work; orphaned records contaminate search and booking.
- **Fix:** resolve the business by `Business.ProviderId == currentUserId`, then authorize against the resolved business ID. Add `GET /businesses/me` or `GET /businesses/provider/{providerId}` and use the returned business ID everywhere.

### C5. Password recovery is both unusable and insecure

- **Observed:** `POST /auth/forgot-password` returned `400` with a MediatR handler-construction error. `GET /auth/validate-reset-token/anything` returned `200 {isValid:true}` for an arbitrary token.
- **Evidence:** `AuthController.cs:92-110` performs only non-empty string validation. `ResetPasswordCommand.cs:83-88` hashes new passwords with raw SHA-256, while login/register use BCrypt.
- **Impact:** recovery cannot complete; if reset did run, the new SHA-256 hash would not verify under BCrypt login; fake tokens are reported as valid.
- **Fix:** repair DI, query token existence/expiry/used state, use the same `IPasswordHasher`/BCrypt implementation throughout, avoid logging reset tokens in paths, and test the whole email-reset-login sequence.

## High

### H1. Pending bookings can be double-booked

- **Observed:** two identical requests for the same provider and `2026-08-12 10:00–17:00Z` both returned `201` and remained `pending`.
- **Evidence:** `BookingRepository.cs:17-31` explicitly excludes `pending` bookings from conflict checks.
- **Impact:** the marketplace can promise the same slot to multiple clients.
- **Fix:** pending bookings must reserve capacity (possibly with expiry), and the check/write must be atomic using DynamoDB conditional writes or transactions. A scan followed by save is race-prone even after fixing the status filter.

### H2. The repository’s local datastore setup is not reproducible

- **Observed:** `docker compose up -d` pulled `localstack/localstack:latest` (2026.7.2), which exited because current LocalStack requires license credentials. Pinning `3.8.1` started it, but both ready scripts failed to execute. API data requests then returned `500` because tables did not exist.
- **Additional mismatch:** tables provisioned in configured `eu-west-1` were invisible to the API. The API only worked after duplicating them in `us-east-1` because `Program.cs` ignores configured `AWS:Region` when constructing `AmazonDynamoDBConfig`.
- **Evidence:** `docker-compose.yml`, `localstack-init/01-create-dynamodb-tables*.sh`, `BookSpot.API/Program.cs`, `appsettings.Development.json`.
- **Fix:** pin a supported image digest/version; normalize executable bits/LF endings; create all seven tables including `password_reset_tokens`; read service URL and region from typed configuration; add a startup/CI smoke test.

### H3. Production JWT configuration is invalid and unsafe by default

- **Evidence:** `BookSpot.API/appsettings.json:9-13` has an empty `Jwt:SecretKey`; startup configuration uses null coalescing, so an empty string is not replaced. Development contains a predictable fallback secret.
- **Impact:** production token generation/validation can fail or be deployed with an unsafe key.
- **Fix:** fail startup unless a sufficiently long secret comes from a secure external provider; rotate current development-like keys before any deployment.

### H4. “Book Now” from service detail navigates to a non-existent route

- **Observed:** clicking Book Now navigated to `/book` and the app showed its 404 page.
- **Evidence:** `ServiceDetail.tsx:37-43`; the API-to-UI service adapter lacks a valid `providerId` because the backend service entity has `BusinessId`/`ProviderId` relationships inconsistent with the frontend model.
- **Impact:** the primary conversion flow is blocked from service details.
- **Fix:** make service-detail use a canonical service booking route such as `/book/service/{serviceId}` and let the backend resolve business/provider.

### H5. The public search booking modal reports completion without persisting a booking

- **Evidence:** `src/components/BookingModal.tsx` imports booking creation but never invokes it; it calls a parent callback and resets. `src/pages/Search.tsx` only clears `selectedService`.
- **Impact:** users can believe they booked while no booking exists.
- **Fix:** use one booking implementation for search and detail flows, await `POST /bookings`, show the returned booking ID/status, and retain form state on errors.

### H6. Provider business settings load and update the wrong resource

- **Observed:** the existing `QA Salon` business did not populate provider Business Settings. The page showed blank fields. Save constructs `id: user.id` and calls `PUT /businesses/{user.id}`.
- **Evidence:** `src/pages/Settings.tsx:70-105,193-205`.
- **Impact:** providers cannot reliably view/update their business; save may 404 or target a coincidentally matching record.
- **Fix:** fetch provider business first, store its `business.id`, and use that ID for updates and child resources.

### H7. Availability is hard-coded and not enforced

- **Observed:** booking page offered a static set of dates and time slots regardless of business hours, existing bookings, service duration, time zone, or lead time. The page contains literal placeholders “I need to add location” and “Add an api to get the rating”.
- **Evidence:** `BookingPage.tsx:196-205,321-361`; no availability/slots endpoint exists. Business-hours API only supports CRUD by record ID.
- **Impact:** users select unavailable/closed slots and receive late conflicts or double bookings.
- **Fix:** add `GET /services/{id}/availability?from=&to=&timezone=` and enforce the same rules atomically during creation.

### H8. Business-hours, reviews, and several resource mutations lack controller authorization/ownership

- **Evidence:** `BusinessHoursController.cs` and `ReviewsController.cs` have no `[Authorize]`; profile CRUD is also open; several business/service commands rely inconsistently on handler claims instead of route-level policies.
- **Impact:** unauthorized creation/update/deletion and inconsistent security behavior.
- **Fix:** default to a global authenticated fallback policy, mark only public reads and auth endpoints `[AllowAnonymous]`, and enforce resource ownership in handlers/services.

### H9. Dashboard data is fabricated or contract-incompatible

- **Evidence:** `GetClientStatsQuery.cs:40-100` returns hard-coded totals/recent bookings; `GetDashboardClientsQuery.cs:38-89` returns five fake clients. Frontend client dashboard expects provider-oriented fields (`todayBookings`, `pendingRequests`, `monthlyRevenue`) that are absent from `ClientStatsDto`.
- **Observed:** client dashboard showed provider copy (“Manage your bookings and grow your business”), blank statistics, and request-oriented navigation.
- **Impact:** business metrics, client history, and UI decisions are misleading.
- **Fix:** remove mocks from production handlers, define separate provider/client DTOs, generate clients from real provider bookings, and add contract tests.

### H10. The frontend API client mishandles empty/non-JSON responses and ignores configuration

- **Evidence:** `src/services/api.ts` uses a hard-coded `http://localhost:5000`; tracked `.env` has malformed `VITE_API_BASE_URL=http//localhost5000` but is effectively ignored. Every response is parsed as JSON, including empty 204/404 responses.
- **Observed:** registration’s empty 404 became a parsing/network failure; successful DELETE calls returned a frontend status-0 style error because 204 had no JSON body.
- **Fix:** read and validate `import.meta.env.VITE_API_BASE_URL`; parse by status/content type; treat 204 as success; preserve HTTP status/problem-details; add global timeout/401 handling.

### H11. Booking payload and historical records are not trustworthy

- **Observed:** client supplied `providerName: "Spoofed Name"`, and the backend stored/returned it. It also accepted a seven-hour `endTime` for a 45-minute service.
- **Evidence:** booking command accepts provider display name and arbitrary start/end. `Booking` stores no booked price snapshot; dashboard revenue looks up current service price.
- **Impact:** identity spoofing, invalid durations, and historical revenue changing when service prices change or services are deleted.
- **Fix:** derive provider/business/name and end time from service/business records; snapshot service name/price/duration at booking time.

## Medium

### M1. Search contract and semantics are incomplete

- Backend `/services/search` returns a bare array; frontend types expect `{services,totalCount,page,pageSize,totalPages}` and applies adapter fallbacks.
- `category` exists in frontend search but backend has no category field/filter.
- Pagination supplies no total count, next-page indicator, or stable ordering.
- **Fix:** publish one OpenAPI contract and generate clients/types from it.

### M2. DynamoDB access patterns are scan-heavy and will not scale

- **Evidence:** service list/search uses full table scans and filters in memory; city filtering performs per-service business lookups (`SearchServicesQuery.cs:29-84`); booking/provider/client queries scan; profile email lookup scans.
- **Impact:** growing latency/cost, pagination inconsistency, throttling, and expensive dashboards.
- **Fix:** design GSIs for email, provider/business/service, client bookings, provider bookings, and availability; use key queries and continuation tokens.

### M3. Multiple visible buttons/forms are dead or simulated

- Provider Settings `+ Add Service` and `Remove` have no handlers (`Settings.tsx:346-388`).
- Dashboard `New Booking`, Add Client, and some service forms simulate delays/local success instead of API persistence.
- **Observed:** clicking Add Service and New Booking produced no usable flow.
- **Fix:** remove incomplete controls or wire them to real mutations with loading/error/success states.

### M4. Service presentation fabricates missing backend data

- The adapter supplies defaults such as “Service Provider”, ratings/review counts, location/availability/contact information.
- **Observed:** detail showed `0 (0 reviews)`, “Location not specified”, “Contact provider”, and placeholder contact details for an orphan service.
- **Impact:** users cannot distinguish verified data from fallback text.
- **Fix:** enrich service DTOs from business/profile/review aggregation and render explicit unavailable states, not invented values.

### M5. Client/provider navigation and terminology are inconsistent

- Client sees provider-oriented dashboard language and request concepts.
- Provider dashboard request badge counted records that were not pending; calendar rendered corrupted/minimum dates inconsistently.
- **Fix:** split role-specific shells or define a strict role capability map and derived status counts.

### M6. Public diagnostics and sensitive-path logging are production risks

- Swagger and test exception routes are enabled unconditionally.
- `GET /test/exception/{type}` and validation-detail endpoints are public.
- Request logging includes full paths; reset tokens are placed in URL paths.
- **Fix:** gate Swagger/test routes to development, log structured/redacted routes, and submit reset tokens in request bodies where practical.

### M7. There is no automated test suite; frontend lint is failing

- `dotnet test BookSpot.sln --no-restore` found no test projects. The solution contains only API/Application/Domain/Infrastructure projects.
- Backend build succeeded with zero warnings/errors, but this does not validate behavior.
- Frontend build succeeded; `npm run lint` failed with **13 errors and 13 warnings** (including `no-explicit-any`, empty interfaces, and React refresh warnings).
- **Fix:** add domain/unit, repository integration, API authorization/contract, booking concurrency, and Playwright/Cypress E2E tests; make lint/tests required in CI.

### M8. Frontend supply-chain and bundle health need attention

- `npm audit --omit=dev` reported **12 production vulnerabilities**: 1 critical, 5 high, 5 moderate, 1 low. Direct vulnerable packages include `quill` and `xlsx`; high findings include `tar-fs`, `node-tar`, and `serialize-javascript` chains.
- Production build emitted a **1,078.69 kB** JS bundle warning (>500 kB).
- **Fix:** upgrade/replace vulnerable dependencies, verify whether unused packages can be removed, add route-level code splitting, and enforce audit policy with reviewed exceptions.

## Low

### L1. Development diagnostics and console logging are noisy

- Frontend emits mode/API/debug logs across flows; backend logs every request path and timing.
- **Impact:** obscures actionable errors and can leak details in shared environments.
- **Fix:** use environment-aware structured logging with levels and redaction.

### L2. Configuration/documentation has drifted

- Tracked `.env` syntax is malformed; frontend is named both Ubuntu Bookings Spot and HirePros; API docs describe capabilities (notably registration/business rules) that runtime does not provide; CI still uses `localstack:latest`.
- **Fix:** make executable setup and generated OpenAPI the source of truth; add documentation smoke checks.

---

## Runtime flow results

| Flow | Result | Evidence/result |
|---|---|---|
| Frontend homepage -> API services | Pass after manual DynamoDB recovery | Service displayed from `/services` |
| Login as seeded client/provider | Pass | Both returned HTTP 200 and frontend session loaded |
| Register new user | **Fail** | `/auth/register` 404; frontend parsing failure |
| Forgot password | **Fail** | HTTP 400 MediatR handler-construction error |
| Validate arbitrary reset token | **Fail/security** | arbitrary token reported valid |
| Provider create business | Pass | HTTP 201 |
| Provider create service for created business | **Fail** | HTTP 400 ownership error |
| Provider create service using own profile ID as business ID | **Incorrect pass** | HTTP 201 orphan service |
| Service detail -> Book Now | **Fail** | navigated to `/book`, app 404 |
| Direct `/book/{providerId}` selection | Partial | service loads; location/rating placeholders; static slots |
| Two clients/requests same pending slot | **Fail** | both HTTP 201 |
| Client update another booking | **Fail/security** | HTTP 200; timestamps reset to year 1 |
| Provider assign arbitrary booking status | **Fail/security** | HTTP 200 |
| Client dashboard | **Fail/partial** | provider copy, blank/mismatched stats |
| Provider dashboard/calendar | Partial | booking shown, but counts/status/date behavior inconsistent |
| Provider business settings | **Fail** | existing business not loaded; wrong ID used |
| Provider add/remove service controls | **Fail** | visible controls do nothing |
| Backend build | Pass | 0 warnings, 0 errors |
| Backend tests | **No coverage** | no test projects |
| Backend dependency audit | Pass | no vulnerable NuGet packages reported |
| Frontend production build | Pass with warning | 1.078 MB main JS chunk |
| Frontend lint | **Fail** | 13 errors, 13 warnings |
| Frontend production dependency audit | **Fail** | 12 vulnerabilities |

---

## Backend capabilities still missing

### Required for a safe MVP

1. Reachable registration and verified account recovery.
2. Email verification and duplicate-email uniqueness (case-normalized, atomic).
3. Global authentication defaults plus resource-level ownership authorization.
4. Canonical provider/business identity lookup.
5. Availability query, time-zone model, business-hours enforcement, lead time, buffers, blocked dates, and atomic slot reservation.
6. Booking lifecycle: pending/accepted/declined/cancelled/completed/no-show; allowed transitions; actor permissions; timestamps and audit trail.
7. Immutable booking snapshots for price, service name, duration, provider/business identity.
8. Review listing/aggregation by service/provider, booking verification, one-review-per-completed-booking rules.
9. Real client/provider dashboard queries.
10. Notification delivery and retry/outbox semantics for booking and recovery events.
11. Health/readiness endpoints and reliable local/CI infrastructure.
12. Automated tests, especially authorization and concurrency.

### Needed before scale/production

- DynamoDB GSIs and cursor pagination instead of scans.
- Idempotency keys for registration/booking/mutations.
- Conditional writes/versioning to avoid lost updates.
- Rate limiting and abuse controls for auth/search/write endpoints.
- Refresh/revocation/logout strategy or short-lived access tokens with secure session storage.
- Production secret management and key rotation.
- Structured audit logging, metrics, tracing, alerting, and redaction.
- API versioning and generated client contract.
- Data retention/deletion and privacy controls.

---

## Recommended execution order

### Phase 0 — Freeze unsafe release paths

- Disable anonymous profile/business-hour/review mutations.
- Disable/reset-token stub and public test endpoints outside development.
- Add server-side booking ownership and status allow-list immediately.

### Phase 1 — Restore coherent core flows

- Define profile/business/service IDs and migrate inconsistent data.
- Implement registration/recovery with one password hasher.
- Repair service creation and provider business lookup.
- Change booking route to be service-centric.

### Phase 2 — Make booking correct

- Build availability service from business hours, existing reservations, duration, and time zone.
- Reserve slots atomically, including pending holds.
- Add explicit booking transition commands and immutable snapshots.

### Phase 3 — Replace mocks/dead UI

- Align generated frontend types to OpenAPI.
- Replace fake dashboard/client/rating data.
- Wire or remove every visible action; add useful error states.

### Phase 4 — Quality and operations gates

- Pin LocalStack and fix executable init scripts/region/table inventory.
- Add integration/E2E/concurrency tests and CI gates.
- Resolve frontend audit/lint/bundle issues.
- Add production config validation, health checks, and observability.

---

## Testing notes and limitations

- Testing used an ephemeral local DynamoDB dataset created solely for QA. No production system or real user data was touched.
- The backend checkout had a **pre-existing uncommitted change** in `BookSpot.Application/BookSpot.Application.csproj`; it was preserved and not modified.
- Frontend and backend source remained unchanged. The only generated deliverable is this report; dependencies/build outputs and ephemeral LocalStack/test data were used for execution.
- Email delivery, AWS Lambda/API Gateway deployment, mobile-device responsive matrices, screen-reader testing, browser compatibility, load testing, and real production secrets were not tested.
