# BookSpot approved API contract and remediation plan

**Date:** 2026-08-10
**Status:** implementation-grade synthesis; analysis/documentation only
**Scope:** `C:\Repository\backend`, the main `C:\Repository\frontend`, and supporting infrastructure.
**Explicit exclusions:** `frontend/payment-reconciliation-mvp/**`; all application-code changes in this task.
**Protected dirty file:** `backend/BookSpot.Application/BookSpot.Application.csproj` must not be modified, formatted, reverted, stashed, or committed by work arising from this document without a separately approved task.

## 0. Authority, interpretation, and release verdict

This document synthesizes and resolves, where engineering defaults permit, the following source handoffs:

- Product requirements: `docs/bookspot-product-requirements-2026-08-09.md`.
- Domain/API draft: `docs/bookspot-domain-api-contract-2026-08-10.md`.
- Security contract: `docs/bookspot-security-contract-2026-08-10.md`.
- DynamoDB concurrency design: `docs/bookspot-booking-concurrency-design-2026-08-09.md`.
- Baseline: `docs/bookspot-baseline-report-2026-08-09.md`.
- Current working-tree code, re-read on 2026-08-10.

Normative words **MUST**, **MUST NOT**, **SHOULD**, and **MAY** are intentional. Security and integrity controls are backend/application/persistence obligations. React route guards, hidden buttons, client-side roles, CORS, body/path identifiers, and denormalized IDs are never security boundaries.

**Release verdict: NOT RELEASABLE.** P0 items and all “release blocker” acceptance tests in this document must be complete and passing. Human decisions HD-01 and HD-02 must be resolved or explicitly risk-accepted before authentication is released. Existing code has no automated backend test project, so implementing the mandatory suite is itself a release blocker.

## 1. Current-tree verification and defect register

### 1.1 Repository facts and protected state

- The repository is a monorepo on `main`; backend and frontend are directories, not independent Git repositories (`docs/bookspot-baseline-report-2026-08-09.md:10-47`).
- The current tree has multiple pre-existing tracked and untracked changes. This is broader than the baseline’s original one-file dirty state; implementation work MUST re-run `git status --short` and preserve all unrelated changes.
- The protected project file’s observed SHA-256 before this document was written was `5215671940f546d3f291220f6d5fb5fba3ad1306eb2ef37b4ac59fe703ac7d4d`.
- `frontend/payment-reconciliation-mvp/**` is unrelated and excluded (`docs/bookspot-baseline-report-2026-08-09.md:156-171`).

### 1.2 Re-verified facts

| ID | Current status | Verified defect / fact and evidence | Consequence |
|---|---|---|---|
| C1 | confirmed | `AuthController` has login/recovery only; no register action (`backend/BookSpot.API/Controllers/AuthController.cs:24-120`) although `RegisterCommand` exists (`backend/BookSpot.Application/Features/Auth/Commands/RegisterCommand.cs:1-5`). | Main registration flow 404s. |
| C2 | confirmed | Profile mutations are anonymous and return `Profile` entities (`backend/BookSpot.API/Controllers/ProfilesController.cs:24-73`). Provider can read arbitrary profiles at lines 28-36. | Account takeover/IDOR and `PasswordHash` disclosure. |
| C3 | confirmed | Generic booking PUT/DELETE accept either role and have no party check (`BookingsController.cs:117-156`); update overwrites both times and free-string status (`UpdateBookingCommand.cs:7-24`). | Cross-user mutation, illegal state, `DateTime.MinValue` corruption. |
| C4 | confirmed | Service create compares `request.BusinessId` directly to JWT profile ID (`CreateServiceCommand.cs:42-59`) rather than loading Business ownership. | Provider cannot safely manage real business IDs; profile/business conflation. |
| C5 | confirmed | Any non-empty reset token validates (`AuthController.cs:92-118`); reset hashes passwords with raw SHA-256 and saves profile before separately marking token used (`ResetPasswordCommand.cs:31-88`). | Recovery is unusable after reset and replay/race-prone. |
| H1 | confirmed | Conflict scan excludes `pending` and check/save is non-atomic (`BookingRepository.cs:17-31`; `CreateBookingCommand.cs:82-103`). | Double booking under ordinary and concurrent requests. |
| H2 | partially remediated in dirty tree | Baseline’s hard-coded LocalStack endpoint/region is stale: current `Program.cs:156-185` reads config and `docker-compose.yml:1-27` pins LocalStack 3.8.1 to `us-east-1`. Local/production table parity and automated provisioning verification remain unproven. | Do not discard current infra work; still require parity/boot tests. |
| H3 | confirmed | Production secret is blank (`appsettings.json:9-13`); startup accepts blank or the source fallback and disables HTTPS metadata (`Program.cs:55-78`). | Predictable/invalid signing boundary and unsafe production startup. |
| H4 | confirmed by handoff | Service “Book Now” can target absent `/book`; no backend service availability endpoint (`baseline:88-91`). | Core booking navigation fails. |
| H5 | confirmed by handoff | Search booking modal reports success without awaiting persistence (`baseline:92-93`). | False success/data loss. |
| H6 | confirmed by handoff | Settings sends profile ID as business ID (`baseline:94-95`; domain contract:34-38). | Wrong-resource reads/updates. |
| H7 | confirmed by handoff | Booking UI uses static slots and backend has no availability projection (`baseline:96-97`). | Availability is fictitious and unenforced. |
| H8 | confirmed | Business-hours and review mutations have no authorization (`BusinessHoursController.cs:9-42`; `ReviewsController.cs:9-42`); service delete has no ownership check (`DeleteServiceCommand.cs:8-18`). | Anonymous/cross-tenant mutation. |
| H9 | confirmed by handoff | Dashboard returns fabricated client data and mutable-price revenue (`domain contract:49-51,582-609`). | False reporting and tenant/financial integrity risk. |
| H10 | confirmed by handoff | Frontend hard-codes API URL and unconditionally parses JSON, including 204 (`baseline:102-103`; domain contract:609). | Valid empty responses appear as failures. |
| H11 | confirmed | Create accepts client `EndTime` and `ProviderName`, chooses supplied end, and stores business ID in `Booking.ProviderId` (`CreateBookingCommand.cs:9,59-100`). | Spoofed data, arbitrary duration, ambiguous ownership. |
| M1 | confirmed by handoff | Search returns a bare array while frontend expects a paged envelope; category is incomplete (`baseline:107-109`). | Contract mismatch and incomplete search. |
| M2 | confirmed | Booking provider/client/conflict access uses scans (`BookingRepository.cs:17-53`). | Unbounded latency/cost; not a concurrency authority. |
| M3 | confirmed by handoff | Settings and dashboards contain dead/simulated controls (`baseline:109-110`). | Misleading UX. |
| M4 | confirmed by handoff | Frontend fabricates provider/location/rating fallbacks (`baseline:111`). | False marketplace data. |
| M5 | confirmed by handoff | Client/provider navigation and terminology are inconsistent (`baseline:112`). | Role confusion and unsafe UX assumptions. |
| M6 | confirmed | Production maps Swagger unconditionally and logs full request paths (`Program.cs:214-264`); reset token is in a path (`AuthController.cs:92`). Test routes are public per security inventory. | Secret leakage and diagnostic exposure. |
| M7 | confirmed by baseline; not re-run here | No backend tests; frontend lint failed with 13 errors/13 warnings (`baseline:115-118`). | No enforceable regression gate. |
| M8 | confirmed by baseline; advisory state not re-run here | Production dependency audit found 12 vulnerabilities and bundle >1 MB (`baseline:119-121`). | Supply-chain and performance risk; exact advisory state must be refreshed during remediation. |
| L1 | confirmed | Request origin/method/path and response CORS headers are console-logged (`Program.cs:214-246`). | PII/token-path leakage and noisy telemetry. |
| L2 | partially changed in dirty tree | Local AWS config/pinning improved, but production JWT/config and docs/runtime contracts remain divergent. | Deployments remain environment-sensitive. |

Classification rule: “confirmed” means current executable code or a still-applicable cited current-tree handoff demonstrates the defect. “Risk” is reserved for races/scale behavior not dynamically reproduced. No dynamic penetration test was performed.

## 2. Adopted defaults, human decisions, and interim assumptions

### 2.1 Adopted engineering/security defaults

1. One immutable role per account: `client` or `provider`; no admin role.
2. JWT subject is `profileId`; role comes from persisted server state.
3. Provider ownership resolves through `Business.ProviderId`; never through a body/path ID or denormalized field.
4. Explicit DTO allow-lists; persistence entities never cross API boundaries.
5. Global authenticated fallback policy; every public endpoint explicitly opts into anonymous access.
6. Email is trimmed, NFC-normalized and invariant-lowercased for lookup/uniqueness; uniqueness is atomic.
7. One password hasher is used for register/login/reset; raw SHA-256 is prohibited.
8. Reset capabilities are 256-bit random, digest-only at rest, 30-minute, latest-generation, body-only, atomically single-use, and revoke sessions.
9. `pending` and `confirmed` bookings consume capacity. Booking create/action/reschedule are atomic and idempotent.
10. Public availability is advisory; transactional create is authoritative.
11. Canonical currency is `ZAR`; interim timezone is `Africa/Johannesburg` and is always returned explicitly.
12. No mock/fabricated data in production flows.

### 2.2 Unresolved human decisions

| ID | Decision | Interim assumption | Release impact |
|---|---|---|---|
| HD-01 (E6/security conflict) | Immediate self-registration with 201/409 versus verification-capability flow with generic 202. | Preserve the product draft’s exact immediate 201/409 contract only in non-production while approval is pending. Production release requires explicit enumeration-risk acceptance with rate limits, or adoption of generic 202 verification. | **Blocking.** Exact production status cannot be finalized by engineering alone. |
| HD-02 (password-policy conflict) | Product compatibility rule (8–100, composition, BCrypt >=11) versus security default (15–64+, no composition, BCrypt calibrated >=12, 72-byte handling). | Security default: 15–64 Unicode characters/spaces, common-password denylist, no composition rule, reject UTF-8 inputs beyond BCrypt’s 72-byte boundary unless a versioned prehash is approved, BCrypt cost >=12. | **Blocking.** Register/reset must use one approved rule. |
| HD-03 (E1) | Dual-role accounts. | No; role immutable and mutually exclusive. | Non-blocking for MVP. |
| HD-04 (E2) | Providers booking other providers. | No; separate client account. | Non-blocking. |
| HD-05 (E3) | Cancellation cut-off/fees and no-show consequences. | Either party may cancel when the server admits the action before start (`actionNow < startTime`), no fee; `no_show` recorded but inert. This is deliberately admission-time, not DynamoDB commit-time, because wall clock is not transaction-conditionable. | Fees/consequences deferred; state safety not blocked. |
| HD-06 (E4/DB-D1) | Multiple staff/rooms/resources. | One exclusive resource per business; `resourceId=single`. | Changing before launch alters reservation keys and backfill. |
| HD-07 (E5) | Online HIBP lookup. | Local common-password denylist only. | Non-blocking. |
| HD-08 (E7) | Revenue timing, gross/net, currency/timezone. | Completed only, gross, ZAR, Africa/Johannesburg. | Non-blocking if explicitly labeled interim. |
| HD-09 (E8) | Lead time, buffers, blocked dates, expanded timezone rules, pending expiry. | Business hours only; no buffer/lead time/automatic pending expiry. | Non-blocking for atomic overlap; availability expansion deferred. |
| HD-10 (E9) | Keep frontend mock mode. | Treat as dead production code; test fixtures remain permitted. | Release blocks fabricated production results. |
| HD-11 | Account deletion retention and normalized-email reuse. | Refuse deletion with 409 where retention/integrity cannot be preserved; keep normalized email reserved. | Needed before enabling account deletion. |
| HD-12 | Access-token browser transport. | Secure HttpOnly SameSite cookie or in-memory bearer; no long-lived localStorage. | Deployment decision before production. |
| HD-13 | Production Swagger operator need. | Unmapped in Production. | Non-blocking unless operations require it. |
| DB-D2 | Require service duration on 15-minute grid. | Yes; inventory and repair exceptions, never round silently. | **Cutover blocker.** |
| DB-D4 | Resolve pre-existing overlapping live bookings. | No automatic winner/cancellation. | **Cutover blocker.** |
| DB-D5 | Actual deployed GSIs/billing mode. | Discover with `DescribeTable`; no GSI assumed for correctness. | Query rollout only. |
| DB-D6 | Idempotency retention beyond lifecycle +24h. | Lifecycle plus 24 hours minimum. | Non-blocking. |
| DB-D7 | Historical offsetless timestamps. | Use only documented source timezone; never guess. | **Cutover blocker for unresolved future live rows.** |

## 3. Canonical entities and identifier semantics

```text
Profile(providerProfileId, userType=provider)
  1 -> many Business(businessId, providerProfileId)
           1 -> many Service(serviceId, businessId, derived providerProfileId)
                         1 -> many Booking(bookingId, serviceId, businessId,
                                           providerProfileId, clientProfileId)
Profile(clientProfileId, userType=client) -> many Booking
```

| Contract ID | Authority | Rules |
|---|---|---|
| `profileId` | `Profile.Id` / JWT subject | Generic identity; never a business ID. |
| `providerProfileId` | `Business.ProviderId` | Role-qualified profile ID; denormalized copies are display/query aids only. |
| `clientProfileId` | JWT subject on create; canonical booking field | Never accepted as booking owner from request. |
| `businessId` | `Business.Id`; `Service.BusinessId` | Current ownership and capacity aggregate. |
| `serviceId` | `Service.Id` | Booked offering; not the exclusive-capacity boundary. |
| `bookingId` | server-generated `Booking.Id` | Stable booking identity; not an idempotency key. |
| `resourceId` | reservation design | Interim literal `single`; future staff/room identity. |

IDs are opaque, non-empty, case-sensitive strings. Clients MUST NOT infer GUID structure. Existing `Booking.ProviderId` currently contains `service.BusinessId` (`CreateBookingCommand.cs:82-99`), so it remains deprecated and ambiguous during migration. New booking rows add `BusinessId`, `ProviderProfileId`, `PriceAmountSnapshot`, `Currency`, `UpdatedAt`, and numeric `Version`. Authorization always resolves Booking -> Service -> Business.

## 4. Role model

| Actor | Identity | Capabilities |
|---|---|---|
| Anonymous | no valid bearer session | Explicit public auth endpoints and active marketplace reads only. |
| Client | authenticated `sub`, role `client` | Self profile; create/read/action own bookings; author/manage own eligible reviews; own dashboard. |
| Provider | authenticated `sub`, role `provider` | Self profile; own businesses/services/hours; own-business booking actions/views; own dashboard. |

There is no admin/support override. Unknown, absent, conflicting, or multiple subject/role claims fail closed. Token validity does not replace loaded-resource authorization.

## 5. Transport, validation, and standard errors

### 5.1 Conventions

- JSON uses `camelCase`; success content type `application/json`; errors `application/problem+json`.
- Input instants MUST be RFC 3339 with explicit offset; output is canonical UTC `Z`, whole-second precision. Offsetless values are 400.
- Booking intervals are half-open `[start,end)`. Starts and service durations are on a 15-minute grid; duration is 15–480 minutes.
- Local schedule times use `HH:mm` plus IANA timezone. Availability ranges are at most 31 days.
- Money is decimal major units plus ISO-4217 currency; no binary floating-point calculation.
- Collections are non-null arrays. Empty collections return 200 `[]`; successful delete returns 204 with no body.
- Unknown enum values, unknown/dangerous JSON fields, and overposted server-owned fields fail 400. An explicit compatibility adapter MAY ignore a documented legacy alias only when tests prove canonical server values win.
- PATCH omission means unchanged. `null` clears only documented nullable fields.
- Field errors use camelCase keys. Validation runs after authentication/ownership where needed to avoid existence/state disclosure.

### 5.2 Standard ProblemDetails envelope

```json
{
  "type": "https://bookspot.example/problems/validation-failed",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more request fields are invalid.",
  "instance": "/bookings",
  "code": "validation_failed",
  "traceId": "opaque-correlation-id",
  "errors": { "startTime": ["invalid_format"] }
}
```

`AuthResponse` is an exact response contract. Canonical properties are always present and aliases are always present (not merely optional in serialized JSON) during compatibility window **AUTH-ALIAS-1**, beginning at first production deployment and ending no earlier than 2026-12-31. Alias values MUST be byte/semantic copies of canonical values: `token=accessToken`, `userId=profile.profileId`, and the remaining aliases equal their `profile` fields; canonical values win if an internal adapter observes disagreement. Every alias emission increments route/client telemetry without recording token or PII. Removal requires 30 consecutive production days with zero alias reads by supported clients, frontend deployment consuming only canonical fields, passing response snapshots, and a published removal note; otherwise the window extends. After removal, aliases are absent on every response. There is no per-request or random presence.

**JWT/session profile:** access tokens are signed JWTs with exactly one each of `sub` (opaque profile ID), `user_type` (`client|provider` from persisted state), `sv` (base-10 security-version integer), `jti` (unique 128-bit-or-more random identifier), `iat`, `nbf`, and `exp`; `iss=bookspot-api` and `aud=bookspot-web`. No email/phone/name or other PII claim is permitted. `iat` and `nbf` are server UTC issuance time; `exp=iat+900 seconds`; accepted clock skew is 30 seconds. Production accepts **only HS256** with an external independently random key of at least 32 bytes. JOSE header has exactly `alg:"HS256"`, `typ:"JWT"`, and required `kid` matching an 8–64 character `[A-Za-z0-9._-]+` allow-listed key ID; at most current+previous keys are active during a documented rotation, and issuance uses current only. Unknown/missing `kid`, `none`, asymmetric algorithms, algorithm substitution, `jku`/embedded key headers, missing/duplicate claims, malformed numeric dates, unknown role, and duplicate subject/role claims fail 401. Issuer, audience, signature, `nbf`, and `exp` validation are mandatory. Changing algorithm family requires a new reviewed contract, not runtime negotiation. `RequireHttpsMetadata=true` outside Development.

Every authenticated request strongly loads or cache-reads `Profile.SecurityVersion` and active state; token `sv` MUST equal persisted value and role MUST equal persisted immutable role. A bounded cache MAY hold this tuple for at most 30 seconds and MUST be synchronously invalidated after password reset/account deletion/security change before success is returned. Security version begins at 1 and increments atomically on each revoking operation. Access-token lifetime is not refreshable in place; no refresh token is defined by this MVP contract. Register/login success uses the current persisted version.

All auth/recovery and all private responses, including errors, set `Cache-Control: no-store, max-age=0` and `Pragma: no-cache`; they MUST NOT set public/shared caching directives. Public active marketplace GETs alone may be cacheable under a separately declared policy. `AuthResponse.expiresAt` is the same instant as JWT `exp`, canonical UTC `Z`.

`errors` appears only for safe field validation. `detail`, logs, and traces MUST NOT contain passwords, hashes, JWTs, reset/idempotency tokens, raw email/phone/address, AWS errors, table names, rival booking identity, or authorization truth.

| HTTP | Stable codes | Semantics |
|---:|---|---|
| 400 | `validation_failed`, `invalid_request`, `reset_token_invalid` | Malformed or semantically invalid caller input. Every unusable reset capability uses the exact `reset_token_invalid` tuple in §9.1. |
| 401 | `authentication_required`, `invalid_credentials` | Missing/invalid/stale session or generic login failure. |
| 403 | `role_forbidden` | Authenticated wrong role for a known capability. |
| 404 | `resource_not_found` or resource-specific equivalent | Missing or deliberately concealed non-owner/non-party object; behavior must be identical. |
| 409 | `email_already_registered`, `booking_slot_conflict`, `booking_state_conflict`, `booking_cancellation_window_closed`, `booking_configuration_conflict`, `idempotency_key_reused`, `idempotency_window_expired` | Atomic uniqueness/domain/concurrency conflict, an authorized cancellation attempted at/after start, or an intentionally expired replay representation. |
| 410 | `legacy_route_removed` | A retired non-secret legacy route is permanently unusable. Reset-capability lifecycle is deliberately not distinguishable by status. |
| 429 | `request_rate_limited` | BookSpot abuse-control bucket exhausted; exact generic tuple and `Retry-After` follow §5.4. This never represents DynamoDB/AWS SDK throttling. |
| 503 | `persistence_unavailable` | Persistence/abuse-store throttling, timeout, unavailable or indeterminate result; retry same idempotency key where applicable. |

Current middleware already emits ProblemDetails but leaks `exception.Message` and lacks stable code/403/409/429/503 mapping (`GlobalExceptionHandler.cs:19-93`); extending it is P0.

### 5.3 Exact request and query validation contract

Validation is server-side and ordinal after the normalization explicitly named below. Unless a row says nullable, JSON `null` fails. Unless a field is marked optional, omission fails. Required strings are trimmed for validation and storage except passwords, tokens, identifiers, and `Idempotency-Key`; a string that is empty after permitted trimming fails. Lengths are Unicode scalar counts; request bodies are UTF-8 JSON and are capped at 64 KiB before binding. Unknown JSON properties fail with `400 validation_failed`, field key equal to the received camelCase property, and token `invalid_value` **except booking create/action bodies**, where the stricter §6 overposting rule uses `must_be_omitted` for every unknown or server-owned property. Duplicate JSON properties, an empty PATCH object, and a PATCH containing no recognized mutable property also fail with field key `$` and `empty_patch` as the sole message token.

The `errors` values are stable machine-oriented tokens, not localized prose. The permitted tokens are `required`, `must_be_omitted`, `blank`, `too_short`, `too_long`, `invalid_format`, `invalid_value`, `out_of_range`, `too_many_items`, `duplicate_item`, `empty_patch`, `invalid_cursor`, `invalid_range`, and `password_policy_failed`. Each failing field appears once; errors are ordered by request-property order and then rule order. HTTP code remains `validation_failed`.

| Field(s) | Exact constraint / normalization |
|---|---|
| Every opaque path/body ID | Required, 1–128 UTF-8 bytes, no trim/case conversion, printable non-control characters; malformed -> field key matching the route name. |
| `email` in auth/business requests | Required unless explicitly optional; trim, NFC, invariant lowercase for lookup; 3–100 scalars and <=254 UTF-8 bytes; exactly one `@`, non-empty local/domain, domain labels and total domain valid after IDNA2008 conversion. Display email preserves NFC-trimmed casing. Controls/whitespace inside fail. |
| `fullName`, `businessName`, service `name` | Required, trimmed, 1–100 scalars; no controls. |
| `description` | Required, trimmed, 1–2000 scalars; no controls other than LF; CRLF normalizes to LF. |
| `address` | Required, trimmed, 1–250 scalars; `city` required 1–100; no controls. |
| `phone`, `contactNumber` | `phone` required; `contactNumber` optional nullable. After trim, 7–32 chars matching `^\+?[0-9][0-9 ()-]{5,30}[0-9]$`; store trimmed input. Empty string is not null. |
| `website`, `imageUrl` | Optional nullable; when present 1–2048 chars absolute HTTPS URL, no credentials, fragment, controls, or non-default port. `http://localhost` is allowed only in Development fixtures, never persisted in Production. |
| `category`, `location` | Optional nullable; when present trimmed 1–100 scalars. |
| `tags` | Optional, defaults `[]`; 0–20 entries, each trimmed/NFC 1–50 scalars, case-insensitive unique after invariant lowercase; preserve first display spelling and input order. |
| `priceAmount` | Required decimal JSON number, `0.00`–`1000000.00` inclusive, at most 2 fractional digits; scientific notation, NaN/infinity, strings, and binary rounding are rejected. Currency is server-fixed `ZAR`. |
| `durationMinutes` | Required integer 15–480 inclusive and divisible by 15. |
| `isActive` | Optional boolean, defaults `true` only on create; no string/number coercion. |
| `rating`, `comment` | Rating required on create, optional on update, integer 1–5. Comment required on create, optional on update, trimmed 1–2000 scalars; update must contain at least one. |
| `password`, `newPassword` | Required string; never trim/normalize. Apply the one HD-02 policy to both fields. Under the interim security default: 15–64 Unicode scalars, 1–72 UTF-8 bytes, local common-password denylist. |
| reset `token` | Required string, base64url without padding encoding exactly 32 decoded bytes (43 ASCII chars); never trim. Malformed values still use the indistinguishable failure tuple, not field-level details. |
| `Idempotency-Key` | Required for every booking mutation; 16–128 printable ASCII bytes, no surrounding whitespace/control characters. Missing/malformed -> `validation_failed`, key `idempotencyKey`; never log or echo. |
| `startTime`, availability `from/to`, booking-list `from/to` | RFC 3339 explicit offset, whole seconds, no fractional seconds; canonicalize to UTC. Booking start is 15-minute aligned. Availability span must be positive and <=31 days. Booking-list span, when both supplied, must be positive and <=366 days. |
| `timeZone` | Required IANA TZDB identifier, 1–64 ASCII characters, present in server-pinned TZDB; Windows timezone names/offset strings fail. |
| business-hours `days` | Exactly 7 items and one of every enum value. Closed: both times MUST be null. Open: both required `HH:mm`, 15-minute aligned, and local `openTime < closeTime`; overnight hours are unsupported in MVP. |
| `action` / `expectedVersion` | Exact lowercase enum; version required integer 1–2,147,483,647. `startTime` required only for `reschedule` and MUST be omitted otherwise. |
| `status` list filter | Optional exact lowercase booking enum; comma lists/arrays fail. `sort` optional `asc|desc`, default `desc`. |
| Search `q`, `name`, `category`, `city` | Optional; trim/NFC. `q` 1–200 when present; category/city 1–100. Current `name` is a deprecated response-neutral query alias for `q`, follows the same 1–200 rule, telemetry/sunset in §7.1, and supplying both is 400 `invalid_request`. Blank supplied values fail rather than becoming absent. |
| Search `minPrice`, `maxPrice` | Optional decimals with the same format/range as `priceAmount`; if both present `minPrice <= maxPrice`. |
| Search `minDuration`, `maxDuration` | Optional integers 15–480 divisible by 15; if both present `minDuration <= maxDuration`. |
| Search `page`, `pageSize` | Decimal integers only. Defaults `page=1`, `pageSize=20`; `page` 1–100000, `pageSize` 1–100. Repeated query keys fail. |
| Booking-list `businessId` | Optional only on provider route; same ID constraint and must be owned. On client route it is an unknown query and fails. |
| Booking-list `cursor` | Optional opaque base64url JSON envelope, 1–2048 chars, HMAC-authenticated and bound to route, subject, normalized filters, sort, and schema version; malformed/tampered/mismatched/expired (>24h) -> `400 validation_failed`, `errors.cursor=[\"invalid_cursor\"]`. |
| Legacy `startDate`, `endDate` | On current booking ID aliases they are deprecated aliases for canonical `from/to`; both-or-neither is required and supplying old plus canonical names fails 400. On provider-insights they are independently optional legacy bounds and `from/to` fail as unknown: accept `YYYY-MM-DD` or RFC 3339 with explicit offset and 0–7 fractional digits; date-only start means inclusive `00:00:00Z`, date-only end means inclusive through that UTC date, timestamp end is inclusive at its represented 100 ns tick. When both exist, start must be <= end and span <=366 days. |
| Owner service `includeInactive` | Optional exact lowercase query literal `true|false`, default `true`; no numeric/case coercion. |

All query endpoints reject unknown or repeated query keys. Boundary tests cover omitted/null/blank, minimum-1/minimum/maximum/maximum+1, normalization equivalence, duplicate collection entries, malformed UTF-8/JSON, unknown properties, and decimal precision. Model binding MUST NOT return framework-specific bodies.

### 5.4 Exact authentication and recovery abuse controls

These controls apply before password verification, capability lookup, recovery dispatch, or account-existence-dependent work. They are backend controls shared by every API instance; CORS, the frontend, and process-local counters do not satisfy them. A **device** dimension exists only when the request carries a valid server-signed, Secure, HttpOnly, SameSite=Lax abuse cookie containing at least 128 random bits. Missing, invalid, expired, or privacy-blocked cookies cause the device dimension to be **omitted**, never mapped to a shared fallback bucket; IP and syntactically valid account/capability dimensions remain mandatory. Device identity is defense in depth and never replaces them. No new bootstrap route is required or implied by this contract. **IP** is the normalized direct peer address unless, and only unless, that peer belongs to the deployment’s explicit CIDR allow-list of trusted reverse proxies; then the rightmost untrusted syntactically valid address from `Forwarded`/`X-Forwarded-For` is used. Private/reserved/invalid forwarded values and headers from untrusted peers are ignored. IPv4 keys use /32 and IPv6 keys use /64. The allow-list is deployment configuration covered by startup and spoofing tests.

Every applicable dimension uses `base64url(HMAC-SHA-256(abusePepper, UTF8(routeFamily + "\u0000" + dimension + "\u0000" + normalizedDimensionValue)))`. The IP value is the canonical /32 or /64 prefix and a present device value is the verified random identifier. For syntactically valid email input, the account value is `EmailNormalized`; malformed/missing email omits the account dimension and is still constrained by IP and any verified-device bucket rather than a globally shared invalid-input key. Validate/reset capability buckets use the lowercase base64url SHA-256 digest of the exact non-empty received UTF-8 token string (including malformed format), never plaintext; missing/empty input omits that dimension and remains IP/device limited. This abuse digest is deliberately independent of §9’s decoded-token capability lookup. The pepper is external, independently rotatable, and distinct from JWT/reset peppers. Raw email, IP, device ID, token, and bucket key MUST NOT enter logs, metrics labels, or responses.

| Route | IP bucket | Device bucket | Account/capability bucket |
|---|---:|---:|---:|
| `POST /auth/register` | 10 / 15 min | 5 / 15 min when verified | 3 / 60 min normalized-email hash when syntactically valid |
| `POST /auth/login` | 30 / 5 min | 20 / 5 min when verified | 10 / 15 min normalized-email hash when syntactically valid |
| `POST /auth/forgot-password` | 10 / 15 min | 5 / 15 min when verified | 3 / 60 min normalized-email hash when syntactically valid |
| `POST /auth/validate-reset-token` | 30 / 5 min | 20 / 5 min when verified | 10 / 15 min capability hash when non-empty |
| `POST /auth/reset-password` | 10 / 15 min | 5 / 15 min when verified | 5 / 30 min capability hash when non-empty |

Each row means the IP bucket and every other **applicable** independent bucket must have capacity: verified device only when present, normalized-account only when syntactically valid, and non-empty capability only when supplied. Applicability depends solely on syntax/verified cookie state, never account/capability existence. Windows are fixed UTC intervals aligned to their duration. One atomic conditional increment per applicable bucket is performed in shared DynamoDB table `auth_abuse_counters`, string PK `BucketKey`, with `Count`, `WindowStartedAtUtc`, `WindowEndsAtUtc`, `ExpiresAtEpochSeconds` (TTL cleanup only), and `SchemaVersion=1`; key format is `ABUSE#v1#<routeFamily>#<dimension>#<windowStartEpoch>#<digest>`. A request consumes applicable capacity even when credentials/account/capability are unknown or invalid. Evaluate all applicable dimensions and return the most restrictive exhausted result; implementations MUST NOT skip a syntactically applicable account/capability bucket based on existence. Atomic conditional writes, not read-then-write, enforce the limit across instances. Partial bucket charging caused by a later dimension failure is permitted and deliberately fail-safe; counters are not business transactions.

An exhausted bucket returns HTTP `429`, `Content-Type: application/problem+json`, auth/recovery no-store headers, and `Retry-After: <whole seconds>` equal to the ceiling until the latest-ending exhausted window, clamped to `1..3600`. Body is exactly `{"type":"https://bookspot.example/problems/request-rate-limited","title":"Request rate limited","status":429,"detail":"Too many requests. Try again later.","code":"request_rate_limited"}`; no account, dimension, or capability truth is exposed. Register and forgot-password use this same tuple for known and unknown accounts. If the shared counter store is unavailable, indeterminate, or throttled, all five routes fail closed with generic `503 persistence_unavailable`, no credential/capability/account operation or mail occurs, and `Retry-After: 5`; SDK/DynamoDB throttling is never mapped to 429. Counters are retained until window end plus 24 hours, access is restricted to the auth service, and no analytics export contains bucket digests.

Deterministic-clock tests cover each limit-1/limit/limit+1 boundary, UTC rollover, exact `Retry-After`, malformed/spoofed forwarding headers, trusted-proxy chains, absent/invalid/rotated device cookie, normalization-equivalent email sharing, token-digest sharing, unknown-account parity, counter TTL not authorizing early reuse, atomic N-way contention, two API instances sharing one limit, partial charging, store failure, and strict separation of 429 abuse exhaustion from 503 persistence throttling. A distributed negative test exhausts every cookie-less bucket available to attacker IP/account A and proves cookie-less client B on a different IP/account is unaffected; malformed/missing account or token inputs likewise cannot create a global shared bucket.

## 6. Exact schemas

```ts
type UserType = 'client' | 'provider';
type BookingStatus = 'pending' | 'confirmed' | 'declined' | 'cancelled' | 'completed' | 'no_show';
type BookingAction = 'confirm' | 'decline' | 'cancel' | 'complete' | 'mark_no_show' | 'reschedule';
type DayOfWeek = 'monday' | 'tuesday' | 'wednesday' | 'thursday' | 'friday' | 'saturday' | 'sunday';
type Money = { amount: number; currency: 'ZAR' };

interface RegisterRequest {
  email: string;
  fullName: string;
  contactNumber?: string | null;
  password: string;
  userType: UserType;
}
interface LoginRequest { email: string; password: string; }
interface ForgotPasswordRequest { email: string; }
interface ValidateResetTokenRequest { token: string; }
interface ResetPasswordRequest { token: string; newPassword: string; }
interface ForgotPasswordSuccessResponse {
  message: 'If an account matches, password reset instructions will be sent.';
  success: true;
}
interface ResetPasswordSuccessResponse {
  message: 'Password reset completed.';
  success: true;
}
interface ResetTokenValidityResponse { valid: true; }

interface ProfileDto {
  profileId: string;
  email: string;
  fullName: string;
  contactNumber: string | null;
  userType: UserType;
  createdAt: string;
}
interface UpdateMyProfileRequest { fullName?: string; contactNumber?: string | null; }

interface AuthResponse {
  accessToken: string;
  tokenType: 'Bearer';
  expiresAt: string;
  profile: ProfileDto;
  // Response-only compatibility aliases for one measured migration window:
  token: string; // required during AUTH-ALIAS-1; absent only after its removal gate
  userId: string;
  email: string;
  fullName: string;
  contactNumber: string | null;
  userType: UserType;
}

interface BusinessDto {
  businessId: string;
  providerProfileId: string;
  businessName: string;
  description: string;
  address: string;
  city: string;
  phone: string;
  email: string;
  website: string | null;
  imageUrl: string | null;
  isActive: boolean;
  rating: number;
  reviewCount: number;
  timeZone: string;
  createdAt: string;
}
interface CreateBusinessRequest {
  businessName: string; description: string; address: string; city: string;
  phone: string; email: string; website?: string | null; imageUrl?: string | null;
  isActive?: boolean;
}
type UpdateBusinessRequest = Partial<CreateBusinessRequest>;

interface ServiceDto {
  serviceId: string;
  businessId: string;
  providerProfileId: string;
  providerDisplayName: string;
  name: string;
  description: string;
  category: string | null;
  price: Money;
  durationMinutes: number;
  imageUrl: string | null;
  tags: string[];
  location: string | null;
  isActive: boolean;
  createdAt: string;
}
interface CreateServiceRequest {
  businessId: string; name: string; description: string; category?: string | null;
  priceAmount: number; durationMinutes: number; imageUrl?: string | null;
  tags?: string[]; location?: string | null; isActive?: boolean;
}
type UpdateServiceRequest = Partial<Omit<CreateServiceRequest, 'businessId'>>;
interface ServiceSearchResponse {
  items: ServiceDto[]; page: number; pageSize: number; totalCount: number;
}

interface BusinessHoursDayDto {
  dayOfWeek: DayOfWeek;
  isClosed: boolean;
  openTime: string | null;
  closeTime: string | null;
}
interface ReplaceBusinessHoursRequest { timeZone: string; days: BusinessHoursDayDto[]; }
interface BusinessHoursDto { businessId: string; timeZone: string; days: BusinessHoursDayDto[]; }
interface AvailabilitySlotDto { startTime: string; endTime: string; }
interface ServiceAvailabilityDto {
  serviceId: string; businessId: string; timeZone: string;
  from: string; to: string; durationMinutes: number; slots: AvailabilitySlotDto[];
}

interface CreateBookingRequest { serviceId: string; startTime: string; }
interface BookingActionRequest {
  action: BookingAction;
  expectedVersion: number;
  startTime?: string; // required only for reschedule; forbidden otherwise
}
interface BookingBaseDto {
  bookingId: string;
  serviceId: string;
  businessId: string;
  providerProfileId: string;
  status: BookingStatus;
  startTime: string;
  endTime: string;
  price: Money;
  version: number;
  createdAt: string;
  updatedAt: string;
  service: { name: string; durationMinutes: number };
  business: { businessName: string; address: string; city: string };
}
interface ClientBookingDto extends BookingBaseDto {
  view: 'client';
  clientProfileId: string; // always the authenticated subject
  // no client object: the caller already owns that PII
}
interface ProviderBookingDto extends BookingBaseDto {
  view: 'provider';
  client: { fullName: string; email: string; contactNumber: string | null };
  // clientProfileId is deliberately omitted from provider responses
}
type BookingDto = ClientBookingDto | ProviderBookingDto;
interface BookingMutationResultDto {
  view: 'client' | 'provider';
  bookingId: string;
  status: BookingStatus;
  startTime: string;
  endTime: string;
  version: number;
  updatedAt: string;
  // Bounded mutation/replay projection: no party/profile/business/service IDs,
  // contact data, business address/location, presentation fields, or free text.
}
interface BookingPageDto { items: BookingDto[]; nextCursor: string | null; }

interface CreateReviewRequest { bookingId: string; rating: number; comment: string; }
interface UpdateReviewRequest { rating?: number; comment?: string; }
interface ReviewDto {
  reviewId: string; rating: number; comment: string;
  createdAt: string; updatedAt: string | null; displayName: string | null;
}
interface CityInfoDto {
  city: string; province: string; serviceCount: number; businessCount: number;
  providerCount: number; averageServicePrice: number; popularCategories: string[];
}

interface ProviderDashboardDto {
  kind: 'provider'; generatedAt: string; timeZone: string;
  todayBookings: number; weekBookings: number; pendingRequests: number;
  totalClients: number; activeServices: number; monthlyRevenue: Money;
  upcoming: BookingDto[];
  recentClients: Array<{clientProfileId:string; fullName:string; lastBookingAt:string; totalBookings:number}>;
}
interface ClientDashboardDto {
  kind: 'client'; generatedAt: string; totalBookings: number; completedBookings: number;
  cancelledBookings: number; pendingRequests: number; totalSpent: Money;
  upcoming: BookingDto[]; recent: BookingDto[];
}
type DashboardDto = ProviderDashboardDto | ClientDashboardDto;

interface ProviderInsightsCompatibilityDto {
  stats: {
    todayBookings: number; weekBookings: number; totalClients: number;
    monthlyRevenue: number; pendingBookings: number; confirmedBookings: number;
  };
  popularServices: Array<{
    serviceId: string; serviceName: string; bookings: number; revenue: number;
  }>;
}
```

Booking serialization is deterministic by authenticated party, never request-selected. Client and provider **read/list/dashboard** routes return `ClientBookingDto` and `ProviderBookingDto` respectively. A client read view never contains the nested `client` object; a provider read view never contains `clientProfileId`. The provider contact tuple is present only on authorized provider read/list/dashboard responses because it is operationally required for fulfilment, and only after Booking -> Service -> Business ownership succeeds. Every booking **create/action** returns the independent exact `BookingMutationResultDto`; it does not inherit `BookingBaseDto` and contains no party/profile/business/service IDs, contact data, business address/location, presentation fields, price, or free text. A client follows `GET /bookings/{id}` for its party-specific current view; a provider does likewise only when contact data is operationally needed. Durable replay and post-window behavior follow §§11.2–11.3. No public route returns any booking shape. Every private response is `no-store`. Snapshot tests assert exact property sets and recursively reject all unspecified fields.

`ProviderInsightsCompatibilityDto` is deliberately isolated from canonical `ProviderDashboardDto`. Its legacy numeric `monthlyRevenue`/`revenue` fields are decimal ZAR major units with at most two fractional digits; this exception exists only to preserve the current route’s response shape and MUST NOT spread to canonical DTOs.

Register/profile update/business/service/booking DTOs reject server-owned IDs, owner fields, role, hashes, ratings, status, derived times, price snapshots, versions, and timestamps. **Booking request compatibility is strict:** no temporary create/action request aliases exist. `providerName`, `clientName`, `clientEmail`, `clientPhone`, `endTime`, `businessId`, `providerProfileId`, `clientProfileId`, `status`, `price`, `priceAmount`, `durationMinutes`, `createdAt`, `updatedAt`, `version`, and any unknown property each produce `400 validation_failed`/`must_be_omitted`; none is ignored. Response-only aliases elsewhere do not authorize request overposting. This rule supersedes any source draft that said derived booking fields could be ignored.

## 7. Canonical route table

All routes require a valid session unless marked **Public**. `owner` and `party` are loaded server-side relationships.

| Method and route | Access | Request -> response | Success / principal errors |
|---|---|---|---|
| `POST /auth/register` | Public | `RegisterRequest -> AuthResponse` | Interim 201 + `Location: /profiles/me`; 400; 409; 429/503 per §5.4. Subject to HD-01 production gate. |
| `POST /auth/login` | Public | `LoginRequest -> AuthResponse` | 200; generic 401; 429/503 per §5.4. |
| `POST /auth/forgot-password` | Public | `ForgotPasswordRequest -> ForgotPasswordSuccessResponse` | Always identical 200 body `{"message":"If an account matches, password reset instructions will be sent.","success":true}`; 429/503 per §5.4. |
| `POST /auth/validate-reset-token` | Public | `ValidateResetTokenRequest -> ResetTokenValidityResponse` | 200 valid; exact 400 tuple in §9.1 for every unusable token; 429/503 per §5.4. `no-store`. |
| `POST /auth/reset-password` | Public | `ResetPasswordRequest -> ResetPasswordSuccessResponse` | 200 winner body `{"message":"Password reset completed.","success":true}`; exact 400 tuple in §9.1 for every unusable/race-losing token; 429/503 per §5.4. |
| `GET /profiles/me` | Client/Provider self | none -> `ProfileDto` | 200; 401/404. `no-store`. |
| `PATCH /profiles/me` | Client/Provider self | `UpdateMyProfileRequest -> ProfileDto` | 200; 400/401. |
| `DELETE /profiles/me` | Client/Provider self | none | 204; 401; 409 under HD-11. |
| `GET /businesses/{businessId}` | Public | none -> active `BusinessDto` | 200/404. |
| `GET /businesses/mine` | Provider | none -> `BusinessDto[]` | 200 empty array allowed; 401/403. |
| `POST /businesses` | Provider | `CreateBusinessRequest -> BusinessDto` | 201 + Location; 400/401/403. Owner from JWT. |
| `PATCH /businesses/{businessId}` | Provider owner | `UpdateBusinessRequest -> BusinessDto` | 200; 400/401/404 conceal. |
| `DELETE /businesses/{businessId}` | Provider owner | none | 204; 404 conceal; 409 dependencies; prefer deactivate. |
| `GET /services/{serviceId}` | Public | none -> active `ServiceDto` | 200/404. |
| `GET /services/search` | Public | `q/category/city/minPrice/maxPrice/minDuration/maxDuration/page/pageSize -> ServiceSearchResponse` | 200; exact §5.3 filters. Current `name` aliases `q` until sunset. |
| `GET /businesses/{businessId}/services` | Public | none -> `ServiceDto[]` | 200 empty allowed; 404 absent business. |
| `GET /businesses/{businessId}/services/manage` | Provider owner | `includeInactive=true|false` (default true) -> `ServiceDto[]` | 200 including inactive for owner; 401/404 conceal; no public cache. |
| `GET /services/{serviceId}/manage` | Provider owner | none -> `ServiceDto` | 200 whether active/inactive; 401/404 conceal; no public cache. |
| `POST /services` | Provider owner of body business | `CreateServiceRequest -> ServiceDto` | 201 + Location; 400/401/404 conceal. |
| `PATCH /services/{serviceId}` | Provider owner | `UpdateServiceRequest -> ServiceDto` | 200; 400/401/404 conceal. |
| `DELETE /services/{serviceId}` | Provider owner | none | 204; 404 conceal; preserve booking history/prefer deactivate. |
| `GET /businesses/{businessId}/hours` | Provider owner | none -> `BusinessHoursDto` | 200; 401/404 conceal. |
| `PUT /businesses/{businessId}/hours` | Provider owner | `ReplaceBusinessHoursRequest -> BusinessHoursDto` | 200; 400/401/404 conceal. |
| `GET /services/{serviceId}/availability` | Public | `from,to -> ServiceAvailabilityDto` | 200; 400 range/time; 404. |
| `GET /locations/cities` | Public | none -> `CityInfoDto[]` | 200; active public data only. |
| `GET /reviews/{reviewId}` | Public | none -> `ReviewDto` | 200/404; no booking/client identity. |
| `POST /reviews` | Client booking owner | `CreateReviewRequest -> ReviewDto` | 201; completed booking only; 409 one-per-booking. |
| `PATCH /reviews/{reviewId}` | Review author | `UpdateReviewRequest -> ReviewDto` | 200; 400/404 conceal. |
| `DELETE /reviews/{reviewId}` | Review author | none | 204; 404 conceal. |
| `POST /bookings` | Client only | `CreateBookingRequest -> BookingMutationResultDto` | 201 + Location; requires `Idempotency-Key`; 400/404/409/503. |
| `GET /bookings/{bookingId}` | Booking client/owning provider | none -> party-specific `BookingDto` | 200; non-party/absent identical 404. |
| `GET /bookings/client/me` | Client | filters/cursor -> `BookingPageDto` | 200; 400/401/403. |
| `GET /bookings/provider/me` | Provider | owned `businessId?`, filters/cursor -> `BookingPageDto` | 200; 400/401/404 conceal. |
| `POST /bookings/{bookingId}/actions` | Booking party + action role | `BookingActionRequest -> BookingMutationResultDto` | 200; requires `Idempotency-Key`; 400/403/404/409/503. |
| `GET /dashboard/me` | Client/Provider self | none -> `DashboardDto` | 200 real zero/empty allowed; 401/403. |

### 7.1 Legacy route disposition

- `GET/PUT/DELETE /profiles/{id}`: temporary self-only aliases requiring `id == sub`; return DTOs; cross-profile concealed 404. Remove `POST /profiles`.
- Current `PUT` business/service/review routes MAY alias safe PATCH logic; they MUST NOT bind persistence entities or move ownership.
- Current item-ID `/business-hours/**` routes are owner-checked compatibility adapters only; canonical frontend uses business schedule routes.
- `GET /bookings/client/{clientId}` and `/provider/{providerId}` are temporary aliases requiring path ID equal subject; provider query resolves owned businesses, not legacy `Booking.ProviderId`.
- `PUT /bookings/{id}` and `DELETE /bookings/{id}` MUST be unmapped (404/410) and MUST NOT reach old handlers. User deletion is `cancel` action.
- `GET /auth/validate-reset-token/{token}` MUST be unmapped; no redirect.
- Existing dashboard ID routes MAY remain only with subject equality and real data; migrate to `/dashboard/me`.
- `TestController` and Swagger/OpenAPI are unmapped in Production.
- Compatibility aliases carry deprecation telemetry and a dated removal criterion; aliases never weaken authorization.

The following dated matrix is the exhaustive disposition of the **43 current controller actions observed 2026-08-10**. “Alias” means the canonical DTO, authorization, active filtering, stable errors, `no-store` rules, and handler boundary above apply; it never means reuse of an unsafe current handler. Each alias emits `Deprecation: true`, a `Sunset` date of **2027-03-31**, and non-PII route/client-version counters. Removal requires the canonical frontend deployed, 30 consecutive production days with zero supported-client calls, contract tests proving replacement parity, and an announced release; otherwise the sunset moves. “Unmap” means 404 in Production and no handler invocation.

| Current route(s) | Classification and exact disposition |
|---|---|
| `POST /auth/login`, `POST /auth/forgot-password`, `POST /auth/reset-password` | Retain as canonical Public routes with exact DTOs, generic semantics, throttling, and auth/recovery `no-store`. |
| `GET /auth/validate-reset-token/{token}` | Unmap now (404); replacement is Public body-only `POST /auth/validate-reset-token`; never redirect or log path token. |
| `GET /profiles/me` | Retain private self route, exact `ProfileDto`, `no-store`. |
| `GET /profiles/{id}`, `PUT /profiles/{id}`, `DELETE /profiles/{id}` | Private self-only aliases; require `id==sub`; cross-profile/absent identical 404; PUT delegates to strict PATCH allow-list; delete delegates to self lifecycle. |
| `POST /profiles` | Unmap (404); only `POST /auth/register` creates accounts. |
| `GET /businesses/{id}` | Retain Public active-only `BusinessDto`; inactive is 404 except through owner operations. |
| `GET /businesses/{id}/services` | Retain Public active-business + active-service `ServiceDto[]`; absent/inactive business 404, no matching services 200 `[]`. |
| `GET /businesses/provider/{providerId}/services` | Public transitional browse alias; active business/service only, exact `ServiceDto[]`, no profile/contact join. Replacement is business/service search. Sunset gate above. |
| `POST /businesses` | Retain Provider canonical mutation; JWT owner, strict DTO. |
| `PUT /businesses/{id}`, `DELETE /businesses/{id}` | Private provider-owner; PUT is strict PATCH alias; delete follows dependency/deactivation rule; non-owner/absent identical 404. |
| `GET /services` | Public transitional list: active business/service only, exact `ServiceSearchResponse` with fixed `page=1,pageSize=100,totalCount`; no query parameters accepted. Replacement `GET /services/search`; sunset gate above. |
| `GET /services/search`, `GET /services/{id}` | Retain Public canonical search/detail, active records only and exact DTO/envelope. Inactive detail 404 to public/non-owner; owner uses `/services/{id}/manage`. |
| `POST /services` | Retain private provider-owner canonical mutation. |
| `PUT /services/{id}`, `DELETE /services/{id}` | Private provider-owner; PUT strict PATCH alias; inactive/historical service remains discoverable through owner manage routes; non-owner/absent identical 404. |
| `GET /business-hours/{id}`, `POST /business-hours`, `PUT /business-hours/{id}`, `DELETE /business-hours/{id}` | Private provider-owner compatibility adapters; resolve Hour -> Business -> owner; exact schedule validation; canonical replacement is business-scoped GET/PUT hours. |
| `GET /locations/cities` | Retain Public aggregate; active public data only, exact DTO, bounded result. |
| `GET /reviews/{id}` | Retain Public exact `ReviewDto`; no booking/client/contact identifiers. |
| `POST /reviews`, `PUT /reviews/{id}`, `DELETE /reviews/{id}` | Client booking-owner/author only; PUT strict PATCH alias; one-per-completed-booking and concealment rules apply. |
| `GET /bookings/{id}` | Retain private party-only exact party-specific shape. |
| `GET /bookings/client/{clientId}`, `GET /bookings/provider/{providerId}` | Private self-only aliases requiring path subject equality; provider resolution traverses owned businesses; exact page and party shapes. Replacements are `/client/me` and `/provider/me`. |
| `POST /bookings` | Retain canonical Client-only mutation with strict minimal body, idempotency, eligibility, and transaction contract. |
| `PUT /bookings/{id}`, `DELETE /bookings/{id}` | Unmap (404; 410 `legacy_route_removed` only during explicitly announced client migration). Neither may invoke old generic update/delete; replacement is action endpoint. |
| `GET /dashboard/provider/{providerId}/stats` | Private Provider self-only alias; exact `ProviderDashboardDto`, real owned-business data, `no-store`; replacement `/dashboard/me`. |
| `GET /dashboard/providers/{providerId}/insights` | Private Provider self-only legacy compatibility route; exact `ProviderInsightsCompatibilityDto` and range semantics below, `no-store`; replacement `/dashboard/me`. |
| `GET /dashboard/client/{clientId}/stats` | Private Client self-only alias; real exact client DTO, `no-store`; replacement `/dashboard/me`. |
| `GET /dashboard/clients` | Private Provider only; clients derived solely from bookings at caller-owned businesses; exact `recentClients` element shape, max 100 sorted by `lastBookingAt desc`, no arbitrary profile access, `no-store`; replacement provider `/dashboard/me.recentClients`. |
| `GET /dashboard/my-stats` | Private Client self only; exact `ClientDashboardDto`, real data/zero state, `no-store`; replacement `/dashboard/me`. |
| `GET /test/exception/{type}`, `GET /test/validation-details` | Unmap in Production (404). Development only with synthetic data and explicit environment guard. |

Owner manage routes are not public aliases: public `/services/{id}` and business collection remain active-only, while owner manage routes load Service -> Business -> owner and return inactive records. Non-owner and absent are byte-equivalent 404. This is the only supported path to discover/reactivate an inactive service.

For `GET /dashboard/providers/{providerId}/insights`, preserve the current independently optional, inclusive legacy query contract. Parse each supplied bound under the provider-insights row of §5.3 and canonicalize to UTC ticks. A date-only start becomes inclusive UTC midnight; a date-only end becomes the final 100 ns tick of that UTC date. A timestamp end is inclusive at its represented tick. Internally translate an end that is below `DateTime.MaxValue` to an exclusive bound by adding one 100 ns tick; compare directly at MaxValue. Start-only selects all booking starts `>= start`; end-only selects all `<= end`; neither selects all owned-business bookings; both require start <= end and at most 366 days. This legacy route intentionally does not impose the canonical default 30-day selection. Capture one `generatedAt` for relative metrics. All computation uses booking snapshots and businesses owned by the authenticated provider; current service prices never rewrite revenue. `todayBookings` counts selected starts on `generatedAt`’s `Africa/Johannesburg` local date; `weekBookings` counts selected starts in the intersection with the seven local calendar days ending at `generatedAt`; `monthlyRevenue` sums selected `completed` booking price snapshots in the intersection with `[generatedAt-30 days,generatedAt)`; `totalClients` is distinct selected clients; pending/confirmed counts are selected rows in those states. `popularServices` aggregates all selected bookings by snapshotted service ID/name, includes zero completed revenue, sorts by `bookings desc`, then `serviceId` ordinal ascending, and returns at most 10. Empty selected data returns zero stats and `[]`. The route accepts only `startDate/endDate`—not canonical `from/to`. Response snapshots cover neither bound, start-only, end-only, date-only midnight expansion, timestamp exact-end inclusion, both-bound maximum/range rejection, ordering/cap, and empty state until the dated alias removal gate; it MUST NOT be described or implemented as `ProviderDashboardDto`.

## 8. Authentication, ownership, and authorization matrix

| Resource/operation | Anonymous | Client | Provider | Authoritative check |
|---|---:|---:|---:|---|
| Public active businesses/services/search/availability/cities/reviews | allow | allow | allow | DTO/filter only active public records. |
| Auth register/login/recovery | allow | allow | allow | Rate limit; no privilege from existing token. |
| Self profile | deny | self | self | `JWT.sub == Profile.Id`. |
| Business create | deny | deny | allow | Owner set to JWT subject. |
| Business update/delete/hours | deny | deny | owner | Load Business; `Business.ProviderId == sub`. |
| Service create | deny | deny | owner | Load body Business; compare `Business.ProviderId`. |
| Service update/delete | deny | deny | owner | Service -> Business -> owner. |
| Booking create | deny | allow | deny | Client subject becomes `clientProfileId`. |
| Booking read/list | deny | own only | own-business only | Booking client equality or Booking -> Service -> Business owner. |
| Booking confirm/decline/complete/no-show | deny | deny | owning provider | Party check before state disclosure. |
| Booking cancel/reschedule | deny | booking client | owning provider | Party resolver + expected version. |
| Review create | deny | completed-booking client | deny | Booking client + completed + atomic one-per-booking. |
| Review update/delete | deny | author | deny | Review -> Booking -> client. |
| Dashboard | deny | self | owned-business scope | Self route and role-selected query. |
| Diagnostics/Swagger in Production | deny | deny | deny | Route not mapped. |

Known wrong-role capability returns 403. Direct absent/non-owner/non-party object returns identical concealed 404. Ownership is checked in application/domain handlers as well as endpoint policy; no security boundary relies on frontend enforcement.

## 9. Password recovery and session contract

1. Normalize email, enqueue one account-agnostic recovery request, and return the same generic 200 body and release schedule for known and unknown accounts.
2. Generate 32 random bytes with a CSPRNG; base64url without padding. Store only SHA-256/HMAC digest, account, generation, issue/expiry, and consumed/revoked state outside the envelope-encrypted delivery outbox.
3. New issue atomically supersedes earlier outstanding tokens. Expiry is 30 minutes by server UTC; DynamoDB TTL is cleanup only.
4. Email only the registered address. Preferred SPA link carries token in URL fragment, immediately clears history, loads no third-party resources, and sets `Referrer-Policy: no-referrer`.
5. Validation POST hashes/looks up token, is read-only, never extends expiry, and returns `no-store`.
6. Reset POST requires a reset-scoped `Idempotency-Key`, atomically conditions on latest generation, unexpired and unused; writes a hash from the common BCrypt service, consumes token, records the keyed operation binding/outcome, increments security/session version, and invalidates all reset capabilities.
7. Exactly one of concurrent resets succeeds. Old password and stale sessions fail afterward; new password logs in.
8. Unknown/malformed/superseded/expired/used/race-losing tokens return the same safe body. No path/query API token and no token/password logging.
9. Atomically record a redacted audit event and durable confirmation-delivery item with the successful reset; notification dispatch occurs from that committed item and is not part of the HTTP success boundary.

The two success schemas are distinct literal contracts, not a shared free-string response. `ForgotPasswordSuccessResponse` always emits `{"message":"If an account matches, password reset instructions will be sent.","success":true}` for known and unknown accounts. A winning reset emits `ResetPasswordSuccessResponse` as `{"message":"Password reset completed.","success":true}`. Property order, casing, spacing (none outside string literals), status, content type, and §5 cache headers are snapshot-frozen; OpenAPI MUST expose each literal enum/const value and implementations MUST write these constants or use an equivalently byte-deterministic serializer.

**Forgot-password non-enumerating dispatch:** after validation and §5.4 charging, the HTTP handler does not look up identity claims. It creates a random 128-bit `RecoveryRequestId` and writes exactly one message for every syntactically valid known-or-unknown email to encrypted queue `auth_recovery_requests`: `{RecoveryRequestId, EmailCiphertext, SourceIpPrefixHmac, DeviceHmac|null, RequestedAtUtc, ExpiresAtUtc, SchemaVersion=1}`. `EmailCiphertext` is envelope-encrypted normalized email and is visible only to the recovery worker; no deterministic email digest is stored in the queue. The two provenance values are computed before enqueue with the audit-correlation pepper over the same canonical IP prefix and verified device identifier defined by §5.4; they are opaque, never used for identity lookup/routing/rate-limit decisions, and become the exact source values copied into a winning `RESET_CAPABILITY_ISSUED` audit item. Queue retention is 24 hours, dead-letter retention is 7 days, logs/metrics contain no message field, and enqueue is deduplicated by request ID only. Queue unavailable/throttled/indeterminate returns the same generic `503 persistence_unavailable` regardless of account existence and no identity lookup occurs.

The public response is released from a monotonic-clock gate at `acceptedAt + 400 ms + jitter`, where `jitter = HMAC-SHA-256(timingPepper, RecoveryRequestId)[0] mod 51` milliseconds. It is never released earlier; successful enqueue normally returns at that gate. If enqueue has not resolved by 450 ms, return the generic 503 at the same 450 ms gate and let queue idempotency resolve/cancel the request without account lookup. The timing pepper is external and rotated independently; jitter is deterministic per request, not account-dependent. No mail, capability, identity, or audit work occurs on the HTTP request path.

The worker consumes the same queue shape for every request, decrypts and re-normalizes the email, derives the exact §9.2 `ClaimKey`, performs one strongly consistent base-table claim read and referenced Profile read, and acknowledges only after bounded processing. No recovery GSI or alternate identity index exists. For a known account it pre-generates the token and encrypted delivery payload, then runs the four-action issue transaction below, copying the queued source hashes into the issue audit item and atomically putting the delivery item; it acknowledges the request only after resolving that transaction. For an unknown/broken claim it performs the same HMAC/token-generation/envelope-serialization work in memory, writes no capability/audit/delivery item, and acknowledges. Worker retries/DLQ behavior is private and cannot alter the already released public tuple. Only the §9.1 delivery worker may decrypt and dispatch committed delivery items; there is no post-commit enqueue gap or reconstruct-from-digest path. Access to queues/DLQs is restricted, encryption at rest/in transit is mandatory, and expired requests are discarded without identity-dependent public effects.

### 9.1 Additive reset-capability persistence and exact failure tuple

Create additive DynamoDB table `auth_capabilities` with string partition key `CapabilityKey`; no plaintext-token key or GSI is permitted. Items are:

```text
CapabilityKey = RESET#DIGEST#v1#<base64url(HMAC-SHA-256(serverPepper, rawToken))>
Kind = RESET_CAPABILITY
ProfileId, Generation (number), IssuedAtUtc, ExpiresAtUtc, ConsumedAtUtc|null, RevokedAtUtc|null
ConsumeOperationBinding|null; ConsumeHttpStatusCode|null; ConsumeOutcomeBodyBase64|null
ExpiresAtEpochSeconds (TTL cleanup only), SchemaVersion = 1

CapabilityKey = RESET#ACCOUNT#v1#<b64url(profileId)>
Kind = RESET_ACCOUNT_STATE
ProfileId, LatestGeneration (number), UpdatedAtUtc, SchemaVersion = 1
```

The HMAC pepper is an external rotatable secret distinct from JWT/password keys. A raw token is exactly 32 random bytes. The digest lookup is a base-table strongly consistent `GetItem`; comparisons are constant-time where bytes are compared. The profile row adds numeric `SecurityVersion` (default/backfill 1) and optional `ResetGeneration` only if duplicating the account-state generation simplifies transaction conditions; account-state item remains authoritative.

Create additive DynamoDB table `auth_audit` with string partition key `AuditKey`, no sort key, no GSI, and no TTL:

```text
AuditKey = AUTH#v1#<eventType>#<base64url(profileId)>#<generation>#<eventId>
Kind = AUTH_SECURITY_EVENT; SchemaVersion = 1
EventType = RESET_CAPABILITY_ISSUED | RESET_DELIVERY_REVOKED | PASSWORD_RESET_COMPLETED
EventId (128-bit-or-more server random); ProfileId; CapabilityGeneration
OccurredAtUtc; SecurityVersionBefore|null; SecurityVersionAfter|null
SourceIpPrefixHmac; DeviceHmac|null; TraceIdHash|null
```

The audit item contains no raw/full IP, email, phone, password/hash, token/digest, JWT, user-agent, request body, capability key, or free-form text. Source fields are independently keyed hashes suitable only for restricted abuse correlation. Access is restricted to the auth service for writes and named security/audit roles for reads; application support and clients have no read path. Records are immutable and retained seven years, subject to legal hold; deletion requires a separately approved retention job and produces only aggregate reconciliation evidence. `attribute_not_exists(AuditKey)` is mandatory. Event ID is fixed before transaction submission and reused only while resolving that transaction’s indeterminate result.

Create additive DynamoDB table `auth_delivery_outbox` with string partition key `DeliveryKey`, no sort key, DynamoDB Streams `NEW_AND_OLD_IMAGES`, and `AuthDeliveryDueIndex` whose partition key is `QueueShard` and sort key is `NextAttemptKey`. It is the sole durable handoff for reset issue and completion mail; stream consumers and retry queues carry `DeliveryKey` only and are hints, never delivery authority:

```text
DeliveryKey = AUTHMAIL#v1#<eventType>#<base64url(profileId)>#<generation>#<eventId>
Kind = AUTH_MAIL_DELIVERY; SchemaVersion = 1
EventType = RESET_CAPABILITY_DELIVERY | PASSWORD_RESET_CONFIRMATION
ProfileId; CapabilityGeneration; AuditKey; CreatedAtUtc; NotBeforeUtc; DeliveryDeadlineUtc
State = PENDING | LEASED | SENT | CANCELLED | DEAD
AttemptCount; LeaseOwner|null; LeaseExpiresAtUtc|null; NextAttemptAtUtc; SentAtUtc|null
QueueShard = two lowercase hex characters from SHA-256(DeliveryKey)
NextAttemptKey = <UTC sortable NextAttemptAtUtc>#<DeliveryKey>
ProviderMessageId|null; LastFailureClass|null; TerminalAtUtc|null; CleanupAfterEpochSeconds|null
PayloadCiphertext; EncryptedDataKey; KmsKeyId; EncryptionContextVersion = 1
```

Before transaction submission, the auth worker obtains a fresh data key from the dedicated auth-mail KMS key and uses AES-256-GCM with authenticated context `{DeliveryKey,EventType,ProfileId,CapabilityGeneration,SchemaVersion}`. For `RESET_CAPABILITY_DELIVERY`, the encrypted payload contains exactly the raw token, normalized registered destination, reset-link template/version and public expiry; for `PASSWORD_RESET_CONFIRMATION`, it contains the destination and confirmation template/version but no token, password, hash, IP, or session value. Plaintext data keys and payloads exist only in bounded worker memory and are zeroed/released after use; neither appears in logs, traces, metrics, exceptions, backups, DLQs, provider metadata, or audit. Only the issue/consume transaction role may encrypt/put, and only the isolated delivery-worker role may read/decrypt/update; support, API request handlers, clients, and general application roles have no decrypt or table-read permission. KMS grants require the exact encryption-context keys and table/service identity. DynamoDB encryption and point-in-time recovery remain enabled, but they do not replace payload envelope encryption.

**Issue transaction:** in the asynchronous recovery worker, a known account issues generation `g=LatestGeneration+1` in one four-action `TransactWriteItems`: conditionally update/create account-state from expected generation to `g`; put the digest capability with `attribute_not_exists(CapabilityKey)` and 30-minute expiry; put exactly one `RESET_CAPABILITY_ISSUED` auth-audit item conditioned absent; and put the matching `RESET_CAPABILITY_DELIVERY` item in `PENDING` state conditioned on `attribute_not_exists(DeliveryKey)`. The request message is acknowledged only after an indeterminate result is resolved by strongly reading all four deterministic keys. A concurrent issuer that loses commits zero capability/account-state/audit/delivery actions, discards its plaintext/ciphertext, rereads/retries with a new token/generation/event ID, and only its eventual committed winner has one audit and one delivery item; an abandoned/exhausted loser has no durable auth artifact and increments only non-PII contention telemetry. Only the greatest committed generation validates. Older capability rows need not be rewritten because generation comparison supersedes them. Unknown/broken-claim requests take the worker path defined above and create no durable capability, auth audit, delivery item, or mail.

The delivery worker receives `DeliveryKey` from the table stream/retry queue or queries all 256 due-index shards up to `now`; it strongly reads the base item and claims it with a conditional update from due `PENDING`, or from an expired `LEASED`, to `LEASED`, setting a random owner, a 60-second lease and incremented attempt count. Before every reset-token send it strongly reads account state and capability and sends only when the item generation is still latest, the digest capability is unconsumed/unrevoked, and `now < min(ExpiresAtUtc,DeliveryDeadlineUtc)`; otherwise it conditionally marks the delivery `CANCELLED` and erases ciphertext/key fields. It decrypts only after this check and submits `DeliveryKey` as both the deterministic provider idempotency/custom message header and an authenticated provider event tag. Synchronous provider rejection while the lease is held follows the retry/dead rules below. Provider acceptance is followed by a lease-owner-conditioned `SENT` update that persists `ProviderMessageId`, erases ciphertext/key fields, clears lease fields, and sets terminal metadata. Because SMTP/SES acceptance and DynamoDB acknowledgement cannot be one transaction, delivery is at-least-once: an ack loss may duplicate the same template/link, but retries cannot create a different capability, and all reset links remain governed by latest-generation validation. Mail wording MUST say the link may have been superseded and MUST NOT assert that it remains valid.

Transient failures set `NextAttemptAtUtc = now + min(5 seconds * 2^(AttemptCount-1), 5 minutes) + (HMAC-SHA-256(deliveryTimingPepper, DeliveryKey || AttemptCount)[0..1] mod 1001) milliseconds`, return state to `PENDING`, and retry only while the next attempt precedes `DeliveryDeadlineUtc`. Reset delivery deadline equals capability expiry; confirmation deadline is completion time + seven days. Synchronous hard rejection, reset attempt 10, or reset deadline invokes one revocation transaction conditioned on the live delivery lease, current account generation and unconsumed/unrevoked capability: advance account generation to `g+1`, set capability `RevokedAtUtc`, mark delivery `DEAD` while erasing ciphertext/key fields, and put one `RESET_DELIVERY_REVOKED` audit item.

Asynchronous provider bounce/complaint events are accepted only through the provider-authenticated webhook/subscription after signature, timestamp and configured-account/topic checks. The handler extracts the authenticated `DeliveryKey` tag and `ProviderMessageId`, strongly reads that exact item, and deduplicates by conditional terminal state/event identity; it never scans or trusts an address/token supplied by the event. For a `SENT` reset delivery whose provider ID matches, it may execute the same four-action revoke/dead/audit transaction **without a live lease**, conditioned instead on `State=SENT`, matching `ProviderMessageId`, current account generation and unconsumed/unrevoked capability. Duplicate events observe `DEAD` and succeed idempotently; a delayed event for a superseded, consumed or already revoked capability may mark only delivery metadata `DEAD` and cannot advance generation or add a revocation audit. Bounce-vs-consume transactions contend on capability/account state, so exactly one security transition wins. A bounced confirmation becomes `DEAD` and pages operations but never rolls back reset.

Thus every committed issue is either provider-accepted before deadline or atomically made invalid; a later authenticated hard bounce also invalidates an otherwise-current unused capability. No uncommitted token can be sent. A superseding issue may leave an older item pending, but the pre-send generation check cancels it and any already accepted older mail contains a token that validation rejects. Confirmation items use the same schedule for at most 20 attempts/seven days; permanent failure marks `DEAD`, erases ciphertext/key fields and pages operations but never rolls back the completed password reset. Terminal items retain non-payload delivery metadata for 30 days, then TTL cleanup may remove them; `PENDING/LEASED` items have no cleanup TTL. A reconciler runs at least every five minutes, queries every due-index shard, reclaims expired leases, finds deadline breaches, performs the same conditional revoke/dead transition, and alarms on any unresolved, orphan, stream-lagged, retry-queue-lost, or unmatched provider-event state. DLQs contain `DeliveryKey` plus non-secret provider event ID only, never payload/token/destination.

**Validate:** digest the syntactically untrusted token, strongly read capability and account state, and return success only if capability exists, generation equals latest, `ConsumedAtUtc` and `RevokedAtUtc` are absent, and `now < ExpiresAtUtc`. It never mutates or extends expiry.

**Consume transaction:** `Idempotency-Key` is mandatory, 1–200 printable ASCII characters with at least 128 bits of client randomness, and is never logged. Before submission derive `ConsumeOperationBinding=base64url(HMAC-SHA-256(resetIdempotencyPepper, raw Idempotency-Key || 0x00 || capability digest))`; deterministically derive completion `AuditKey` and confirmation `DeliveryKey` from that binding plus generation, and precompute BCrypt, exact success bytes, and the encrypted confirmation payload. Then one five-action `TransactWriteItems` conditionally (a) updates the Profile password hash and increments `SecurityVersion` exactly once from the version read, (b) updates capability `ConsumedAtUtc`, operation binding, exact status `200`, and exact success body only when unconsumed/unexpired/unrevoked/generation matches, (c) advances account-state generation to `g+1` conditioned on `LatestGeneration=g`, invalidating every capability, (d) puts the deterministic `PASSWORD_RESET_COMPLETED` auth-audit item conditioned absent, and (e) puts the deterministic `PASSWORD_RESET_CONFIRMATION` outbox item in `PENDING` conditioned absent. If any condition loses, no password/session/audit/capability/delivery state changes and the precomputed plaintext/ciphertext is discarded: the winning consume has exactly one durable audit event and confirmation handoff and every race loser has zero, with only aggregate non-PII invalid-token/contention telemetry. Cache invalidation occurs synchronously before returning success. Transaction outcome, not a preflight read, decides success; confirmation dispatch is asynchronous and cannot change the successful reset response.

On timeout, connection loss, process restart, or any indeterminate consume result, the handler performs bounded strongly consistent reads of the capability plus deterministic audit/delivery keys. If the capability is consumed with a constant-time-equal operation binding and both deterministic children exist, it completes cache invalidation if necessary and replays the stored exact 200 tuple with `Idempotent-Replayed: true`; it never rehashes or mutates the password. A different/missing key, different binding, partial/impossible child set, or an ordinary later reuse receives the same generic invalid-token tuple below (an impossible partial set additionally pages integrity operations). Reads retry with exponential backoff only until the request deadline; unresolved absence returns generic `503 persistence_unavailable` and instructs retry with the same token/key. The stored binding/outcome is access-restricted, removed with capability TTL, and cannot be used to recover the raw key, token, or password.

Except for a proven same-operation consume replay above, for **both** validate and consume, unknown, empty, malformed, superseded, expired, used, revoked, digest-pepper mismatch, different/missing idempotency key, and race-losing tokens return this byte-equivalent tuple (apart from `traceId` and `instance`, which MUST be omitted here to preserve byte equivalence): HTTP `400`; `Content-Type: application/problem+json`; `Cache-Control: no-store, max-age=0`; `Pragma: no-cache`; no `Retry-After`, `WWW-Authenticate`, `Set-Cookie`, `ETag`, or lifecycle-dependent header; body exactly `{"type":"https://bookspot.example/problems/reset-token-invalid","title":"Reset token invalid","status":400,"detail":"The reset capability is invalid.","code":"reset_token_invalid"}` encoded UTF-8 without insignificant-property variation. Response length and timing are padded to the same configured bucket. Only a valid validate returns `200 {"valid":true}`; only a winning consume returns the exact 200 `ResetPasswordSuccessResponse`.

**Legacy migration:** provision `auth_capabilities`, `auth_audit`, `auth_delivery_outbox`, `auth_abuse_counters`, encrypted `auth_recovery_requests`, its DLQ, recovery/delivery workers, DeliveryKey-only DLQ, KMS key/grants, reconciler and alarms in deployment and local integration infrastructure before enabling new auth writers; verify exact keys/message schemas, queue/outbox retention, payload encryption context, TTL policy (capability/counter and terminal delivery cleanup only; no auth-audit TTL), least-privilege IAM, encryption at rest/in transit, point-in-time recovery where applicable, and alarms. Deploy dual-read/no-legacy-write code, create/backfill Profile security versions and account-state generations, then invalidate rather than copy every plaintext-keyed legacy reset row. During a maximum 30-minute drain, legacy tokens are uniformly treated as invalid and users request a new token; no plaintext token is moved into the new table. Disable/delete the old writer first, verify zero new old-table writes, wait maximum legacy expiry + safety margin, revoke table access, export only non-secret migration counts, then delete the old table under a separately approved retention task. Reconciliation asserts every committed reset generation has exactly one issue audit and one delivery item, every successful security-version reset increment has exactly one completion audit and confirmation item, every terminal delivery has payload/key fields erased, every permanently failed current capability has one revocation audit, no losing/invalid/unknown attempt has a durable artifact, and no audit/delivery exists without corresponding committed state. Tests prove byte-equivalent public tuples, identical known/unknown request shape, no synchronous identity lookup, digest-only capability storage outside the envelope-encrypted outbox, latest-generation supersession, issue/consume/revoke races, transaction rollback at every action, exact four/five-action issue/consume counts, one password/version/audit mutation, audit/outbox redaction/retention/access, and no legacy plaintext capability in logs/backups/new storage.

### 9.2 Atomic normalized-email identity claim

Create additive DynamoDB table `identity_claims` with string PK `ClaimKey` and item shape:

```text
ClaimKey = EMAIL#v1#<base64url(UTF8(EmailNormalized))>
Kind = NORMALIZED_EMAIL
ProfileId, EmailNormalized, CreatedAtUtc, SchemaVersion = 1
```

Registration generates the profile ID first and executes one `TransactWriteItems`: conditional Put claim with `attribute_not_exists(ClaimKey)`; conditional Put profile with `attribute_not_exists(Id)`, display email, identical `EmailNormalized`, BCrypt hash, immutable role, `SecurityVersion=1`, and timestamps; optional registration audit Put. The API returns success only after all items commit. A claim collision maps to HD-01’s selected external behavior: acknowledged immediate registration -> 409 `email_already_registered`; enumeration-resistant flow -> the same generic 202 as a new request. Both outcomes still create at most one profile. Any timeout/indeterminate result is resolved by strongly reading the claim and profile; it never falls back to scan/check-then-save.

Login normalizes once on its synchronous request path, strongly reads `identity_claims[ClaimKey]`, then strongly reads the referenced Profile. Forgot-password performs no synchronous identity read: only the §9 asynchronous worker decrypts/normalizes the queued email and performs those same two strong reads after the public response is decoupled. For either flow, a missing/broken link has the same external result as unknown credentials/account and raises protected integrity telemetry. No GSI or eventually consistent scan is an identity authority. Account deletion follows interim HD-11: refuse deletion when required history exists; if deletion is allowed, atomically tombstone the profile and increment security version while **retaining the claim permanently reserved**. This is not deferred to a frontend or later policy choice.

Migration freezes registration/email mutation, exports profiles, computes v1 normalization, and emits a deterministic collision/invalid-email report without auto-selecting a winner. Humans repair every collision; unresolved count must be zero. Then transactionally create each claim conditioned absent while adding/validating Profile `EmailNormalized` and `SecurityVersion`; verify one claim per profile and one profile per claim; deploy claim-authoritative readers/writers; lift freeze; retain rollback data. Fault injection at each transaction action proves no claim without profile and no profile without claim. Concurrent case/space/NFC-equivalent registration tests use a barrier and assert one committed pair and the exact HD-01 response behavior.

## 10. Booking state transitions

| Source | Action | Target | Actor | Atomic effects/invariants |
|---|---|---|---|---|
| none | create | `pending` | client | Derive all IDs/end/snapshots; create booking, request record, cells. |
| `pending` | confirm | `confirmed` | owning provider | Times/cells retained; slot status updated. |
| `pending` | decline | `declined` | owning provider | Terminal; release all cells. |
| `pending` | cancel | `cancelled` | client or owning provider | Require server admission time `actionNow < startTime`; terminal; release all cells. |
| `confirmed` | cancel | `cancelled` | client or owning provider | Require server admission time `actionNow < startTime`; terminal; release all cells. |
| `confirmed` | complete | `completed` | owning provider | Require `endTime <= now`; terminal; release cells. |
| `confirmed` | mark_no_show | `no_show` | owning provider | Require `startTime <= now`; terminal; release cells. |
| `pending` | reschedule | `pending` | client or owning provider | New start only; derive end; atomic slot move. |
| `confirmed` | reschedule | `pending` | client or owning provider | Same; confirmation intentionally resets. |
| terminal | any | none | none | `declined/cancelled/completed/no_show` cannot transition; 409 for party. |

Every mutation requires positive `expectedVersion` except create, increments version exactly once, stamps server `updatedAt`, appends actor/action/from/to/time audit data, and preserves identities. Non-reschedule actions forbid `startTime` and preserve both times byte-for-byte. Ownership is evaluated before state details. All unlisted transitions are 409.

### 10.1 Create/reschedule eligibility and configuration serialization

Create and reschedule MUST satisfy all rules below at the booking transaction’s linearization point; availability preview is never authority:

1. The loaded Service exists, `isActive=true`, has valid 15-minute-aligned duration 15–480, belongs to the loaded Business, and the loaded Business exists and `isActive=true`. Missing/inactive public targets are concealed 404 `resource_not_found`; an authenticated owner using manage routes may observe inactive state, but booking mutation still returns 404 so clients cannot distinguish it.
2. Capture one server UTC `validationNow` immediately before constructing the transaction. Canonical `startTime > validationNow`; equality/past is `400 validation_failed`, `errors.startTime=["invalid_range"]`. Create/reschedule derives `endTime=startTime+Service.DurationMinutes`; overflow fails 400.
3. Convert every instant in `[start,end)` with the Business IANA timezone using the server-pinned TZDB version. The supplied RFC 3339 offset MUST equal that timezone’s actual offset at the supplied instant; mismatch is 400 `errors.startTime=["invalid_value"]`. The interval must map to one local calendar date and be fully contained in that day’s open half-open interval `[openTime,closeTime)`; closed/missing day or partial containment is 400 `errors.startTime=["invalid_range"]`. Overnight hours are not supported. A spring-forward nonexistent local start cannot have a matching zone offset and fails; either occurrence of a fall-back repeated time is accepted only with its explicit matching offset and maps to a distinct UTC slot key.
4. Capacity is free for all deterministic cells under `(businessId,"single")`; a conflicting pending/confirmed owner produces 409 `booking_slot_conflict` without rival identity.
5. Create snapshots service name, duration, price/currency, business presentation, canonical IDs, and the exact TZDB/timezone/config versions used. Reschedule preserves the original commercial/name/price snapshots and service/business identities, derives end from the **booking’s immutable duration snapshot**, and rechecks current Business/Service active state and current hours. Thus a later service duration/price edit never silently changes an existing booking’s duration/price.

Add numeric `ConfigVersion` (starts/backfills at 1) to each Business and Service. Store the whole-week business schedule as one business-keyed aggregate with `ScheduleVersion` and `TimeZone`; canonical hours replacement increments it once atomically. Any mutation of booking-relevant Business active/timezone fields increments Business.ConfigVersion; any mutation of Service business/activity/duration/price/name fields increments Service.ConfigVersion. These versions are integrity controls, not HTTP ETags.

After strongly consistent reads, create/reschedule includes three `ConditionCheck` actions inside the **same** `TransactWriteItems` as request/booking/slots/audit: Business ID/active/config-version unchanged; Service ID/business/active/config-version unchanged; schedule business/timezone/schedule-version unchanged. The persisted booking snapshot records observed config versions, but the idempotency fingerprint contains only canonical client intent as §11.3 defines. A concurrent deactivate, duration/price/name change, business timezone/activity change, or schedule replacement therefore orders before or after the booking transaction: if before, the booking rereads/revalidates; if after, the booking commits under the old snapshot and the configuration mutation occurs after it. A condition loser is retried internally from strongly consistent reads at most twice while the client request deadline remains; if newly ineligible return the stable 400/404 above, if still eligible rebuild the transaction only before any request record exists, and on repeated churn return 409 `booking_configuration_conflict`. Same-key replay after a committed request always returns its immutable original outcome and does not re-evaluate eligibility.

Configuration writers MUST use conditional version increments, so they serialize against booking condition checks. A business/service/schedule change does not retroactively cancel committed bookings; operators must use explicit booking actions. Migration backfills versions/schedule aggregates while booking writes are frozen, verifies every active service has one valid business/schedule chain, then enables version-conditioned writers together.

Mandatory tests cover inactive/missing Business and Service; past/equal-now; outside/partially outside/closed-day hours; boundary ending exactly at close; explicit offset mismatch; spring-forward gap; both fall-back occurrences producing distinct UTC cells; TZDB version fixture; concurrent business/service deactivation; concurrent duration/price/timezone/hours changes; repeated configuration churn; and proof that committed snapshots are immutable while a later reschedule uses the booking duration snapshot and current eligibility/hours.

## 11. Concurrency, idempotency, outcome, and audit mechanism

### 11.1 Atomic guarantee and linearization

For one `(businessId, resourceId)` and half-open interval, at most one live booking (`pending|confirmed`) owns each deterministic 15-minute cell. **Every** booking mutation—create, confirm, decline, cancel, complete, mark-no-show, and reschedule—has one and only one linearization point: successful commit of one DynamoDB `TransactWriteItems`. That transaction contains the booking effect, a conditional request/outcome record that is immutable during its 24-hour replay window and privacy-compacted afterward, all required capacity writes/deletes/status updates, and one immutable audit event; create/reschedule additionally contain the three configuration condition checks in §10.1. None can commit partially. Confirm and every terminal action are subject to the same rule; there is no non-idempotent action path.

Pre-validation/authorization failures occur before a mutation and write no request/audit/capacity state. A transaction condition loser also commits nothing. A committed mutation always has exactly one request outcome and one audit event. Scans, GSIs, availability previews, check-then-save, separate writes with compensation, TTL, or old handlers are never authoritative.

### 11.2 Tables and immutable item schemas

`booking_reservations` has string-only partition key `ReservationKey` (no sort key). Slot item:

```text
ReservationKey = SLOT#v1#<b64url(businessId)>#<b64url(resourceId)>#<UTC yyyyMMdd'T'HHmmss'Z'>
Kind = SLOT; BookingId; BusinessId; ProviderProfileId (diagnostic only)
ResourceId = single; StartTimeUtc; EndTimeUtc; Status = pending|confirmed
CreatedAtUtc; SchemaVersion = 1
```

Request/outcome item in the same table:

```text
ReservationKey = REQ#v2#<operation>#<base64url(SHA-256(raw Idempotency-Key))>
Kind = BOOKING_REQUEST; SchemaVersion = 2
ActorBinding = base64url(HMAC-SHA-256(idempotencyPepper, actorProfileId))
Operation; BookingId
RequestFingerprint = base64url(HMAC-SHA-256(idempotencyPepper, JCS(canonical client intent)))
CommittedBookingVersion; CommittedBookingStatus; CommittedAtUtc
ReplayExpiresAtUtc = CommittedAtUtc + 24 hours
HttpStatusCode = 201(create)|200(action)
ContentType = application/json
OutcomeBodyBase64 = base64(exact UTF-8 bytes of BookingMutationResultDto serialized before commit)
OutcomeHeaders = canonical map containing Location for create and ETag/version when emitted
AuditKey; CreatedAtUtc
```

There is no speculative/half-populated request row. It is immutable while replayable, then undergoes the one-way privacy compaction below. The request-key namespace is global per operation: clients MUST generate unguessable keys with at least 128 bits of randomness; because raw keys are never exposed or logged, cross-account collision is treated as key reuse rather than widening the durable key with a personal identifier. The exact outcome is deterministic before transaction submission because booking ID, actor view, server timestamps, snapshots, and resulting version are fixed in the transaction input. It MUST fit DynamoDB’s 400 KiB item limit; current mutation DTOs are capped at 8 KiB serialized, otherwise the request fails before transaction. Dynamic transport headers (`Date`, trace IDs) are not outcome representation and are not stored.

Create table `booking_audit` with string PK `AuditKey`:

```text
AuditKey = AUD#v1#<b64url(bookingId)>#<zero-padded resulting version>
Kind = BOOKING_MUTATION; SchemaVersion = 1
BookingId; BusinessId; ServiceId; ActorProfileId; ActorRole
Operation; FromStatus|null; ToStatus; FromVersion|null; ToVersion
OldStartTimeUtc|null; OldEndTimeUtc|null; NewStartTimeUtc; NewEndTimeUtc
OccurredAtUtc
```

Audit contains no raw or digested idempotency key, request fingerprint, outcome/body hash, contact data, token, or free-form request body. It deliberately has no request-table join key; audit completeness is reconciled by booking ID/version and committed mutation count, never by an idempotency digest. `attribute_not_exists(AuditKey)` plus booking-version condition gives one event per committed version. Booking-audit records are immutable, access-restricted to the booking service for writes and named security/audit roles for reads, encrypted at rest, retained seven years from `OccurredAtUtc`, and held longer when a legal hold applies. Deletion requires a separately approved retention job after the seven-year boundary and produces aggregate reconciliation evidence. This authoritative security-record retention does **not** justify retaining response copies.

Request/outcome rows have a hard replay/privacy window of **24 hours from commit**, superseding DB-D6. Before `ReplayExpiresAtUtc`, they retain only keyed actor/fingerprint bindings and the minimized mutation result—no plaintext profile/party ID, client contact, business address/location, or presentation/free-text field. At or after the boundary, the API MUST NOT replay the body even if compaction has not run: same-key use returns `409 idempotency_window_expired`. A restricted deterministic compactor runs at least hourly and, no later than 25 hours after commit, conditionally transforms the row in place to `Kind=BOOKING_REQUEST_TOMBSTONE`, `SchemaVersion=2`, retaining only `ReservationKey`, `Operation`, `CommittedAtUtc`, and `CompactedAtUtc`; it removes `ActorBinding`, fingerprint, booking/audit IDs, status/version, outcome body/headers, and every other attribute. Tombstones have no TTL and permanently burn the globally scoped operation/key digest without retaining an actor/profile/business/service/booking identifier or response representation. No booking-audit/auth-audit/identity item stores the request-key digest, so the tombstone cannot be joined through contract fields to persistent identity-bearing records. Tombstones MUST NOT be deleted under this contract; changing that rule requires a new reviewed idempotency namespace/version and migration that preserves permanent expired-key behavior. Compaction is idempotent, conditionally requires the expired request kind/version, and never changes booking/audit state. Alarms page on any uncompacted expired row before the 25-hour SLA; access to pre-compaction rows is restricted to the booking service.

### 11.3 Key scope, fingerprint, and replay

`Idempotency-Key` is mandatory on create and every action, with §5.3 validation. Operations are `create`, `confirm`, `decline`, `cancel`, `complete`, `mark_no_show`, and `reschedule`; action is part of both request key and fingerprint. JCS canonical intent contains actor profile ID, operation, target booking ID (except create), service ID and canonical start UTC (create), expected version (actions), and canonical new start UTC (reschedule). It excludes mutable server-read configuration so an ordinary retry remains equivalent.

Before mutation, strongly read the globally scoped operation/request key. An unexpired request with matching `ActorBinding` and fingerprint returns stored status/body/content type/headers, adds `Idempotent-Replayed: true`, and performs **no** eligibility/state/version/slot re-evaluation or audit write. This remains true after later legal mutations only while the 24-hour replay window remains open. An unexpired actor/fingerprint mismatch returns 409 `idempotency_key_reused`. An expired request or tombstone returns this exact tuple without actor/fingerprint/outcome comparison or disclosure: HTTP `409`; `Content-Type: application/problem+json`; private `no-store` headers; no `Idempotent-Replayed`, `Location`, `ETag`, or lifecycle-dependent header; body `{"type":"https://bookspot.example/problems/idempotency-window-expired","title":"Idempotency window expired","status":409,"detail":"The idempotency replay window has expired.","code":"idempotency_window_expired"}`. On concurrent same-key submissions, one transaction commits; a request-put condition loser strongly rereads and applies these rules. Replays never hydrate current profile/contact data.

The SDK `ClientRequestToken` MAY use a bounded digest but is not this durable contract. An indeterminate transport result triggers strongly consistent request-key resolution: found/unexpired same actor+fingerprint -> replay; found/unexpired mismatch -> 409 `idempotency_key_reused`; found expired/tombstone -> 409 `idempotency_window_expired`; not found after bounded retries -> 503 `persistence_unavailable` instructing retry with the same key. It MUST NOT rerun under a new key automatically.

### 11.4 Exact transaction contents by mutation

Every transaction has conditional request Put + conditional booking Put/Update + audit Put. Booking actions condition exact ID, party-resolved canonical identities, expected source status, and expected version, then increment version once. Logical contents are:

- **Create:** three §10.1 configuration ConditionChecks; Put request outcome absent; Put booking version 1 absent; Put every slot absent; Put audit version 1 absent.
- **Confirm:** Put request absent; update pending booking at expected version to confirmed/version+1; conditionally update every owned slot from pending to confirmed (`BookingId` and old Status match); Put audit absent.
- **Decline:** Put request absent; update pending booking to declined/version+1; conditionally delete every old slot owned by booking; Put audit absent.
- **Cancel:** after party/state/version reads and immediately before transaction construction, capture one server UTC admission timestamp `actionNow`. Admission requires `actionNow < startTime`; equality or later fails 409 `booking_cancellation_window_closed` for an authorized party without submitting a transaction. Put the admitted `actionNow` in the immutable request outcome and audit input; then Put request absent, update pending/confirmed booking to cancelled/version+1 at the transaction linearization point, conditionally delete every old slot owned by booking, and Put audit absent. HD-05 is explicitly an **admission-time** policy, not a claim that DynamoDB evaluates wall-clock time at commit: a request admitted before start may commit after start following bounded transport delay. Submission has a 2-second client deadline and an indeterminate result is resolved only by the durable request key; implementations MUST NOT refresh `actionNow`, resubmit under a new key, or describe commit time as the cancellation boundary.
- **Complete / mark-no-show:** Put request absent; update confirmed booking to terminal/version+1 with end/start time condition already evaluated from server clock; conditionally delete every old slot owned by booking; Put audit absent.
- **Reschedule:** three configuration ConditionChecks; Put request absent; conditionally Put every `new−old` slot absent; conditionally delete every `old−new` slot owned by booking; conditionally update each intersection slot to pending when source was confirmed; update booking at expected status/version to pending/version+1 with only times/status/version/updatedAt changed; Put audit absent. Old capacity is never released before acquisition because all actions commit together.

Every retained slot is ownership-conditioned. A slot condition -> 409 `booking_slot_conflict`; booking state/version -> 409 `booking_state_conflict`; unexpired request-key mismatch -> 409 `idempotency_key_reused`; expired request/tombstone -> 409 `idempotency_window_expired`; repeated configuration churn -> 409 `booking_configuration_conflict`; throttling/timeout/unresolved indeterminate result -> 503. Raw cancellation reasons remain internal. A maximum 8-hour create uses 38 actions (32 slots + booking/request/audit + 3 checks); a disjoint 8-hour reschedule uses 70, below DynamoDB’s 100-action limit.

### 11.5 Mandatory replay, atomicity, and audit tests

For each of the seven operations, inject failure at every transaction action and assert zero partial booking/request/slot/audit effects. Simulate response loss after commit and assert a same-key retry within 24 hours returns byte-identical status/body/headers plus replay marker. Commit a later legal mutation and replay the earlier key both before expiry (original minimized representation/version/status, unchanged audit count) and at/after expiry (exact `idempotency_window_expired`, no replay). Concurrent same-key/fingerprint requests yield one mutation/request/audit and identical in-window outcomes; same key/different fingerprint yields one mutation and 409 losers. Distinct-key same-version contenders yield one commit, one request record, and one audit; losers leave neither. Replays and contention losers create no audit. Boundary tests freeze 24-hour behavior; compaction tests prove conditional/idempotent conversion by 25 hours, complete removal of actor/fingerprint/booking/outcome attributes, permanent key burn, safe races between replay and compaction, alerting for SLA breach, and no booking/audit mutation. Schema/reconciliation tests prove no persistent audit/identity item contains `ReservationKey`, request-key digest, request fingerprint, or outcome hash and no contract-field join exists from a tombstone to a booking, actor, business, or service. After every test, assert audit versions are contiguous one-per-booking-version, each unexpired request names exactly one audit key and contains one outcome representation, live slots exactly equal expected cells, terminal slots are empty, and every slot points to one existing live booking. Repeat ambiguous-response and cancellation/reschedule cases against disposable real AWS DynamoDB as well as LocalStack.

## 12. Compatibility and migration

1. **Inventory:** backup/export and use deployed `DescribeTable`. Report statuses, broken service/business links, ambiguous legacy IDs, offsets, grid/duration violations, live overlaps, and deployed indexes.
2. **Expand:** create `booking_reservations` and `booking_audit`; add optional canonical booking attributes/version/snapshots, Business/Service config versions, business-keyed schedule version, and read-compatible DTO mapping; mutation feature flag off. Provision `identity_claims`, `auth_capabilities`, `auth_audit`, `auth_delivery_outbox`, `auth_abuse_counters`, encrypted recovery queue/DLQ, recovery/delivery workers, KMS grants, delivery reconciler and DeliveryKey-only DLQ through their independent §5.4/§9 migrations. Deployment and local integration manifests MUST match exact keys/message schemas/encryption context/retention/TTL rules (`auth_capabilities`, `auth_abuse_counters`, and terminal outbox metadata cleanup only; no TTL on identity, pending/leased delivery, slot, booking-request/tombstone, booking audit, or auth audit items), and MUST provision the hourly request-privacy compactor plus 25-hour SLA alarm before enabling booking writes.
3. **Normalize:** derive each booking through Service -> Business -> owner. Never infer canonical provider from legacy `Booking.ProviderId` alone.
4. **Resolve dirty data:** report overlaps deterministically; humans choose outcomes. Do not auto-cancel.
5. **Mutation freeze:** briefly reject all booking mutations while reads continue.
6. **Backfill:** transactionally add canonical fields and cells. Any collision/malformed live record blocks cutover.
7. **Cut over all writers together:** enable version-conditioned Business/Service/schedule writers and the transactional booking writer as one compatibility set. Old/new readers may coexist; old booking/config mutation writers may not. Old instances become read-only before freeze lifts.
8. **Verify:** every future live booking owns exactly expected cells; every cell has one live booking; terminal bookings have none; exception count is zero.
9. **Contract later:** remove legacy aliases/fields only after telemetry and tests prove no consumer. Query GSIs are separate online optimizations.
10. **Rollback:** before cutover, new table can be rebuilt. After any new transactional mutation, do not revert to old writer; disable mutations and roll forward. Never delete a cell without `BookingId` ownership condition.

Frontend migrates vertical slice by vertical slice: shared ProblemDetails/AuthResponse -> auth/profile -> business resolution/settings -> service/search -> hours/availability -> bookings/actions -> dashboards -> alias cleanup. It must handle 204 without JSON parsing and show success only after a real 2xx.

## 13. P0–P3 remediation order

Priority is ranked by exploitability first, then user/data integrity impact, then dependency leverage.

### P0 — release blockers

1. **Fail closed:** global authenticated fallback, explicit public route inventory, production Test/Swagger unmapping, redacted structured logging, safe ProblemDetails, production JWT startup validation and TLS metadata.
2. **Stop account/tenant compromise:** remove anonymous profile create/mutations and entity serialization; self-only DTO routes; immutable role/email; authoritative business/service/hour/review/booking ownership.
3. **Disable destructive booking paths:** unmap generic booking PUT/DELETE; implement explicit actor/state/version actions and concealed non-party handling.
4. **Safe auth identity:** register route with atomic normalized-email uniqueness, one approved password policy/hasher, generic login failure, exact distributed §5.4 abuse controls; resolve HD-01/HD-02.
5. **Safe recovery:** digest-only capability, body route, atomic consume/password/session update, replay/concurrency protection, no token logging.
6. **Atomic booking writer:** canonical IDs/snapshots/version, reservation table, transaction/idempotency mechanism, migration freeze/backfill/cutover.
7. **Create mandatory backend security/contract/concurrency test project and enforce it in CI.**

### P1 — core user correctness

1. `/businesses/mine` and Settings migration from profile ID to real business ID.
2. Service create/update/delete ownership through Business; server-derived provider fields.
3. Business schedule and real service availability; replace static slots.
4. Real booking create from `{serviceId,startTime}`; frontend awaits persistence and fixes navigation.
5. Real self-scoped dashboards using price snapshots; no fabricated values/identities.
6. Safe review author/completed-booking/one-per-booking rules.

### P2 — scalability, compatibility, and operations

1. Paged search/category contract and frontend HTTP/config hardening.
2. Replace scans for list queries with measured query GSIs after canonical ID backfill; never use them for exclusion.
3. Reproducible LocalStack/AWS provisioning parity, health/readiness, real-AWS transaction parity tests.
4. Alias telemetry/deprecation and cleanup; remove frontend mock production paths/dead controls.
5. Refresh dependency audit, remediate supported vulnerable packages, reduce bundle split/load cost.

### P3 — quality and consistency

1. Role-specific navigation/terminology, no fabricated presentation fallbacks.
2. Remove noisy frontend/backend diagnostics; enforce lint and contract generation.
3. Documentation/config drift checks and measured performance/bundle budgets.

## 14. Finding-to-workstream mapping

Legend: FE frontend, BE backend/application, INF infrastructure/persistence, VAL validation/tests. Every baseline finding is represented.

| Finding | FE | BE | INF | VAL |
|---|---|---|---|---|
| C1 | wire canonical register/AuthResponse | expose register DTO | atomic email key/rate limits | register/login/concurrent duplicate tests |
| C2 | self-profile types only | fallback auth, DTO, self resolver | session revoke on delete | anonymous/IDOR/serialization/overpost tests |
| C3 | explicit actions/version | party/state handlers; disable PUT/DELETE | transactional version/audit | matrix, timestamp preservation, action race |
| C4 | carry real businessId | Business owner resolver | canonical ID backfill | two-business/profile-ID confusion tests |
| C5 | body/fragment reset flow | one hasher and generic semantics | digest/latest/atomic consume/session version | lifecycle/replay/race/log canaries |
| H1 | show 409 safely | persistence-neutral atomic boundary | reservation transactions | N-way overlap/pending tests |
| H2 | configurable API URL | consume config | pinned LocalStack/parity/DescribeTable | reproducible boot and AWS parity |
| H3 | no secret in client/log | fail startup, strict JWT validation | external key/rotation/TLS | startup and forged/stale-token matrix |
| H4 | valid service booking route | availability endpoint | none | route/flow E2E |
| H5 | await POST; 2xx-only success | canonical create | idempotency | failure/timeout UI integration |
| H6 | Settings selects `/businesses/mine` ID | owner business routes | query owner index optional | persistence round-trip/two businesses |
| H7 | server slots only | schedule projection | query live cells/bookings | hours/timezone/conflict tests |
| H8 | role UX only | fallback + every owner/author resolver | atomic review uniqueness | route inventory and actor matrix |
| H9 | real discriminated dashboard | subject-scoped real queries | snapshot/query indexes | zero state/cross-tenant/totals |
| H10 | env base URL, empty-body handling | consistent 204/ProblemDetails | deploy config | 204/non-JSON/error contract tests |
| H11 | send minimal create DTO | derive all fields | snapshot canonical IDs | overpost/spoof/snapshot immutability |
| M1 | consume paged envelope/category | bounded search DTO | optional query index | FE-BE contract/pagination tests |
| M2 | cursor UX | persistence interfaces | query GSIs after migration | pagination/scale plans; no Scan authority |
| M3 | wire or remove controls | real operations only | none | E2E no false success/dead controls |
| M4 | remove fabricated fallbacks | return canonical display fields | none | fixture/snapshot tests prohibit constants |
| M5 | role-consistent navigation | immutable role DTO | none | role-flow accessibility/E2E |
| M6 | no token console/query; reset-page policy | body token; production unmap; redact | protected logs/secret scanning | canary log + production surface tests |
| M7 | fix lint and enforce | add test projects | CI gates | all mandatory suites |
| M8 | upgrade/split safely | n/a | dependency/SBOM policy | refreshed audit/build budget |
| L1 | remove debug console PII | route-template logging | redacted sink/retention | automated canary scan |
| L2 | valid env/config docs | startup/config validation | parity and drift checks | deploy smoke/config tests |

## 15. Implementation dependencies

1. Freeze contract decisions HD-01 and HD-02; assign owners for remaining human decisions.
2. Establish test project, route inventory, ProblemDetails, DTO serialization, and auth fallback before adding feature routes.
3. Implement canonical identity/ownership resolver and DTOs before business/service/settings migration.
4. Implement common normalizer/hasher/session version before registration and reset.
5. Inventory production DynamoDB and data before any booking backfill.
6. Add canonical booking fields and persistence-neutral mutation interface before transactional writer.
7. Create reservation/audit infrastructure and versioned Business/Service/schedule configuration before migration freeze/backfill/cutover.
8. Cut over all booking writers before availability/dashboard can claim authoritative live data.
9. Price snapshots and canonical IDs precede financially meaningful dashboard totals.
10. Frontend types/client migrate before legacy aliases are removed; aliases have telemetry and dated exit criteria.
11. Query GSIs and performance optimization follow correctness and identifier cleanup.
12. Supply-chain upgrades occur in isolated, tested tasks; they must not touch the protected project file or excluded payment subtree accidentally.

## 16. Falsifiable acceptance criteria and mandatory tests

### 16.1 Authentication and profile

- Valid client and provider registration follows the resolved HD-01 status contract, returns exact `AuthResponse`, authenticates `/profiles/me`, persists normalized email and BCrypt hash, and never serializes a hash.
- Two concurrent case/space/NFC-equivalent registrations produce at most one profile/unique key; response behavior matches HD-01.
- Registration fault injection at each identity transaction action proves no orphan profile or claim. Strong claim->profile login lookup, permanent claim reservation on allowed deletion, migration freeze/collision report/zero-exception gate, and one-to-one reconciliation are asserted; scan/GSI uniqueness implementations fail review.
- Unknown/admin/mixed role and overposted IDs/hash/timestamps fail 400 with no mutation.
- Unknown email and wrong password produce identical generic 401; passwords/emails do not appear in logs.
- Null/blank/whitespace/fallback/short production JWT key fails startup. Wrong key/algorithm/issuer/audience/lifetime and stale session version return 401.
- Anonymous profile operations return 401. Client/provider A cannot read/update/delete B; response is concealed 404 and state unchanged.
- DTO recursion proves no password/hash/reset/session secret property in any response/error.
- JWT snapshots assert the exact `sub,user_type,sv,jti,iat,nbf,exp,iss,aud` set, unique claims, 900-second lifetime, 30-second skew, pinned algorithm/key-family/`kid` rules, and no PII. Security-version increments and cache invalidation revoke a signed stale token before reset/delete success returns.
- Register/login exact-response snapshots prove all canonical and AUTH-ALIAS-1 fields are always present and equal during the window, all are absent after the telemetry/date gate, and `expiresAt==exp`. Every auth/recovery/private success and error has exact `no-store, max-age=0`/`Pragma: no-cache` headers.
- Every register/login/forgot/validate/reset route passes the §5.4 applicable-dimension limit, trusted-proxy, spoofing, normalization, fixed-window, `Retry-After`, two-instance atomicity, fail-closed 503, retention/IAM, and generic 429 snapshots. Tests prove account/capability existence cannot change charging or the 429 tuple, AWS throttling never maps to 429, and one cookie-less/malformed-input attacker cannot throttle unrelated cookie-less clients on other IP/account keys.
- Table-driven validation exercises every §5.3 field/query at omitted/null/blank/min/max/one-beyond boundaries, decimal/cardinality/duplicate/unknown-property rules, empty PATCH, repeated/unknown query keys, and cursor tampering; exact stable field keys/tokens are asserted.

### 16.2 Password reset

- Known and unknown forgot-password calls enqueue the identical encrypted request shape—including opaque IP-prefix/device provenance hashes but no identity-derived field—without synchronous identity lookup and return the exact byte-equivalent success status/content-type/cache headers/body at the §9 400–450 ms release gate; mail only for a known account in the asynchronous worker. A winning issue audit copies those queued provenance hashes exactly; unknown/broken claims create no audit. Winning reset returns its distinct exact success body.
- A multi-instance statistical timing suite uses at least 10,000 known and 10,000 unknown requests per warm, cold-start, queue-latency, worker-error, and mail-error condition with identical IP/account-limit bypass fixtures. It asserts identical configured 400–450 ms support, two-sample Kolmogorov–Smirnov `p >= 0.05`, absolute median difference <=5 ms, absolute p95 difference <=10 ms, and no classifier achieves ROC-AUC >0.55. Queue failures produce the same 450 ms 503 distribution for inputs later shown known/unknown; worker/DLQ/mail outcomes cannot alter captured HTTP responses.
- Stored reset value is digest only; latest issue supersedes previous; validation is read-only.
- Unknown, empty, malformed, superseded, expired, used, pepper-mismatch and race-losing token have the exact byte-equivalent §9.1 status/content-type/body/header tuple and timing bucket for validate and consume.
- Valid reset makes new password work and old password/stale sessions fail.
- N concurrent consume attempts yield exactly one password mutation/success.
- API path/query token forms are unmapped; canary token/password/JWT/email/phone do not occur in application/access/exception/trace/frontend-console capture.
- Issue/consume fault injection at every transaction action yields no partial capability/profile/security-version/auth-audit/delivery state. Exact four/five-action counts, winner-one/loser-zero audit and delivery semantics, reconciliation, seven-year audit retention/no TTL, outbox payload encryption/erasure/TTL, IAM/KMS-context isolation, provisioning parity, and no orphan audit/state/delivery are asserted. Crash injection covers before transaction, every transaction action, issue and consume commit-response loss/process restart, claim, decrypt, provider acceptance, and delivery acknowledgement; same-key consume replay returns the exact stored success while different/missing keys remain generic invalid. Delivery tests prove every committed issue is eventually provider-accepted before deadline or atomically revoked, no uncommitted token is mailed, ack loss can only duplicate the identical capability, superseded mail never validates, terminal payload/key fields are erased, confirmation handoff survives consume commit, and reconciler/lease/DLQ paths converge. Authenticated provider-event tests cover synchronous rejection, delayed and duplicate asynchronous hard bounce, forged/mismatched provider IDs, superseded generation, and bounce-vs-consume races. Migration proves no plaintext legacy row is copied, old writer is disabled, and the capability table contains digest keys only.

### 16.3 Authorization and ownership

- Reflect all routes and assert exactly one explicit public/private classification plus active fallback policy; new unclassified route fails CI.
- Anonymous/client/provider call every route. Repeat object routes for owner, same-role non-owner, wrong role, absent and malformed IDs; assert status and no side effects.
- Provider with two businesses manages both. Profile ID passed as business ID returns 404. Provider A cannot mutate B’s business/service/hour.
- Service ownership always loads Business. Changing denormalized `Service.ProviderId` or legacy `Booking.ProviderId` cannot grant access.
- Review requires completed booking client; provider/non-author denied; concurrent duplicate reviews produce one record.
- Route inventory test compares endpoint metadata by exact method/template against every row of §7/§7.1, including current `GET /services`, `GET /businesses/provider/{providerId}/services`, `GET /dashboard/clients`, and `GET /dashboard/my-stats`; it asserts authn/authz, DTO, active filtering, headers, replacement, telemetry, and unmap/removal behavior.
- Public service routes never return inactive records; owner manage list/detail returns owned inactive records and concealed 404 for non-owner/absent. Client/provider booking DTO snapshots assert exact mutually exclusive party property sets and private cache headers.

### 16.4 Booking and concurrency

- Minimal create accepts exactly `{serviceId,startTime}` and rejects every enumerated derived/server-owned/unknown field in §6 with exact 400 `must_be_omitted`; no booking request alias or ignore behavior exists.
- N=20 same slot/distinct keys -> exactly 1x201, 19x409, one booking, exact cells all owned by winner.
- N=20 same key/fingerprint -> same booking/representation, one mutation; different fingerprint -> 409.
- Partial overlap -> one success; adjacent half-open intervals -> both succeed.
- Pending and confirmed block; terminal release is atomic. Pending and confirmed cancellation both require captured server admission `actionNow < startTime`; before/equal/after admission-boundary tests for each actor/source assert equality and later return 409 `booking_cancellation_window_closed` with no mutation. A deterministic delayed-commit test proves a request admitted before start may commit after start under the explicit HD-05 admission-time policy while retaining its immutable `actionNow`; no test or implementation claims wall-clock evaluation inside DynamoDB commit.
- Every transition table row succeeds only for permitted party; every unlisted transition -> 409; wrong-role party ->403; non-party -> concealed 404.
- Status action preserves times byte-for-byte. Two same-version actions -> exactly one commit.
- Cancel-vs-confirm yields one coherent winner. Failed reschedule retains old booking and all old cells. Reschedule-vs-create yields one owner per contested cell.
- Equivalent offset instants map to identical UTC keys; offsetless/off-grid/non-aligned duration ->400.
- Fault injection at every action of all seven transaction shapes produces no partial booking/request/slot/audit state. Ambiguous response retry replays exact stored status/body/headers.
- Replay each create/action key after at least one later legal mutation within 24 hours and assert the earlier minimized `BookingMutationResultDto` is returned without party/profile/business/service IDs, client contact, address/location, presentation, price, or free text. At/after 24 hours assert `idempotency_window_expired`; by 25 hours assert only the non-identifying tombstone remains. There is one request and one audit per committed version; replays/contention losers add no audit.
- Inactive business/service, past/equal-now, closed/outside/partial-hours, close boundary, offset mismatch, spring gap, both fall-back occurrences, and TZDB fixture tests assert §10.1 outcomes. Concurrent deactivate/duration/price/timezone/hours changes serialize by versioned condition checks; snapshots remain immutable.
- Cross-business same time succeeds twice; two services in one business conflict under interim single resource.
- After every mutation: live booking cells equal deterministic expected cells; terminal booking has none; each cell points to exactly one existing live booking.
- Repeat overlap, adjacency, cancellation and reschedule races on disposable real AWS DynamoDB, not LocalStack only.

### 16.5 Frontend, dashboard, operations

- Booking/search/settings flows show success only after real 2xx; 204 is accepted without JSON parse; ProblemDetails renders safely.
- Static slot lists and fabricated provider/dashboard values are absent from production mode.
- New account dashboard returns exact zeroes/empty arrays. Completed booking snapshot changes totals deterministically; later service price changes do not.
- Provider A cannot see B’s dashboard/clients. Provider contact data is limited to clients with bookings at owned businesses.
- Current dashboard compatibility routes return only the exact real/self-scoped shapes and satisfy their dated telemetry/removal gates; `/dashboard/clients` is provider-owned-business scoped and capped/sorted as §7.1 defines. Provider-insights snapshots assert its named compatibility DTO, independently optional inclusive legacy bounds, start-only/end-only/neither/date-only/exact-end cases, internal exclusive translation, timezone intersections, snapshot revenue, stable top-10 ordering/cap, and zero/empty state; it never returns `ProviderDashboardDto`.
- Production Test/Swagger routes return 404; no development CORS/diagnostic behavior leaks into Production.
- Local integration boot creates the same required tables/keys/indexes/TTL plus encrypted recovery queue/DLQ/retention and worker bindings as declared deployment; `DescribeTable` and queue-attribute evidence is captured. LocalStack limitations MUST use an explicit emulator/test double with the same message and failure contract, while the timing/error suite runs against the production queue class in disposable infrastructure before release.
- Frontend lint/build/audit and backend build/tests are CI gates; dependency advisories are refreshed rather than copied from the baseline.

## 17. Release-blocking task list

- RB-01: Resolve HD-01 registration enumeration/verification contract.
- RB-02: Resolve HD-02 password policy and benchmark/calibrate BCrypt.
- RB-03: Global auth fallback, explicit route inventory, production diagnostics unmapping.
- RB-04: External validated JWT signing/session-version boundary and redacted logs/errors.
- RB-05: DTO-only profile/auth/public/party responses; close profile IDOR and overposting.
- RB-06: Authoritative ownership resolvers on every private mutation/read.
- RB-07: Atomic normalized-email uniqueness and safe register/login behavior.
- RB-08: Complete atomic password-reset lifecycle and session revocation.
- RB-09: Unmap old booking PUT/DELETE and implement explicit state/version/audit actions.
- RB-10: Inventory production data and resolve DB-D2, DB-D4, DB-D7 exceptions.
- RB-11: Reservation table, transactional/idempotent writer, write freeze/backfill/all-writer cutover.
- RB-12: Mandatory route/actor/DTO/auth/reset/ownership/booking/concurrency/logging/production-surface suites passing in CI, including real-AWS parity subset.
- RB-13: Frontend real booking/settings/availability flows and no fabricated production success/data.
- RB-14: Preserve `backend/BookSpot.Application/BookSpot.Application.csproj`, all unrelated dirty work, and `frontend/payment-reconciliation-mvp/**` through every implementation task.

## 18. Document verification boundary

This is analysis/documentation only. It authorizes no application-code mutation. Evidence references are repository-relative and point to the current tree or named source artifacts. Dynamic behavior from the baseline was not fabricated or silently treated as re-run. H2/L2 were explicitly downgraded where current dirty infrastructure code has changed since the baseline. Implementers must re-inspect current source and working-tree state at task start.
