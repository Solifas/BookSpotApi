# BookSpot — Product Requirements, Acceptance Criteria & Human Decisions
**Author:** Sophia (Product / Requirements Analyst) · **Date:** 2026-08-09 · **Analysis only — no code changed.**
Evidence base: `docs/bookspot-baseline-report-2026-08-09.md` + source read of `backend/BookSpot.Domain/Entities/*`, `RegisterCommandValidator.cs`, `Program.cs` policies, `frontend/src/pages/Register.tsx|Settings.tsx`.

## Code-grounded facts driving these decisions
- `Profile.UserType` is a single string; validator + handler allow exactly `"client" | "provider"` (RegisterCommandValidator.cs:50-52). JWT carries claim `user_type`; policies `Client`, `Provider`, `ClientOrProvider` (Program.cs:85-93).
- `Business.ProviderId` exists → the schema **already** models business ownership by provider; C4/H6 are bugs against the existing schema, not a modelling gap.
- `Service` carries both `BusinessId` and `ProviderId` (denormalised) → ownership must be resolved via Business, not by comparing to the user id.
- `Booking.Status` is a free string defaulting to `"pending"`; no enum, no transitions, `CreatedAt` only (no UpdatedAt).
- `PasswordResetToken` already has `ExpiresAt` + `IsUsed` → single-use/expiry are implementable today; C5 is enforcement, not schema.
- Register UI already offers a client/provider toggle (Register.tsx:10) — an account is one or the other today.

---

## 1. Decisions table

| # | Question | Recommended default | Rationale | Call |
|---|---|---|---|---|
| 1 | Role model | Keep single `userType` per account: `client` \| `provider`, mutually exclusive. Both selectable at registration. Store as lowercase; treat as immutable after creation. | Matches entity, validator, JWT claim and all policies. Dual-role would require re-issuing claims and re-auth across every policy — a product change, not a fix. | **ADOPT** (dual-role support = ESCALATE #E1) |
| 2 | Providers booking other providers | **No** for MVP. `POST /bookings` requires policy `Client`. A provider needing to book must use a separate client account. | Cheapest consistent rule under a mutually-exclusive role model; avoids ambiguous "my bookings" views. | **ESCALATE #E2** (business-desirable, low cost to allow) |
| 3 | Booking lifecycle | Enum (lowercase, persisted as string): `pending`, `confirmed`, `declined`, `cancelled`, `completed`, `no_show`. Transitions below. | Superset of the string values already in use; closes C3. | ADOPT for enum + transitions; **ESCALATE #E3** for cancellation-window/no-show policy |
| 4 | Provider→Business→Service | Provider may own **many** businesses. A business has **exactly one** provider owner (`Business.ProviderId`) — no multi-staff in MVP. **Service is owned by a Business**; `Service.ProviderId` is a denormalised copy of `Business.ProviderId`, never the authorization source. Authorization = load Business by `Service.BusinessId`, assert `Business.ProviderId == currentUserId`. | Directly fixes C4/H6 without schema change. Multi-provider businesses (staff/resources) is a real product feature. | ADOPT; **ESCALATE #E4** (multi-provider/staff businesses) |
| 5 | Email & password policy | Email: trim, lowercase, NFC-normalise, RFC-ish single-`@` validation, ≤100 chars; the lowercased form is the uniqueness key and login key. Password: keep current rule (≥8, ≤100, upper+lower+digit+special) but widen the allowed special-character set to any non-alphanumeric; add a top-1000 common-password denylist. BCrypt (work factor ≥11) everywhere, no exceptions. | Uniqueness bugs and "can't log in with the email I registered" come from unnormalised case. Current regex silently rejects legitimate specials. Breached-password API (HIBP k-anonymity) is optional. | **ADOPT** (HIBP online check = ESCALATE #E5, optional) |
| 6 | Self-registration | Both `client` and `provider` self-register from the frontend, no gating, no email verification for MVP. Provider gets an account immediately but **no business exists until they create one**; provider dashboard shows an onboarding prompt until then. | Unblocks C1 with the flow the UI already implements. | ADOPT; **ESCALATE #E6** (provider vetting/approval, email verification) |
| 7 | Password recovery | Token = 256-bit cryptographically-random, URL-safe, stored **hashed**; delivered only by email via SES to the registered address; **30-minute** expiry; **single-use** (`IsUsed` set atomically at consumption); token passed in request **body**, never in the URL path (fixes M6); `forgot-password` always returns 200 with a generic message (no account enumeration); all outstanding tokens invalidated + sessions considered stale on successful reset; reset re-hashes with **BCrypt** (fixes C5). Reset UI: token from query string, new password + confirm, live policy hints, distinct errors for invalid/expired/used, redirect to login on success. | Standard, and every field needed already exists on `PasswordResetToken`. | **ADOPT** (expiry length is tunable; 30 min chosen over 60 for security) |
| 8 | Dashboard scope | **Client:** upcoming bookings (next N, real), past bookings, total bookings count, total spent (sum of completed bookings' price snapshot), pending requests awaiting provider action, quick rebook. **Provider:** today's bookings, pending requests needing accept/decline, this-week schedule, monthly revenue (completed only), active services count, recent clients derived from real bookings (never a fabricated list). Every number must be traceable to a query; empty state = zeroes with an empty-state UI, **never** placeholder data. | Replaces H9's hard-coded `15` / `1250.00` / `client-001…005` and aligns `ClientStatsDto` with what the client UI renders. | **ADOPT** (revenue definition = ESCALATE #E7) |

### 3a. Booking state machine (canonical)

| From → To | Action | Permitted actor |
|---|---|---|
| — → `pending` | Create | Client (owner of `ClientId`) |
| `pending` → `confirmed` | Accept | Provider owning the service's business |
| `pending` → `declined` | Decline | Provider |
| `pending` → `cancelled` | Cancel | Client **or** Provider |
| `confirmed` → `cancelled` | Cancel | Client **or** Provider (see cancellation window, E3) |
| `confirmed` → `completed` | Complete | Provider only, and only when `EndTime <= now` |
| `confirmed` → `no_show` | Mark no-show | Provider only, after `StartTime` |
| `pending`/`confirmed` → same status, new times | Reschedule | Client or Provider; a reschedule always resets status to `pending` and re-runs the conflict check |
| `declined`, `cancelled`, `completed`, `no_show` | — | **Terminal.** Any transition out = 409 |

Rules: transitions validated server-side against this table; actor derived from JWT, never from the request body; unlisted transition → `409 Conflict`; non-owner → `403` (`404` if the caller may not know the booking exists); status-only updates must **not** touch `StartTime`/`EndTime` (C3); every transition stamps `UpdatedAt` and appends an audit entry (actor, from, to, at).

---

## 2. Acceptance criteria

### Registration (C1)
- Given a valid client payload, when `POST /auth/register`, then 201 with `{token, id, email, fullName, userType}` and **no** `passwordHash`; profile persisted with BCrypt hash and lowercased email.
- Same for `userType: "provider"`.
- Given an email differing only by case/whitespace from an existing account, then 409 (duplicate) — not a second account.
- Given `userType` not in {client, provider}, then 400 with a field-level error.
- Given a password failing policy, then 400 listing the unmet rules; the password is never logged or echoed.
- The returned token authenticates immediately against an authorized endpoint (no second login required).
- Frontend `Register.tsx` submits to the implemented route and no longer 404s.

### Login continuity
- Given an account created via register, when logging in with the same credentials (any letter case in the email), then 200 + token whose `user_type` claim equals the stored `UserType`.
- Wrong password / unknown email → 401 with an identical generic message (no enumeration).
- Response body contains no `passwordHash`.

### Password recovery end-to-end (C5)
- `POST /auth/forgot-password` with a known **or** unknown email → 200, identical generic body; an email is sent only for known accounts.
- Token validation: unknown/empty/expired/already-used token → invalid; **only** a token that exists, is unexpired and unused → valid. (Fixes "any non-empty token is valid".)
- `POST /auth/reset-password` with a valid token + policy-compliant password → 200; the new password authenticates at login (proves BCrypt, not SHA-256); reusing the same token → 400/410.
- Token expires after 30 minutes; expiry and single-use are enforced server-side, verified by test.
- Tokens are never present in a URL path or server log.

### Profile read/update/delete (C2)
- `GET /profiles/me` returns the caller's profile **without** `PasswordHash` (public DTO).
- `POST`/`PUT`/`DELETE` on profiles without a token → 401 (currently anonymous).
- `PUT /profiles/{id}` where `id != currentUserId` → 403/404; a self-update succeeds.
- `userType`, `id`, `email`(if verification added), `passwordHash` and `createdAt` supplied in an update body are **ignored** — no privilege escalation via profile update.
- Update of only `fullName` leaves `contactNumber` and all other fields intact.
- `DELETE` self → 204 and the token is unusable afterwards; deleting another user → 403.

### Business CRUD (C4/H6)
- Creating a business sets `ProviderId = currentUserId` from the JWT, ignoring any body-supplied provider id; response returns a **business id ≠ profile id**.
- `GET /businesses/{businessId}` with a profile id → 404 (surfaces the H6 misuse instead of silently succeeding).
- A provider can list *their* businesses via a provider-scoped endpoint; Settings loads by that business id, not `user.id`.
- Update/delete allowed only when `Business.ProviderId == currentUserId`; otherwise 403.
- Clients cannot create businesses → 403.

### Service CRUD
- Creating a service requires a `businessId`; the server loads the Business and asserts `ProviderId == currentUserId` — the check is **never** `businessId == currentUserId` (removes C4).
- `Service.ProviderId` is set server-side from the owning business, not from the request.
- Update/delete permitted only to the owning provider; other providers → 403.
- Deactivating a service hides it from search but preserves existing bookings.

### Booking creation (H1/H4/H5/H11)
- Client submits `{serviceId, startTime}`; the server derives `providerId`, `businessId`, `endTime` (from `Service.DurationMinutes`), `providerName` and a **price snapshot** — client-supplied values for these are ignored.
- Given an existing `pending` **or** `confirmed` booking overlapping the slot for the same provider, the second request → 409 (fixes H1: pending must count as a conflict).
- Two concurrent identical requests → exactly one succeeds (conditional write / concurrency test).
- Start time in the past, or outside the business's hours, → 400.
- Status is forced to `pending`; `CreatedAt` set server-side.
- Frontend booking modal actually awaits `POST /bookings` and only shows success on a 2xx (fixes H5); the "Book Now" path resolves to a real route with a real availability source (fixes H4/H7).

### Booking lifecycle actions
For each row of the state machine: given the permitted actor and a legal source status → 200 with the new status, `UpdatedAt` changed, `StartTime`/`EndTime` **unchanged** (C3). Plus:
- Any authenticated user who is neither the booking's client nor the owning provider → 403/404 on every action (closes C3's missing ownership check).
- A status-only update never writes `0001-01-01` timestamps.
- Illegal transition (e.g. `completed` → `pending`, client attempting Accept or Complete) → 409/403.
- Reschedule re-runs the conflict check and returns to `pending`.
- Complete is rejected before `EndTime`.

### Settings persistence (H6/M3)
- Settings loads the provider's real business by business id and displays persisted values; a reload after save shows the saved values (round-trip proven).
- Business hours edited in Settings persist to `business_hours` and drive availability.
- Non-functional controls (`+ Add Service`, `Remove`) are either wired to real endpoints or removed — no dead buttons.
- Clients never see provider-only settings copy.

### Dashboard data (H9)
- No response field is a literal constant: a brand-new account's dashboard shows zeroes/empty states, not `15` / `1250.00` / `client-001`.
- Client stats fields match what the client dashboard renders (contract test frontend↔backend).
- Provider stats recompute after a booking is created/confirmed/completed within the same session.
- "Recent clients" is derived from actual bookings for that provider; a provider with no bookings sees an empty list.
- All dashboard endpoints require auth and are scoped to the caller — no cross-tenant data.

---

## 3. Escalation list (genuine human/product decisions only)

| ID | Decision needed | Why it's not an engineering default | Interim assumption |
|---|---|---|---|
| **E1** | May one account be both client and provider (dual role)? | Changes the auth claim model, every policy, and navigation/IA. | Mutually exclusive |
| **E2** | May providers book other providers' services? | Pure business-model call. | No (separate account required) |
| **E3** | Cancellation policy: how late may a **client** cancel a `confirmed` booking (free window / cut-off / fee)? Is `no_show` tracked, and does it carry consequences? | Commercial policy with revenue and trust implications. | Both parties may cancel any time before `StartTime`; no fees; `no_show` recorded but inert |
| **E4** | Can a business have multiple providers/staff members (bookings against a specific staff member or resource)? | Materially changes the data model and booking/availability logic. | One provider per business |
| **E5** | Reject breached passwords via an external HIBP lookup? | Adds a third-party runtime dependency and a privacy consideration. | Local common-password denylist only |
| **E6** | Provider onboarding gating — email verification, document/vetting, or admin approval before a provider can be listed publicly? | Trust & safety / marketplace-quality policy. | None; immediate self-service |
| **E7** | Revenue definition on the provider dashboard: recognise on `completed` only, or on `confirmed`? Gross vs net of any future fees? Currency & timezone for "today"/"this month". | Financial reporting semantics. | Completed only, gross, ZAR, Africa/Johannesburg |
| **E8** | Availability rules: booking lead time, buffer between bookings, blocked dates/holidays, timezone handling. | Operational policy; drives the availability endpoint replacing H7's hard-coded slots. | Business hours only, no buffer, no lead time |
| **E9** | Is the frontend `mock` data-source mode a deliberate demo feature or dead code to remove? | Affects scope and the definition of "real data" for H9. | Treat as dead code |

**Not escalations (adopted defaults):** email normalisation, password policy & BCrypt, register-route existence, DTOs excluding `passwordHash`, ownership checks, status enum & transition table, pending-counts-as-conflict, server-side derivation of booking fields, token expiry/single-use, generic no-enumeration responses, dashboard queries returning real data.
