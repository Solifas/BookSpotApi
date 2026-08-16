# BookSpot — Target Security / Authorization Model & Sensitive-Data Rules

**Date:** 2026-08-09
**Author:** Cerberus (Security Engineer), BookSpot / Ubuntu Bookings Spot remediation
**Status:** DESIGN-ONLY — no source code modified.
**Source findings:** `docs/bookspot-baseline-report-2026-08-09.md` (26 verified findings: C1–C5, H1–H11, M1–M8, L1–L2).
**Scope guardrails:** `frontend/payment-reconciliation-mvp/` untouched; `backend/BookSpot.Application/BookSpot.Application.csproj` untouched.

---

## 0. Role model (as-designed)

The current codebase recognizes two roles only — **Client** and **Provider** — enforced via the existing `ClientOrProvider` policy (see C2/C3/H8). There is **no Admin role** in code today.

- The **Admin** column is reserved for a future constrained admin and is marked **N/A** where no admin capability exists today. Do **not** invent an admin path during remediation; if an admin is later needed it must be a distinct role with its own least-privilege policy, never granted by profile-update mutation.
- Roles are **authoritative-server-side claims** derived from the signed JWT, never from any client-supplied field (see §3, C2).

---

## 1. Authorization matrix

Legend:
- **A** = Allow, **D** = Deny
- Policy name in parentheses = the named authorization policy the route/operation must be gated by.
- Ownership policies (`Self*`, `Own*`) require a server-side check that the authenticated subject is the owner/party of the targeted resource — not merely an authenticated user (fixes C2/C3 IDOR).

| # | Operation | Anonymous | Client | Provider | Admin | Policy (target state) | Remediation driver |
|---|---|:--:|:--:|:--:|:--:|---|---|
| 1 | register | **A** | D | D | D | `P-Public` (credential issuance) | C1 (restore registration) |
| 2 | login | **A** | D | D | D | `P-Public` (credential exchange) | — |
| 3 | forgot-password | **A** | D | D | D | `P-Public` (token issuance; token in response body/email, never URL) | C5, M6 |
| 4 | reset-password | **A** | D | D | D | `P-Public` (consume single-use token via body) | C5, M6 |
| 5 | validate-reset-token | **A** | D | D | D | `P-Public` (token in request body/header, **never URL path**) | C5, M6 |
| 6 | own profile GET | D | **A** | **A** | N/A | `P-SelfProfile` | C2 |
| 7 | own profile PUT | D | **A** | **A** | N/A | `P-SelfProfile` (Role field stripped — see §3) | C2 |
| 8 | own profile DELETE | D | **A** | **A** | N/A | `P-SelfProfile` | C2 |
| 9 | other profile access | **D** | **D** | **D** | N/A | `P-NoCrossProfile` (no cross-user read/write) | C2 |
| 10 | browse services | **A** | **A** | **A** | N/A | `P-Public` | — |
| 11 | create booking | D | **A** | D | N/A | `P-ClientOnly` (client books a provider service) | C3, H11 |
| 12 | accept booking | D | D | **A** | N/A | `P-ProviderOnly` + `P-OwnBooking` | C3 |
| 13 | decline booking | D | D | **A** | N/A | `P-ProviderOnly` + `P-OwnBooking` | C3 |
| 14 | cancel booking | D | **A** | **A** | N/A | `P-OwnBooking` (subject is client-who-booked OR provider-of-service) | C3 |
| 15 | reschedule booking | D | **A** | **A** | N/A | `P-OwnBooking` (ownership + state-transition allow-list) | C3, H1 |
| 16 | complete booking | D | D | **A** | N/A | `P-ProviderOnly` + `P-OwnBooking` | C3 |
| 17 | view client bookings | D | **A** | D | N/A | `P-SelfBookings` (client sees only own) | C3 |
| 18 | view provider bookings | D | D | **A** | N/A | `P-OwnBookings` (provider sees only own-business bookings) | C3 |
| 19 | business GET (browse) | **A** | **A** | **A** | N/A | `P-Public` | H8 |
| 20 | business POST (create) | D | D | **A** | N/A | `P-ProviderOnly` + `P-OwnBusiness` (new business owned by subject) | H6, C4 |
| 21 | business PUT (update) | D | D | **A** | N/A | `P-ProviderOnly` + `P-OwnBusiness` (subject owns business id) | H6, C4 |
| 22 | service create | D | D | **A** | N/A | `P-ProviderOnly` + `P-OwnBusiness` (businessId must equal subject's business) | C4, H6 |
| 23 | dashboard | D | **A** | **A** | N/A | `P-DashboardSelf` (role-appropriate, no fabricated data — H9) | H9 |
| 24 | settings | D | **A** | **A** | N/A | `P-SelfProfile` (settings bound to own account/business) | H6, C4 |
| 25 | test/debug endpoints (TestController) | **D** | **D** | **D** | N/A | `P-DevOnly` (disabled unless `Environment != Production`) | M6 |

### Policy definitions (named)

- **`P-Public`** — No authentication required. Applies only to truly public surfaces (register, login, password recovery, service/business browsing). Everything else is deny-by-default.
- **`P-AuthAny`** — Any authenticated (valid JWT) principal.
- **`P-ClientOnly`** — Authenticated **and** `role == Client`.
- **`P-ProviderOnly`** — Authenticated **and** `role == Provider`.
- **`P-ClientOrProvider`** — Authenticated and role is Client or Provider (existing policy; retained but **never sufficient alone for mutations** — pair with an ownership policy).
- **`P-SelfProfile`** — Authenticated and `targetProfileId == subjectId`. Request `Role` field is ignored/stripped server-side (fixes C2 role manipulation). DTO never includes `PasswordHash`.
- **`P-NoCrossProfile`** — Hard deny for any operation targeting a profile id ≠ subject id (fixes C2 anonymous + cross-user profile admin).
- **`P-OwnBooking`** — Authenticated and subject is a party to the booking: the client who created it, or the provider owning the service's business. All booking mutations (accept/decline/cancel/reschedule/complete) require this (fixes C3 IDOR).
- **`P-OwnBusiness`** — Authenticated and `business.OwnerId == subjectId`. Business create/update and service-create must use this, never compare `businessId == currentUserId` as a profile-id conflation (fixes C4/H6 — profile id is NOT business id).
- **`P-SelfBookings` / `P-OwnBookings`** — Read-scoped ownership: client sees own bookings; provider sees bookings for businesses they own. No cross-tenant listing.
- **`P-DashboardSelf`** — Authenticated and returns only data scoped to the subject's role and id; no hard-coded/fabricated figures (fixes H9).
- **`P-DevOnly`** — Allowed **only when** `IHostEnvironment.IsProduction() == false`. In Production, routes are removed/return 404 — never 403-with-detail. Applies to `TestController` (fixes M6).

---

## 2. Authentication requirements

1. **JWT secret must be non-empty and fail-closed (fix H3).**
   - `appsettings.json` `Jwt:SecretKey` must be a strong, non-empty, environment-supplied secret (never a code fallback).
   - **Fail-closed:** if `SecretKey` is null/empty/whitespace at startup, the application **must refuse to start** (throw during `Program.cs` host build), not substitute a predictable `"fallback"`. No `?? "fallback"` for the signing key.
   - Validate key length/entropy; reject keys below the algorithm's minimum (e.g. HS256 ≥ 256 bits).
2. **Consistent BCrypt password hashing (fix C5).**
   - `ResetPasswordCommand` and every password write must use the **same** BCrypt work-factor and verify path as `register`/`login`. Remove the SHA-256 branch. A reset password must subsequently verify at login.
3. **Reset tokens must be expiring, single-use, and non-leaking (fix C5).**
   - Tokens are cryptographically random (≥ 128-bit), stored server-side (or signed + stateful), with a short TTL (e.g. 15–30 min).
   - **Single-use:** validated token is invalidated immediately on first successful `reset-password` (and on `validate-reset-token` consumption if that consumes it — prefer validate-as-readonly but still expiry-bound).
   - **Non-leaking:** token is returned only to the account's verified email and in the reset **request body**, never logged, never in a URL path.
4. **Reset token must NOT be placed in the URL path (fix M6).**
   - Replace `GET validate-reset-token/{token}` and token-in-path reset flows with token supplied in the **request body** (or an `Authorization`/custom header). URL paths are persisted in browser history, proxies, and logs.
5. **Token validation correctness.**
   - `validate-reset-token` must verify signature/expiry/single-use, not merely "non-empty string" (the current empty-string-only check is the C5 vulnerability).

---

## 3. Sensitive-data rules

1. **`PasswordHash` is never present in any response DTO.**
   - The `Profile` entity must never be returned directly. Map to a dedicated read DTO that excludes `PasswordHash` and any credential material (fixes C2 leak).
2. **Role cannot be changed via normal profile update (fix C2).**
   - `P-SelfProfile` (PUT) must **ignore** any `Role`/`role` field in the inbound request. Role is an authoritative server-side claim; privilege changes require a separate, admin-gated, audited flow that does not exist in the current model. A client must not be able to self-escalate to Provider/Admin.
3. **Ownership checks required for all profile/booking/business mutations (fix C2/C3 IDOR).**
   - Every mutation reads the subject id from the authenticated principal, never from a client-supplied id that is then trusted.
   - Profile mutations: `targetId == subjectId` (else `P-NoCrossProfile`).
   - Booking mutations: `P-OwnBooking`.
   - Business/Service mutations: `P-OwnBusiness` — and the ownership comparison must use the **business owner id**, not the profile id (fixes C4/H6 id conflation).
4. **No fabricated/simulated sensitive data in production responses (fix H9).**
   - Dashboard/stats endpoints return real, subject-scoped data only. Remove hard-coded totals/clients.

---

## 4. Test / debug endpoints (TestController — fix M6)

- `TestController` (`test/exception/{type}`, `test/validation-details`, and any sibling debug routes) currently has **no `[Authorize]`** and **no dev-only gating**.
- **Target:** gate behind `P-DevOnly`. In `Program.cs`, conditionally **map these endpoints only when not production** (or return 404 in Production). Never expose stack traces, validation internals, or request-origin details to anonymous/unauthenticated callers in Production.
- Also address the related M6/L1 logging concern out of pure scope: `Program.cs` request logging must not emit full paths/origins/methods that can leak sensitive routing or reset-token paths. (Logging hardening is recommended alongside the auth fixes; the matrix's hard requirement is the endpoint gating.)

---

## 5. Global fail-closed authorization policy (recommended)

Adopt an **authenticated-by-default** posture: the application should start from "deny everything" and explicitly opt routes into openness.

Recommended ASP.NET Core implementation intent (design, not code):
- Apply a **global `[Authorize]`** (or a default authorization policy requiring an authenticated user) at the **app/middleware/controller base** level so that any endpoint without an explicit `[AllowAnonymous]` is denied to anonymous callers.
- Per-route, downgrade only the genuinely public surfaces (register, login, forgot/reset/validate-reset-token, browse services, browse businesses) with explicit `[AllowAnonymous]`.
- Mutating and read-sensitive routes additionally require a **named policy** (`P-SelfProfile`, `P-OwnBooking`, `P-OwnBusiness`, `P-ClientOnly`, `P-ProviderOnly`) — authentication alone is never sufficient for a mutation.
- No controller (including `TestController`, `AuthController` mutating actions, `BookingsController`, `BusinessHoursController`, `ReviewsController`, `ServicesController`, `LocationsController`) ships "open by omission." This directly closes the C2/H8/M6 root cause: *"No global authenticated-by-default policy."*
- A startup self-check should assert that no `[AllowAnonymous]`-decorated action performs a state-changing mutation on profile/booking/business data.

---

## 6. Abuse cases to verify post-fix

These are the regression tests / adversarial checks that must pass after remediation:

1. **Anonymous profile admin (C2).**
   - `POST/PUT/DELETE /profiles/{id}` as anonymous → **401/403**, no mutation, no `PasswordHash` in any error or success body.
2. **Role escalation (C2).**
   - Authenticated Client sends `PUT /profiles/me` with `role:"Provider"` (or `role:"Admin"`) → role unchanged; response omits `PasswordHash`; subsequent calls confirm role still Client.
3. **IDOR on bookings (C3).**
   - Client A `PUT /bookings/{id-owned-by-B}` (accept/decline/cancel/reschedule/complete) → **403**; booking state unchanged. Provider P updates a booking for a service they do not own → **403**.
4. **IDOR on profiles (C2).**
   - Client A `GET/PUT/DELETE /profiles/{id-of-B}` → **403/404** (no cross-profile read or write).
5. **IDOR on business/service (C4/H6).**
   - Provider P `POST /services` or `PUT /businesses/{id}` with a `businessId`/`id` not owned by P → **403**; the system never conflates profile id with business id.
6. **Reset-token abuse (C5/M6).**
   - Empty/guessable token → `validate-reset-token` returns invalid.
   - Reused token (after one successful reset) → rejected as single-use.
   - Expired token → rejected.
   - Token passed in URL path → no such route; token accepted only in body; token never appears in logs/response except to the verified email.
7. **Debug-endpoint access (M6).**
   - In Production: `GET /test/exception/...` and `/test/validation-details` → **404** (not 403-with-detail, not 200). In Development only, reachable by local caller.
8. **JWT fail-closed (H3).**
   - Empty/`fallback` `Jwt:SecretKey` → application **fails to start**; no token can be signed/validated with a weak key.
9. **Password hashing consistency (C5).**
   - Account created via register, then reset via reset-password, then login → login **succeeds** (BCrypt path consistent both ways).

---

## 7. Mapping to baseline root causes

- Root cause #2 (*"No global authenticated-by-default policy"*) → §1 matrix + §5 global policy.
- Root cause #1 (*identity conflation profile↔business*) → §1 rows 20–22 + §3 rule 3.
- Root cause #3 (*missing server-side validation/derivation, wrong hash algorithm*) → §2 rules 2–3.
- Finding M6 (public diagnostics, token-in-URL, no TestController auth) → §2 rule 4, §4, §6 cases 6–7.
- Finding C2 (anonymous profile admin, `PasswordHash` leak, role manipulation) → §1 rows 6–9, §3 rules 1–2, §6 cases 1–2.
- Finding C3 (booking IDOR, status overwrites) → §1 rows 11–18, §3 rule 3, §6 case 3.
