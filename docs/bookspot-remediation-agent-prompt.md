# AI Coding Agent Prompt — Repair BookSpot / Ubuntu Bookings Spot

You are the senior engineer responsible for repairing the BookSpot full-stack application. Work methodically, test-first, and in small verified vertical slices. Do not attempt a broad rewrite, do not silently change product semantics, and do not claim completion without real command and browser/API evidence.

## 1. Objective

Make the existing Ubuntu Bookings Spot frontend and BookSpot backend into a coherent, secure, locally reproducible booking MVP.

Definition of done:

1. A new client or provider can register, log in, log out, request a password reset, reset the password, and log in with the new password.
2. A provider can create or retrieve their business, update it, configure real business hours, create/update/delete services owned by that business, and see real dashboard data.
3. A client can discover a service, see accurate business/provider/review data, query actual availability, create exactly one booking for an available slot, and see/manage only their own bookings.
4. A provider can see only bookings for their business, accept/decline them through valid state transitions, and cannot corrupt dates or modify unrelated records.
5. Concurrent or repeated requests cannot reserve the same slot twice.
6. Anonymous users cannot mutate private resources or read sensitive profile/booking data.
7. Frontend and backend share one documented API contract and handle errors consistently.
8. Local setup starts reliably from repository instructions without manual table creation or region workarounds.
9. Backend and frontend tests, builds, linting, and dependency gates pass, or any unavoidable pre-existing exception is explicitly documented with evidence.

## 2. Repositories and environment

- Monorepo root: `C:\Repository`
- Backend: `C:\Repository\backend`
- Frontend: `C:\Repository\frontend`
- Backend branch at audit time: `master`
- Frontend branch at audit time: `main`
- Full audit report: `C:\Repository\docs\bookspot-full-stack-assessment.md`
- Expected local URLs:
  - frontend: `http://localhost:8080`
  - backend: `http://localhost:5000`
  - LocalStack: `http://localhost:4566`
- Shell is Git Bash/MSYS on Windows. Use POSIX shell syntax while preserving native Windows paths where tools require them.

Important repository state:

- The backend had a pre-existing uncommitted change in `BookSpot.Application/BookSpot.Application.csproj` before this task.
- Do not discard, overwrite, stage, or commit that change accidentally.
- First capture `git status`, `git diff`, current branches, and recent commits in both repositories.
- If that existing change must be incorporated to fix a defect, explain exactly why and separate it from unrelated work. Otherwise preserve it unstaged.
- Do not modify or analyze the unrelated `payment-reconciliation-mvp` subtree in the frontend.

## 3. Operating rules

### 3.1 Phase lock

Work through the phases below in order. Never start a later phase while the current phase has failing acceptance tests.

For every phase:

1. Restate the exact behaviors being changed.
2. Inspect the relevant implementation and current contracts.
3. Write the smallest failing test that reproduces one defect.
4. Run it and confirm it fails for the expected reason.
5. Implement the smallest coherent fix.
6. Run the focused test, then the full relevant suite.
7. Run static analysis/build/lint.
8. Review the diff for security, ownership, data integrity, and accidental scope expansion.
9. Commit only that phase with a clear conventional commit message.
10. Record commands, outputs, files changed, migrations/data implications, and remaining work.

Use vertical slices; do not write every test first and then implement everything.

### 3.2 Safety and scope

- Do not rewrite the architecture unless a failing test proves the existing design cannot satisfy a required invariant.
- Do not replace DynamoDB or React/Vite.
- Do not add payment functionality; payments are out of scope.
- Do not fabricate API responses, dashboard data, ratings, availability, or successful UI outcomes.
- Do not return persistence/domain entities directly from controllers.
- Do not log passwords, JWTs, password hashes, reset tokens, or unredacted authorization headers.
- Do not introduce hard-coded secrets.
- Do not weaken tests to make implementation pass.
- Do not leave visible buttons with no action. Wire them correctly or remove/disable them with an explicit explanation.
- Do not use blind delays as verification. Use health checks, HTTP responses, test assertions, and browser/network evidence.
- If blocked for more than approximately 30 minutes without meaningful progress, stop, document the root cause and evidence, and request the smallest necessary clarification.

### 3.3 Source of truth

The audit report is evidence, not an immutable design. Verify each claim against current code and runtime before changing it. When documentation, frontend types, Swagger, and runtime disagree, define the desired behavior in a failing contract test and make implementation plus documentation conform to it.

## 4. Non-negotiable domain invariants

Implement and test these invariants explicitly:

### Identity and ownership

- A provider is a `Profile` with provider role/type.
- A provider owns one or more `Business` records through an explicit `ProviderId`; a business ID is never interchangeable with a profile ID.
- A `Service` belongs to a real `Business` by `BusinessId`.
- Provider identity for a service is derived through its business; clients cannot supply authoritative provider IDs/names.
- A `Booking` belongs to one client and one service; business/provider identity is derived server-side.
- Clients can read/mutate only their own permitted booking actions.
- Providers can read/mutate bookings only for services owned by their businesses.

### Authentication and privacy

- Email comparison is normalized and case-insensitive.
- Registration is atomic enough to prevent duplicate emails.
- Password registration, login, and reset use one injected password-hashing abstraction backed by BCrypt.
- Reset tokens are checked against storage for existence, expiry, and used state.
- Public profile DTOs never contain `PasswordHash` or internal security fields.
- Role changes cannot occur through ordinary self-service profile update endpoints.
- Production startup fails if JWT configuration is missing, empty, predictable, or too short.

### Booking lifecycle

Define a single status enum/value object and an explicit state machine. At minimum support:

- `pending -> accepted | declined | cancelled`
- `accepted -> completed | cancelled | no-show`
- terminal states cannot transition arbitrarily

Define which actor can perform each transition. Do not implement generic free-text status updates.

A booking must snapshot at creation:

- service name;
- booked price/currency;
- duration;
- business/provider display identity needed for historical display.

Client-supplied values cannot override these authoritative fields.

### Availability and concurrency

- Availability is based on business hours, service duration, timezone, blocked/unavailable periods, accepted bookings, and pending reservation policy.
- Pending bookings reserve the slot, optionally with a documented expiry policy.
- The final conflict check and write are atomic/conditional. A scan-then-save sequence alone is insufficient.
- End time is derived from start time plus service duration.
- A repeated/idempotent request must not create duplicates.

## 5. Required implementation phases

## Phase 0 — Baseline, reproducible infrastructure, and test harness

Goals:

1. Preserve the pre-existing backend diff.
2. Establish backend unit/integration/API test projects and frontend component/E2E test capability if absent.
3. Repair deterministic local infrastructure.

Required fixes:

- Pin LocalStack to a known supported version/digest instead of `latest`.
- Ensure ready scripts use LF line endings, are executable inside Linux containers, and fail loudly.
- Provision all required tables, including `password_reset_tokens`.
- Make API DynamoDB configuration read one typed configuration source, including explicit region and service URL.
- Ensure local configured region and table region match.
- Add readiness/health checks for API and DynamoDB.
- Gate Swagger/test exception endpoints to development.
- Add one integration smoke test that starts against LocalStack and performs a real repository save/read.

Acceptance tests:

- From a clean local data volume, one documented command starts LocalStack and creates every expected table.
- API health/readiness becomes healthy only when DynamoDB is reachable.
- `GET /services` returns 200 with an empty list on a clean database, not 500/hang.
- Backend build and new smoke tests pass.

Suggested commit: `test: establish BookSpot integration harness and reproducible local stack`

## Phase 1 — Authentication, profile privacy, and authorization foundation

Goals:

- Restore registration/recovery.
- Close anonymous and cross-account access.
- Stop leaking persistence entities.

Required fixes:

1. Implement and expose `[AllowAnonymous] POST /auth/register` using the existing command only after verifying/fixing it.
2. Normalize email and enforce uniqueness; handle concurrent duplicate registration.
3. Introduce one `IPasswordHasher` used by registration, login, and reset.
4. Fix forgot-password handler DI and email abstraction. For local tests, use a deterministic fake/capture email service; do not require a real AWS email account.
5. Make reset-token validation query stored token state; mark tokens used atomically.
6. Add a secure production JWT configuration validator and remove predictable fallbacks.
7. Add an authenticated fallback policy. Mark only deliberately public routes `[AllowAnonymous]`.
8. Replace controller returns of `Profile` entities with DTOs that exclude password hash.
9. Protect profile reads/updates/deletes with self/admin policy. Ordinary updates cannot change role.
10. Add rate-limit hooks or documented policies for login/register/forgot-password.

Minimum negative tests:

- anonymous profile create/update/delete => 401/403;
- client A cannot read/update/delete client B;
- profile JSON never contains `passwordHash`;
- case-variant duplicate email registration is rejected;
- fake/expired/used reset token is rejected;
- reset password can log in through BCrypt afterward;
- missing/empty production JWT key fails startup.

Acceptance flow:

- Register client and provider from frontend, log in, restore session, request reset, capture reset token in local test email, reset, and log in with new password.

Suggested commit: `fix(auth): secure registration recovery profiles and JWT configuration`

## Phase 2 — Canonical provider/business/service model

Goals:

- Eliminate profile/business ID conflation.
- Make provider onboarding and service management work end to end.

Required fixes:

1. Define/query business ownership through `Business.ProviderId`.
2. Add an authenticated endpoint such as `GET /businesses/me` or `GET /businesses/provider/{providerId}` with ownership-safe behavior.
3. Make service create/update/delete resolve the business and verify `business.ProviderId == currentUserId`.
4. Reject services for non-existent businesses.
5. Decide whether one provider may own multiple businesses. Encode and test the decision; do not infer IDs.
6. Add service response DTOs enriched with business/provider/location data required by frontend.
7. Repair provider Settings to retain and use the actual business ID.
8. Wire Add Service, Edit, Remove, and Save to real API mutations with confirmation and error handling.
9. Provide business-hours list/query by business; persist availability settings to actual records.

Data migration:

- Detect orphan services whose `BusinessId` is actually a provider profile ID.
- Write a safe, idempotent migration or repair script with dry-run output.
- Never silently delete ambiguous data.

Acceptance flow:

- Provider registers -> creates/retrieves business -> updates business -> configures hours -> creates service -> edits it -> sees it publicly -> deletes/archives it.
- A different provider receives 403 for every mutation.

Suggested commit: `fix(domain): enforce provider business and service ownership`

## Phase 3 — Booking integrity, lifecycle, and availability

Goals:

- Make the central booking flow correct under normal and concurrent use.

Required fixes:

1. Add an availability endpoint, preferably service-centric:
   `GET /services/{serviceId}/availability?from=...&to=...&timezone=...`.
2. Generate slots from persisted business hours and service duration.
3. Exclude past/closed/blocked/conflicting slots.
4. Replace client-supplied provider name/provider ID/end time with server-derived fields.
5. Snapshot booked service data and price.
6. Implement explicit booking transition commands/routes rather than generic arbitrary status updates.
7. Enforce client/provider ownership for reads and transitions.
8. Prevent pending-slot duplicate bookings with DynamoDB conditional/transactional semantics.
9. Add idempotency behavior for repeated create requests.
10. Decide cancellation/reschedule rules and encode them in tests.
11. Fix frontend route to a canonical service-centric booking URL, e.g. `/book/service/:serviceId`.
12. Replace static time slots with API availability.
13. Ensure every booking screen awaits the real API result; remove simulated-success booking behavior.

Mandatory concurrency tests:

- Two concurrent requests for the same slot: exactly one succeeds.
- Repeating the same idempotency key: one booking returned, no duplicate.
- Pending booking blocks the slot according to policy.
- Booking a different provider/service at the same time remains allowed where appropriate.

Mandatory authorization/state tests:

- client A cannot read/update/delete client B booking;
- unrelated provider cannot view or transition it;
- owning provider can accept/decline pending booking only;
- client can cancel only permitted states;
- unknown status rejected;
- status update never modifies start/end unless using an authorized reschedule command.

Acceptance flow:

- Client searches -> opens service detail -> sees real availability -> books one slot -> receives persisted booking ID/status -> sees it in My Bookings.
- Provider sees the request -> accepts it -> client sees accepted state.
- The same slot disappears/unavailable for a second client.

Suggested commit: `fix(bookings): add atomic availability ownership and lifecycle`

## Phase 4 — Contract alignment, dashboards, reviews, and UI completion

Goals:

- Remove mocks, fabricated defaults, dead controls, and contract drift.

Required fixes:

1. Treat backend OpenAPI as the contract source and generate or rigorously synchronize frontend types/client.
2. Standardize success/error responses. Frontend must:
   - read `VITE_API_BASE_URL`;
   - validate it at startup;
   - handle 204 without JSON parsing;
   - parse problem-details/non-JSON errors safely;
   - preserve HTTP status;
   - apply timeouts and global 401 handling.
3. Decide and implement one search response shape with total count, page, page size, total pages/cursor, and stable ordering.
4. Add category to the backend model/filter only if it is a real product requirement; otherwise remove category UI/contract assumptions.
5. Replace mock provider clients and client stats with real queries.
6. Separate provider and client dashboard copy, fields, actions, and DTOs.
7. Compute request badges from pending records only.
8. Implement service review listing/aggregation, verified completed-booking eligibility, and one-review-per-booking constraints.
9. Remove fake ratings, review counts, provider contacts, and availability defaults.
10. Wire or remove all dead/simulated controls: Add Service, Remove, New Booking, Add Client, search booking modal, and any local-only success toast.
11. Ensure errors do not silently appear as empty states.
12. Improve keyboard semantics, labels, focus behavior, and accessible error feedback for changed flows.

Acceptance tests:

- OpenAPI contract tests cover frontend-consumed endpoints.
- Client dashboard shows only real client metrics.
- Provider dashboard shows only real business metrics.
- Search pagination/counts are correct.
- Empty state differs from request failure.
- All visible actions either work against the API or are intentionally absent.

Suggested commit: `fix(frontend): align API contracts and replace mock dashboard flows`

## Phase 5 — Performance, supply chain, and release gates

Goals:

- Remove known release risks after correctness is established.

Required fixes:

1. Replace DynamoDB scans with designed access patterns/GSIs for:
   - normalized profile email;
   - business by provider;
   - services by business/provider;
   - bookings by client;
   - bookings by provider/business and time;
   - reviews by service/booking;
   - reset tokens as needed.
2. Use cursor pagination where appropriate; avoid N+1 business lookups.
3. Resolve frontend production dependency vulnerabilities. Upgrade, replace, or remove vulnerable packages, especially direct dependencies such as `quill` and `xlsx`; document any accepted residual risk.
4. Fix all frontend lint errors and warnings in touched code, ideally entire project.
5. Add route-level code splitting and reduce the main JS chunk below the current approximately 1.08 MB, or document a justified budget.
6. Add CI gates for backend tests/build/vulnerability scan and frontend tests/type-check/lint/build/audit.
7. Gate diagnostics by environment and use structured redacted logging.
8. Update README/setup/API documentation to match executable reality.

Suggested commit: `chore: add production quality performance and security gates`

## 6. Required tests and tooling

Create test structure where absent. Prefer real behavior over excessive mocking.

Backend minimum:

- domain/unit tests for transition and ownership rules;
- integration tests against LocalStack for repositories and conditional booking writes;
- API tests with real authentication policies and problem-details responses;
- concurrency tests for booking and duplicate registration;
- OpenAPI snapshot/contract tests.

Frontend minimum:

- API client unit tests for JSON, problem-details, 204, 401, and timeout behavior;
- component tests for auth, settings, availability, booking, and role dashboards;
- E2E tests for registration/login/recovery and provider/client booking lifecycle.

Run and report at least:

Backend:

```bash
dotnet restore BookSpot.sln
dotnet build BookSpot.sln --no-restore
dotnet test BookSpot.sln --no-build
dotnet list BookSpot.sln package --vulnerable --include-transitive
```

Frontend:

```bash
npm ci
npm run lint
npm run build
npm audit --omit=dev
# Run the test/E2E scripts you add, by their exact package.json names.
```

Runtime:

- Start clean LocalStack/API/frontend.
- Verify health endpoints.
- Run API integration suite.
- Run browser E2E suite.
- Verify browser console has no errors during core flows.

## 7. Definition of a completed phase

A phase is not complete unless all are true:

- relevant failing tests were observed before implementation;
- focused and full tests pass;
- builds pass;
- no new lint/type errors;
- security/ownership negative tests pass;
- runtime flow was exercised where applicable;
- no persistence entity leaks into public JSON;
- no fabricated success or data remains in the touched flow;
- git diff contains only phase-related changes;
- existing unrelated user changes are preserved;
- the phase has a clear commit and written evidence.

Do not use “should work”, “appears fixed”, or invented results. Quote actual command summaries and HTTP/browser outcomes.

## 8. Final deliverables

At the end provide:

1. Executive summary of repaired flows.
2. Commit list grouped by phase and repository.
3. Files changed and why.
4. Schema/index/data-migration changes and rollback instructions.
5. Test matrix with commands and actual results.
6. Security and authorization matrix for each endpoint family.
7. Remaining known risks or intentionally deferred items.
8. Updated setup instructions proving a clean-machine local start.
9. Before/after API contract notes.
10. A concise manual QA script for client and provider flows.

## 9. First response before coding

Before modifying anything, respond with:

1. Current git status/branches for both repositories.
2. The exact pre-existing backend diff you will preserve.
3. Current baseline build/test/lint/audit results.
4. A short phase-by-phase file/test plan based on actual code inspection.
5. Any genuine blocker that prevents Phase 0.

Then begin Phase 0. Do not start by rewriting production code.
