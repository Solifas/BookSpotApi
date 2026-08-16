# BookSpot fail-closed authentication and authorization security contract

**Date:** 2026-08-10
**Owner:** Cerberus / Security Engineering
**Status:** Mandatory release contract; design/review only
**Scope:** Main BookSpot ASP.NET Core backend and React frontend. `frontend/payment-reconciliation-mvp/` is excluded.
**Evidence:** Current controllers, handlers, entities, repositories and `Program.cs`; `docs/bookspot-baseline-report-2026-08-09.md`; `Downloads/bookspot-full-stack-assessment-2026-08-09.md`; `docs/bookspot-domain-api-contract-2026-08-10.md`; `docs/bookspot-product-requirements-2026-08-09.md`.

This document defines security boundaries, not UI hints. Every rule is enforced by the API/application/persistence boundary. Hiding a button, route guard, stored frontend role, path ID, request DTO ID, or denormalized record field is never authorization.

## 1. Release verdict and non-negotiable blockers

BookSpot is **not releasable** until all blockers below are implemented and their mandatory tests pass:

1. A global authenticated fallback policy is active; every anonymous route is explicitly opted in. Route-policy tests prove no sensitive route is open by omission.
2. Profile entities are never serialized. Anonymous profile CRUD is removed/closed; self-update cannot change ID, email, role, password hash, or creation metadata.
3. JWT signing configuration fails startup on absent, blank, fallback, short, or source-controlled production key material.
4. Registration and login use one atomic normalized-email identity boundary. A scan/check followed by save is not uniqueness enforcement.
5. Password reset uses one password hasher, hashed reset tokens at rest, 30-minute expiry, atomic single-use consumption, replay/race rejection, API-body transport, and session revocation.
6. Every business, service, business-hours, booking, review, and dashboard private operation performs server-side role and ownership checks.
7. Provider ownership is always resolved through authoritative `Business.ProviderId`. It is never inferred from `businessId == subject`, `Service.ProviderId`, legacy `Booking.ProviderId`, provider name, or a frontend role.
8. Generic booking `PUT` and hard `DELETE` cannot reach the old handlers. Explicit, actor-scoped state actions replace them; all derived booking fields are server controlled.
9. Production does not map `TestController`; Swagger is disabled or separately operator-authenticated; sensitive paths/bodies/headers are redacted from logs.
10. Public/private DTOs are explicit allow-lists and mandatory IDOR, state, concurrency, enumeration, logging, and secret-startup tests pass in CI.

A product decision may make a rule stricter. It may not waive tenant isolation, credential protection, atomic uniqueness/single-use, or server-side enforcement.

## 2. Trust model and security defaults

### 2.1 Actors and identity

- **Anonymous:** no valid bearer access token.
- **Client:** authenticated subject with immutable server-issued role `client`.
- **Provider:** authenticated subject with immutable server-issued role `provider`.
- There is no admin role in the current product. Do not create an implicit admin, support override, provider-as-admin, or profile role-change endpoint.
- JWT subject is the profile ID. The role claim is issued from persisted server state, never copied from an update request. Unknown/multiple roles fail authorization.
- Token validity is necessary but not sufficient: handlers must authorize the loaded resource.

### 2.2 Deny-by-default controls

- Configure an ASP.NET Core fallback policy requiring an authenticated principal.
- Use explicit `[AllowAnonymous]` only for routes marked Public in the matrices below.
- A mutation requires role plus resource authorization in the application/domain boundary. Controller attributes alone are not sufficient.
- Missing/malformed/stale subject or role claims fail closed.
- Production route inventory/OpenAPI CI fails if a new endpoint lacks an explicit classification.
- CORS is not access control. All controls apply equally to browsers, scripts, mobile clients, direct HTTP, and internal callers.

### 2.3 Authoritative ownership resolvers

Resolve in this order on every request; do not trust denormalized shortcuts:

- `OwnProfile(profileId)`: `JWT.sub == Profile.Id`.
- `OwnBusiness(businessId)`: load Business; require `Business.ProviderId == JWT.sub`.
- `OwnService(serviceId)`: load Service, then its Business by `Service.BusinessId`; require `Business.ProviderId == JWT.sub`.
- `BookingClient(bookingId)`: load Booking; require canonical `Booking.ClientProfileId` (legacy `ClientId` only during verified migration) equals `JWT.sub`.
- `BookingProvider(bookingId)`: load Booking -> Service by `Booking.ServiceId` -> Business by `Service.BusinessId`; require `Business.ProviderId == JWT.sub`.
- `OwnBusinessHour(hourId)`: load hour -> Business by `BusinessHour.BusinessId` -> compare `Business.ProviderId`.
- `ReviewAuthor(reviewId)`: load Review -> Booking by `Review.BookingId`; require the booking client equals `JWT.sub`.

Current `Booking.ProviderId` is ambiguous and currently receives a business ID. It is prohibited as an authorization input. Current `Service.ProviderId` and `ProviderName` are denormalized presentation data and are also prohibited as authorization inputs.

## 3. Current route disposition matrix (all 43 controller actions)

Legend: **Allow** means permitted only with the conditions in the last column; all unlisted actors are denied. `404 conceal` means an absent resource and a resource owned by someone else have the same external response.

### 3.1 Authentication

| Current route | Anonymous | Client | Provider | Target security contract |
|---|---:|---:|---:|---|
| `POST /auth/login` | Allow | Allow | Allow | Public credential exchange; generic 401 for unknown email/wrong password; normalized email; rate limited; no account-state oracle. Existing authenticated callers gain no extra behavior. |
| `POST /auth/forgot-password` | Allow | Allow | Allow | Always same 200 body/status and comparable timing for known/unknown accounts; enqueue email only for known account; no token in response. |
| `POST /auth/reset-password` | Allow | Allow | Allow | `{token,newPassword}` in JSON body; atomically consume valid token and change password; generic invalid-token response; revoke sessions. |
| `GET /auth/validate-reset-token/{token}` | Deny | Deny | Deny | Remove/unmap and return 404. Token in path is prohibited. Replace with body route below. |

`POST /auth/register` and body-token validation are required new routes in §4.

### 3.2 Profiles/account administration

| Current route | Anonymous | Client | Provider | Target security contract |
|---|---:|---:|---:|---|
| `GET /profiles/me` | Deny | Allow self | Allow self | Return `ProfileDto`, never entity. `Cache-Control: no-store`. |
| `GET /profiles/{id}` | Deny | Allow only `id == sub` | Allow only `id == sub` | Transitional alias; cross-profile is `404 conceal`. Providers do not gain arbitrary profile read. Deprecate in favor of `/me`. |
| `POST /profiles` | Deny | Deny | Deny | Remove/unmap (404/410). Registration is the only account-creation path. |
| `PUT /profiles/{id}` | Deny | Allow only `id == sub` | Allow only `id == sub` | Transitional self alias to safe profile patch. Body ID must be absent or match path; overposted `email`, `userType`/role, `passwordHash`, `createdAt` are rejected. |
| `DELETE /profiles/{id}` | Deny | Allow only `id == sub` | Allow only `id == sub` | Transitional self alias; cross-profile `404 conceal`; server performs account-lifecycle checks and revokes sessions. |

No route permits provider-to-client profile administration. No current admin exists. Email change and password change, if added, are dedicated re-authenticated, verified operations—not fields on profile update.

### 3.3 Businesses and services

| Current route | Anonymous | Client | Provider | Target security contract |
|---|---:|---:|---:|---|
| `GET /businesses/{id}` | Allow | Allow | Allow | Public `BusinessPublicDto`; active/public fields only. Provider profile PII is not joined into it. |
| `GET /businesses/{id}/services` | Allow | Allow | Allow | Public active `ServicePublicDto[]`; inactive records visible only through a separate owner route. Empty list is 200. |
| `GET /businesses/provider/{providerId}/services` | Allow | Allow | Allow | Public active services only. Transitional browse route; must not expose provider profile/contact/private records. Prefer business/service routes. |
| `POST /businesses` | Deny | Deny | Allow | Provider role; server sets `ProviderId = sub`; body owner/ID/ratings/timestamps rejected; explicit DTO. |
| `PUT /businesses/{id}` | Deny | Deny | Allow owner | Load Business and require `Business.ProviderId == sub`; body ID/owner/ratings/createdAt rejected; non-owner `404 conceal`. |
| `DELETE /businesses/{id}` | Deny | Deny | Allow owner | Same resolver; prefer deactivate. Return 409 if history/dependencies prohibit deletion. |
| `GET /services` | Allow | Allow | Allow | Public active records only via DTO; stable bounded pagination is preferred. |
| `GET /services/search` | Allow | Allow | Allow | Public active service/business records only; bound page/range/filter sizes; DTO; no private contact/profile joins. |
| `GET /services/{id}` | Allow | Allow | Allow | Public DTO; inactive service is 404 to non-owner. Owner-private view requires authenticated owner route/flag with explicit policy. |
| `POST /services` | Deny | Deny | Allow owner | Load body `businessId`, then require `Business.ProviderId == sub`; derive provider IDs/name; reject client-controlled derived fields. |
| `PUT /services/{id}` | Deny | Deny | Allow owner | Resolve Service -> Business -> `ProviderId`; reject ownership/ID/provider/timestamps/rating overposting; non-owner `404 conceal`. |
| `DELETE /services/{id}` | Deny | Deny | Allow owner | Same resolver; prefer deactivate and preserve booking history; non-owner `404 conceal`. |

### 3.4 Business hours, locations and reviews

| Current route | Anonymous | Client | Provider | Target security contract |
|---|---:|---:|---:|---|
| `GET /business-hours/{id}` | Deny | Deny | Allow owner | Raw schedule is settings data. Resolve Hour -> Business -> `Business.ProviderId`; non-owner `404 conceal`. Canonical schedule route is in §4. |
| `POST /business-hours` | Deny | Deny | Allow owner | Resolve body `businessId` through Business; server ID; validate day/time/timezone and duplicates. |
| `PUT /business-hours/{id}` | Deny | Deny | Allow owner | Resolve existing hour ownership before applying fields; body cannot move an hour to another business. |
| `DELETE /business-hours/{id}` | Deny | Deny | Allow owner | Same ownership resolver; non-owner `404 conceal`. |
| `GET /locations/cities` | Allow | Allow | Allow | Public aggregate DTO from active businesses/services only; no private addresses/profile data; bounded/cacheable response. |
| `GET /reviews/{id}` | Allow | Allow | Allow | Public `ReviewPublicDto` includes review ID, rating, comment and safe display metadata only; omit booking ID and client contact/ID. |
| `POST /reviews` | Deny | Allow booking client | Deny | Load booking; caller must be its client; booking must be completed; one review per booking, atomically enforced; rating bounded. |
| `PUT /reviews/{id}` | Deny | Allow review author | Deny | Resolve Review -> Booking -> client; non-author `404 conceal`; rating/comment only. |
| `DELETE /reviews/{id}` | Deny | Allow review author | Deny | Same resolver; product may choose soft delete. Provider moderation is not an implicit capability. |

### 3.5 Bookings

| Current route | Anonymous | Client | Provider | Target security contract |
|---|---:|---:|---:|---|
| `GET /bookings/{id}` | Deny | Allow booking client | Allow owning provider | Load and authorize party; non-party `404 conceal`; minimized party-specific `BookingDto`. |
| `GET /bookings/client/{clientId}` | Deny | Allow only `clientId == sub` | Deny | Transitional alias; self-scoped list; path mismatch concealed/denied. Prefer `/client/me`. |
| `GET /bookings/provider/{providerId}` | Deny | Deny | Allow only `providerId == sub` | Transitional alias; query by businesses whose `Business.ProviderId == sub`, not legacy Booking.ProviderId. Prefer `/provider/me`. |
| `POST /bookings` | Deny | Allow | Deny | Client only. Server derives client, business, provider, provider name, end, status, price/duration/name snapshots, IDs and timestamps. Requires atomic capacity and idempotency controls. |
| `PUT /bookings/{id}` | Deny | Deny | Deny | Return 410/404; old generic handler is a release blocker. Use explicit actions. No compatibility path may write default timestamps or arbitrary status. |
| `DELETE /bookings/{id}` | Deny | Deny | Deny | Hard delete is prohibited. User cancellation uses action route and preserves history/audit. |

Booking action authorization:

| Source -> action -> target | Client | Provider | Required invariants |
|---|---:|---:|---|
| create -> `pending` | Booking creator | Deny | Active service/business; future valid slot; derived end/snapshots; atomic reservation. |
| `pending` -> confirm -> `confirmed` | Deny | Owning provider | Times unchanged; expected version; audit. |
| `pending` -> decline -> `declined` | Deny | Owning provider | Terminal; release capacity atomically. |
| `pending`/`confirmed` -> cancel -> `cancelled` | Booking client | Owning provider | Product timing policy applies; terminal; atomic release. |
| `pending`/`confirmed` -> reschedule -> `pending` | Booking client | Owning provider | New start only; derive end; revalidate hours/capacity; atomic move; expected version. |
| `confirmed` -> complete -> `completed` | Deny | Owning provider | `endTime <= now`; expected version; terminal. |
| `confirmed` -> mark_no_show -> `no_show` | Deny | Owning provider | `startTime <= now`; expected version; terminal. |
| terminal -> any | Deny | Deny | 409 for an authorized party; no mutation. |

A non-party receives concealed 404 before state details are evaluated. A party with the wrong role/action receives 403. A valid party attempting an illegal/stale transition receives generic 409 without rival-client or schedule details.

### 3.6 Dashboard and diagnostics

| Current route | Anonymous | Client | Provider | Target security contract |
|---|---:|---:|---:|---|
| `GET /dashboard/provider/{providerId}/stats` | Deny | Deny | Allow only `providerId == sub` | Transitional self alias; real provider-scoped data only. |
| `GET /dashboard/providers/{providerId}/insights` | Deny | Deny | Allow only `providerId == sub` | Same. Date ranges bounded. |
| `GET /dashboard/clients` | Deny | Deny | Allow | Provider only; clients derived only from bookings belonging to businesses where `Business.ProviderId == sub`; minimal contact data needed for booking operations. |
| `GET /dashboard/client/{clientId}/stats` | Deny | Allow only `clientId == sub` | Deny | Transitional client-self alias. Provider-wide arbitrary client statistics are prohibited; provider gets scoped clients via `/dashboard/clients`. |
| `GET /dashboard/my-stats` | Deny | Allow self | Deny | Real client-self data only. |
| `GET /test/exception/{type}` | Deny | Deny | Deny | Not mapped in Production; 404. Development mapping is local/test-only and must never return production secrets/stack traces. |
| `GET /test/validation-details` | Deny | Deny | Deny | Same. |

Swagger/OpenAPI UI and document are also diagnostics: production default is not mapped (404). If business operations require them, place them behind a separately authenticated operator boundary; client/provider JWTs do not grant access.

## 4. Required canonical routes

These routes remove path-controlled identity and unsafe generic operations. Existing aliases above may exist only while equally secured and telemetry-backed for removal.

| Route | Anonymous | Client | Provider | Contract |
|---|---:|---:|---:|---|
| `POST /auth/register` | Allow | Allow | Allow | Registration contract in §6; no body-controlled ID or unvalidated role. |
| `POST /auth/validate-reset-token` | Allow | Allow | Allow | `{token}` JSON body; generic validity response; `Cache-Control: no-store`. Validation does not consume. |
| `PATCH /profiles/me` | Deny | Allow | Allow | Self mutable fields only: `fullName`, `contactNumber`. |
| `DELETE /profiles/me` | Deny | Allow | Allow | Re-authentication recommended; revoke sessions and resolve retention/dependencies. |
| `GET /businesses/mine` | Deny | Deny | Allow | Resolve by `Business.ProviderId == sub`; explicit owner DTO. |
| `PATCH /businesses/{businessId}` | Deny | Deny | Allow owner | Ownership via loaded Business. |
| `PATCH /services/{serviceId}` | Deny | Deny | Allow owner | Service -> Business ownership. |
| `GET /businesses/{businessId}/hours` | Deny | Deny | Allow owner | Owner settings schedule. |
| `PUT /businesses/{businessId}/hours` | Deny | Deny | Allow owner | Atomic whole-schedule replacement; body cannot select owner/business. |
| `GET /services/{serviceId}/availability` | Allow | Allow | Allow | Public bounded availability projection; no party identity; booking create remains final authority. |
| `GET /bookings/client/me` | Deny | Allow | Deny | Self list. |
| `GET /bookings/provider/me` | Deny | Deny | Allow | Resolve owned businesses from subject; optional business filter must also be owned. |
| `POST /bookings/{bookingId}/actions` | Deny | Allow party/action | Allow party/action | Strict action enum, expected version, optional start only for reschedule, idempotency key. |
| `GET /dashboard/me` | Deny | Allow | Allow | Server chooses role-specific real DTO from authenticated role. |

There is no frontend-enforced `/settings` security boundary. Settings composes `/profiles/me`, `/businesses/mine`, owner business/service/hour operations, and each API independently authorizes the caller.

## 5. Response DTO and sensitive-data contract

Use explicit response DTO allow-lists. Persistence entities must never cross the controller boundary.

- `ProfileDto`: `profileId`, `email`, `fullName`, `contactNumber`, immutable `userType`, `createdAt`. Only self receives it. Never: `PasswordHash`, password/reset/session state, internal indexes, version secrets.
- `AuthSessionDto`: access token, type, expiry, self `ProfileDto`. `Cache-Control: no-store`; token never logged.
- `BusinessPublicDto`: public marketplace name/description/city/address/business phone/email/site/image/rating/review count/timezone and active state as needed. Never join provider profile email/contact. Owner-only DTO may add operational fields, but never credentials.
- `ServicePublicDto`: service/business IDs, server-derived provider display identity, name/description/category/price/currency/duration/image/tags/location. Only active public records.
- `ReviewPublicDto`: review ID, rating, comment, created/updated timestamps and an approved display alias if required. Omit booking ID, client ID/email/phone.
- `BookingDto`: always party-only. Client view receives service/business/provider presentation and its own fields. Provider view may receive the booking client's name/email/contact only where operationally necessary. Never expose password data, unrelated profiles, internal reservation keys, idempotency fingerprints, or another booking's conflict details.
- Dashboard DTOs contain only subject-scoped aggregate/booking data. No fabricated identities or constants. Client PII in provider dashboard is limited to clients with bookings at that provider's owned businesses.
- Errors never include stack traces, exception types, DynamoDB table/index names, AWS request details, JWT/reset/password values, raw request body, account existence, rival booking identity, or ownership truth.

All authentication, recovery and private responses use `Cache-Control: no-store`; set `Pragma: no-cache` for legacy clients. Apply a restrictive `Referrer-Policy: no-referrer` on the reset page and avoid third-party scripts/resources there.

## 6. Authentication, registration, email and password contract

### 6.1 JWT and sessions (H3)

- Production signing keys come from an external secret manager/environment, not tracked settings. Startup rejects null/empty/whitespace, known fallback/development values, and insufficient HS256 key material (<256 random bits). Prefer an asymmetric signing service/key with `kid` rotation when deployment supports it.
- Pin allowed algorithm(s); validate signature, issuer, audience, expiry and not-before; zero/small documented clock skew; reject unsigned/unexpected algorithms.
- `RequireHttpsMetadata` is true outside local development. TLS is mandatory.
- Access tokens are short lived (security default: 15 minutes). Claims contain opaque subject, immutable role, issued/expiry times and a session/security version—no email, phone, password state or unnecessary PII.
- Password reset, account deletion, role/security change increments the server-side security version or revokes all active sessions. A valid signature with stale version is denied.
- Login/registration response tokens are not placed in URLs or logs. Frontend storage strategy is a product/deployment decision; the security default is a Secure, HttpOnly, SameSite cookie or an in-memory bearer token, not long-lived localStorage.

### 6.2 Email normalization and atomic uniqueness

Every registration, login, forgot-password, and future email-change path calls the same versioned normalizer before lookup:

1. Reject control characters and invalid length; trim surrounding whitespace.
2. Unicode-normalize to NFC.
3. Apply the product's invariant lowercase rule to the whole address; canonicalize the domain consistently (including IDN handling if accepted).
4. Validate one syntactically acceptable address and store both display email and `EmailNormalized`.

`EmailNormalized` is the login/uniqueness key. Enforce uniqueness with a DynamoDB conditional write/transaction against a dedicated unique key/index item. The current scan, then save, is race-prone and cannot satisfy this contract. Two concurrent case/space-equivalent registrations produce at most one profile. Account deletion must define whether the normalized key remains reserved or may be reused.

### 6.3 Registration enumeration resistance

Security default: `POST /auth/register` returns the same `202 Accepted` envelope and comparable latency for a new or existing normalized email, sends an address-verification/continue-registration capability only to the address, and issues a session only after capability possession. Existing-address attempts send a non-alarming account-exists/recovery notice. Rate-limit by IP/device and a keyed hash of normalized email; never log raw addresses in abuse telemetry.

The existing product draft chooses immediate self-registration with no email verification and a 409 duplicate response. That response is an account-enumeration oracle. Product must either adopt the security default or formally accept this residual risk with abuse controls. It cannot be described as enumeration-resistant while status/body differ for existing email.

Server-controlled registration fields:

- Server creates profile ID, role claim, timestamps, password hash, email normalized key and session metadata.
- Request may select only allow-listed `client` or `provider` if open provider registration remains a product decision. Unknown case/values fail 400; no `admin`/array/custom role.
- Never bind a request directly to `Profile`; reject overposted `id`, `passwordHash`, `createdAt`, verification/session/security-version fields.

### 6.4 Password policy and storage

Security default for a no-MFA consumer product:

- Minimum 15 characters; permit at least 64 Unicode characters and spaces; no composition rules that reduce usability; reject breached/common passwords locally (optional HIBP k-anonymity is a separately approved privacy/product choice).
- Apply a byte-safe maximum before BCrypt. BCrypt has a 72-byte input limit: reject over-limit UTF-8 input or use a deliberately versioned, domain-separated prehash construction. Never silently truncate.
- Use one injected password-hasher service for registration, login, reset and future change-password. BCrypt cost is calibrated to the deployment (minimum agreed target 12 unless benchmark documents an equivalent safe setting) and stored in the encoded hash for upgrades.
- Passwords are never trimmed, normalized, logged, echoed, included in telemetry, or retained after hashing. Compare via the hasher; no raw SHA-256 password storage.
- Login returns identical generic 401 for unknown account/wrong password/unsupported stored state and uses rate limits/backoff without creating a username oracle.

The product draft's 8-character composition rule is a product compatibility choice, not the security default. Release owners must choose one documented rule applied identically to registration and reset; reset may never be weaker than registration.

## 7. Complete password-reset capability lifecycle (C5, M6)

1. **Request:** Normalize email. Return the same 200 generic body and comparable timing for known/unknown accounts. Queue mail asynchronously. Rate-limit IP/device/account-hash without exposing existence.
2. **Issue:** Generate 32 random bytes (256 bits) with a CSPRNG and base64url-encode without padding. Generate a token identifier/version. Creating a new token atomically supersedes all prior outstanding tokens for that account.
3. **Store:** Persist only `SHA-256(token)` (or keyed HMAC digest), never plaintext, as the lookup key; store user ID, issuance/expiry, generation, and `ConsumedAt`/used state. TTL cleanup is hygiene, not expiry enforcement.
4. **Expiry:** 30 minutes from server issuance. Compare using server UTC clock on validation and consumption.
5. **Deliver:** Send only to the registered address over the email service. Preferred browser link carries the capability in the URL fragment (`#token=...`), which is not sent in the HTTP request; the reset SPA immediately reads it, calls `history.replaceState`, and sends it only in HTTPS JSON bodies. If product insists on a query link, accept and document the increased proxy/history/referrer risk, redact it everywhere, use `Referrer-Policy: no-referrer`, load no third-party resources, and replace browser history immediately. API path/query transport is prohibited.
6. **Validate:** `POST /auth/validate-reset-token` receives JSON body. Hash and constant-time compare/lookup. Valid only if the latest generation, unexpired and unused. Validation is read-only and does not extend expiry. Response is generic and `no-store`.
7. **Consume:** `POST /auth/reset-password` atomically conditions on matching digest/generation, unused state and expiry, updates the password with the common hasher, marks consumed, and increments the account security version. Exactly one of two simultaneous attempts succeeds. Do not update password then separately mark token in an unprotected sequence.
8. **Replay/failure:** Unknown, malformed, superseded, expired, used and race-losing tokens receive one generic `reset_token_invalid` response with no state distinction. No password mutation occurs. A successful reset returns generic 200 and never echoes token/password.
9. **After success:** Revoke all sessions/access-token versions, invalidate all reset capabilities, send a confirmation/security notification, and audit event type/account ID/time/source metadata without token, password, raw email or request body.
10. **Logging:** Redact Authorization/Cookie headers and auth/recovery bodies globally. Do not log full paths or query strings. Never include reset capability in traces, exceptions, metrics labels, analytics or frontend console logs.

## 8. Error semantics and information-leak controls

All errors use `application/problem+json` with stable `code`, generic `title/detail`, status and opaque `traceId`. Field errors contain only safe validation messages for caller-supplied fields.

| Condition | External response |
|---|---|
| Missing/invalid/expired access token | 401 `authentication_required`; no token parser/signature detail. |
| Authenticated wrong role for a known capability | 403 `role_forbidden`; no ownership/resource detail. |
| Direct object absent or caller is not a party/owner | Identical 404 `resource_not_found` (`booking_not_found` may be used only if identical); prevents ID enumeration. |
| Authorized party, malformed input | 400 `validation_failed`; no internal identifiers/regex/stack data. |
| Authorized party, illegal/stale booking transition | 409 `booking_state_conflict`; no rival booking/client details. |
| Slot conflict | 409 `booking_slot_conflict`; do not reveal who owns the conflict. |
| Duplicate normalized email | Under enumeration-resistant registration: generic 202. If risk-accepted immediate registration: 409 is an acknowledged oracle. |
| Invalid/expired/used/superseded reset capability | Same 400/410 and same generic `reset_token_invalid` body for all states; no distinction. |
| Unexpected/persistence failure | Generic 500/503; details only in protected redacted server telemetry. |
| Test/Swagger route in Production | 404; do not advertise disabled diagnostics. |

Do not return `ValidationException` messages that say whether another user/provider/profile exists or reveal owner IDs. Authorization is evaluated before domain-state details where practical. State/validation failures must not disclose a resource to a non-party.

## 9. Sensitive logging, secrets and diagnostics (H3, M6, L1)

- Remove full path/query/origin/body logging from the current request middleware. Log route templates (for example `/bookings/{id}`), status, latency and opaque trace ID after redaction.
- Redact `Authorization`, `Cookie`, `Set-Cookie`, reset/verification/idempotency tokens, password fields, email/phone/address and JWTs. Never use PII or capability values as metric labels.
- Production exceptions never include stack traces or framework/DynamoDB details in responses.
- `TestController` and Swagger are conditionally not mapped in Production. Development diagnostics bind only to intended local/test environments and contain synthetic data.
- Secret scanning is mandatory for tracked files and generated artifacts. JWT/AWS/SES secrets use deployment secret stores. Rotate any real secret ever committed or logged; deletion from Git is not rotation.
- Security/audit events include actor subject, action, canonical resource ID, result, server time and trace ID where justified; access is restricted and retention is defined. Do not audit raw credentials/capabilities or unnecessary PII.

## 10. Exploit-path mapping and required negative tests

| Finding | Verified exploit path/root cause | Required control | Mandatory negative/adversarial tests |
|---|---|---|---|
| C2 | Anonymous `POST/PUT/DELETE /profiles`; provider cross-profile read; entity response leaks `PasswordHash`; body role/email overwrite | Fallback auth; self-only resolver; DTO allow-list; immutable role/email; remove profile create | Anonymous each profile route -> 401/no mutation; Client A read/update/delete B -> concealed 404; provider read B -> 404; overpost `userType`, `role`, `id`, `email`, `passwordHash`, `createdAt` -> 400 and unchanged; recursively assert no response/error contains hash. |
| C3 | Any client/provider calls generic booking PUT/DELETE on another booking; arbitrary status and `DateTime.MinValue` overwrite | Party resolver via booking/service/business; disable generic routes; explicit state actions/versioning/audit | Every action by non-party client/provider -> 404 unchanged; wrong-role party -> 403; unknown action/status -> 400; every illegal transition -> 409; status action preserves times byte-for-byte; old PUT/DELETE cannot mutate; two actions at same version -> one success. |
| C5 | Any non-empty validation token accepted; plaintext token key; SHA-256 password; separate save/use race; token in URL path | Full §7 lifecycle and one BCrypt service | Random/empty/malformed/expired/used/superseded token all same failure; valid reset then login succeeds; old password fails; replay fails; two concurrent resets exactly one succeeds; only digest stored; path/query API rejected; logs contain neither known canary token nor password. |
| H3 | Empty signing key accepted; predictable fallback; HTTPS metadata disabled | Startup validation, external key, algorithm/issuer/audience/lifetime checks, prod TLS | Start with null/empty/whitespace/fallback/short key -> process fails; forged wrong-key/alg/issuer/audience/expired token -> 401; stale security-version token after reset/delete -> 401. |
| H8 | Controllers open by omission; handler checks inconsistent; delete handlers lack ownership | Global fallback plus explicit route classification and authoritative ownership in handlers | Enumerate every route anonymously and compare to matrix; anonymous mutation -> 401; Client tries business/service/hour/review-owner operations -> 403/404; Provider A mutates B's business/service/hour -> 404 unchanged; review author rules and one-per-booking atomicity. |
| H11 | Client controls provider name/end/duration and current code stores business ID as provider ID; no historical snapshots | Minimal create DTO; resolve Service -> Business; derive all identities/times/status/snapshots | Submit extra `providerName`, provider/business/client IDs, end/status/price/duration/timestamps -> 400 (or ignored only if explicitly tested, with server values winning); own-service checks use business owner; price/name/duration changes later do not alter booking snapshot. |
| M6 | Public test routes/Swagger; reset token in path; full request path logged | Production unmapping; body transport; redaction | Production test/Swagger paths -> 404; canary token in path/query/body/header never appears in logs; error responses have no stack/table/path details; reset API rejects path/query token. |
| C1 / registration | Missing route; non-atomic duplicate check; body-selected role | Safe register route; allow-listed role; normalized atomic uniqueness; enumeration decision | Client/provider valid registration; unknown/admin/mixed role rejected; case/space/Unicode-equivalent concurrent attempts create at most one profile; overposting rejected; enumeration timing/status/body test according to adopted decision. |
| C4/H6 | Profile ID compared with business ID; frontend sends `user.id`; service orphaning | `Business.ProviderId` resolver; `/businesses/mine`; server-derived service provider | Provider with two businesses can manage each; profile ID as business ID -> 404; foreign business -> concealed 404; created service references real loaded business and derived provider; no orphan. |
| H1 | Pending excluded; check-then-save race | Atomic capacity claim for pending/confirmed; half-open interval; idempotency | Two concurrent overlapping creates -> exactly one success; pending and confirmed both conflict; terminal releases capacity; same idempotency key/same body replays, different body conflicts. |
| H9 | Fabricated/cross-scoped dashboard/client data | Real self/owned-business queries; minimal DTO/snapshots | Empty account -> zero/empty; Provider A cannot see B stats/clients; provider client list contains only own-business booking clients; mutable service price does not rewrite totals. |
| L1/PII | Full paths/origins/debug logs and direct entities | Structured route-template logs; redaction; DTOs | Automated canary scan across response/log/trace output for password, hash, JWT, reset token, email/phone; no match outside an explicitly protected test sink. |

## 11. Mandatory test suite and release gate

The following tests are required, not optional examples:

1. **Route inventory contract test:** reflect endpoint metadata, assert exactly one explicit classification per action, assert fallback policy, and compare all 43 current actions plus canonical routes to this matrix. A new unclassified route fails CI.
2. **Actor matrix integration tests:** anonymous/client/provider request every route; assert expected status and no side effect. Repeat object routes for self/owner, same-role non-owner, wrong role, absent ID and malformed ID.
3. **DTO serialization tests:** recursively deny properties matching password/hash/token/secret/security-version/internal reservation keys; snapshot public vs client-party vs provider-party shapes.
4. **Overposting tests:** send IDs, roles, owner IDs, email, password hash, timestamps, status, derived booking values and unknown JSON fields. Configure strict input handling or prove dangerous fields cannot bind/change state.
5. **Registration/login tests:** normalization equivalence, atomic duplicate concurrency, role allow-list, generic invalid credentials, password boundaries including BCrypt 72-byte behavior, common-password rule, hasher cost/upgrade path, and adopted enumeration behavior.
6. **Reset lifecycle tests:** entropy/format, digest-only persistence, latest-token supersession, expiry boundary, read-only validation, atomic single-use race, replay, BCrypt reset-login round trip, old password rejection, session revocation, confirmation notification and canary log scan.
7. **JWT/config tests:** startup failure matrix; wrong key/algorithm/issuer/audience/lifetime/not-before; malformed/missing/duplicate role/subject claims; stale session version.
8. **Business/service/hour/review authorization tests:** authoritative resolver chain, cross-provider IDOR, profile/business ID confusion, deleted/missing parent, ownership changed between read/write, one-review-per-completed-booking race.
9. **Booking tests:** every state-machine row for both roles; every unlisted transition; non-party concealment; generic routes disabled; derived-field spoofing; snapshot immutability; idempotency; expected-version race; pending/confirmed overlap concurrency; reschedule atomicity and no timestamp erasure.
10. **Dashboard/PII tests:** cross-tenant isolation, real empty state, snapshot-based totals, minimal client contact visibility.
11. **Production-surface tests:** Production `TestController`/Swagger 404; no development CORS/diagnostic behavior; HTTPS/auth metadata settings; standard ProblemDetails without internals.
12. **Logging/secret tests:** issue canary JWT, reset token, password, email and phone through success/failure paths; scan application/access/exception/trace output and frontend console capture; prohibited values must not appear.

Release CI must fail on any test failure. Backend currently has no test project; creating and enforcing this suite is therefore itself a blocker, not follow-up hardening.

## 12. Security defaults versus product decisions

### 12.1 Security defaults (apply unless made stricter)

- Authenticated fallback policy; only listed public reads/auth flows are anonymous.
- One immutable server role per account; no admin capability.
- Provider ownership through `Business.ProviderId`; object-party checks in backend.
- Explicit DTOs; no persistence entities or credential material in responses.
- Atomic normalized email uniqueness.
- Enumeration-resistant registration with email capability verification.
- 15-character no-MFA password minimum, common/breached denylist, one calibrated BCrypt service, no silent 72-byte truncation.
- 256-bit reset tokens, digest-only storage, 30-minute expiry, latest-token semantics, atomic single use, body-only API transport, session revocation.
- 15-minute access tokens and external rotatable signing keys.
- Concealed 404 for direct non-owner object access; generic auth/reset/errors.
- Production diagnostics unmapped; structured redacted logs; secrets external.
- Booking create is client-only; pending/confirmed consume capacity; state actions are explicit and versioned.

### 12.2 Product decisions requiring explicit confirmation (not frontend choices)

1. **Registration experience:** adopt verification/generic 202, or formally accept account enumeration from immediate-token/409 behavior.
2. Whether provider self-registration needs vetting/approval and whether email verification is mandatory for listing.
3. Whether a provider account may also book (current interim rule: no; separate client account).
4. Cancellation cutoff/fees and consequences of `no_show` (authorization/state safety does not wait for fee design).
5. Staff/resources/multi-provider business model (current boundary: one owning provider; business is the capacity owner).
6. Lead time, buffers, blocked dates, pending expiry and timezone expansion (atomic overlap protection remains mandatory).
7. Account deletion retention/cascade and whether normalized email becomes reusable.
8. Frontend session transport (security default cookie/in-memory; localStorage risk requires explicit acceptance and CSP/XSS controls).
9. Optional online HIBP lookup and associated privacy/availability handling.
10. Production Swagger operator need. Default is unmapped.

These decisions do not permit the frontend to enforce ownership, roles, lifecycle, uniqueness, reset consumption, or data minimization.

## 13. Implementation handoff checklist

- [ ] Route inventory matches §3/§4 and fallback authorization is active.
- [ ] Ownership resolver service is authoritative and used by every private handler.
- [ ] Public/party/owner DTOs satisfy §5.
- [ ] Registration/email/password/session controls satisfy §6, including the resolved enumeration decision.
- [ ] Reset lifecycle satisfies §7 atomically.
- [ ] Error middleware and logs satisfy §8/§9.
- [ ] Unsafe generic booking/profile/debug paths are removed or fail closed.
- [ ] Mandatory suite in §11 runs in CI and passes.
- [ ] `frontend/payment-reconciliation-mvp/` remains untouched.

No security boundary in this contract may be delegated solely to React route guards, hidden controls, client validation, local storage, body/path IDs, or denormalized entity fields.
