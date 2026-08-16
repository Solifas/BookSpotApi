# BookSpot canonical domain model and incremental API contract

**Date:** 2026-08-10
**Status:** Architecture draft / ADR; design only
**Scope:** Main ASP.NET Core 8 backend and React/TypeScript frontend. `frontend/payment-reconciliation-mvp` is excluded.
**Preservation:** This document does not authorize changes to the pre-existing dirty `backend/BookSpot.Application/BookSpot.Application.csproj` or to unrelated concurrent work.

## 1. Decision summary

1. The canonical ownership chain is `Profile(provider) 1 -> many Business 1 -> many Service 1 -> many Booking`, while a client `Profile` is the booking customer.
2. `profileId` is the generic profile identity. `providerProfileId` and `clientProfileId` are role-qualified profile identities. Neither is a business id.
3. `Business.ProviderId` already stores the provider profile id. Therefore the profile/business conflation is principally an enforcement and call-site bug, not a missing relationship.
4. `Service.BusinessId` is the authoritative service ownership link. `Service.ProviderId` is denormalized and may be returned as `providerProfileId`, but authorization must resolve through the business.
5. Existing `Booking.ProviderId` cannot be trusted or renamed in place: current booking creation writes a business id into it. Add canonical `BusinessId` and `ProviderProfileId` attributes, retain the legacy field during migration, and authorize by resolving `Booking.ServiceId -> Service.BusinessId -> Business.ProviderId`.
6. Use self-scoped endpoints for private data (`/profiles/me`, `/businesses/mine`, `/bookings/client/me`, `/bookings/provider/me`, `/dashboard/me`). Keep current id-scoped routes temporarily as compatibility aliases with server-side subject matching.
7. Replace the unsafe generic booking `PUT` contract with an explicit booking-action contract. Status-only actions never contain or overwrite times; reschedule is the only action that changes time.
8. There is no separate `Account` or `Settings` domain aggregate. Account operations use `/profiles/me`; the Settings screen composes profile, business, service, and business-hours contracts. Do not add a large settings endpoint.
9. Public and private responses use DTOs, never DynamoDB entities. In particular, no response contains `PasswordHash`.
10. Booking persistence must expose atomic create/transition/reschedule interfaces and the invariants in this document. The DynamoDB mechanism is owned by Pixel and is not redesigned here.

## 2. Evidence from the current implementation

### 2.1 Entity relationships and identifier ambiguity

- Profile is keyed by `Profile.Id`; role is the free string `UserType`; `PasswordHash` is on the persistence entity (`backend/BookSpot.Domain/Entities/Profile.cs:5-27`).
- Business is keyed by `Business.Id` and owned by `Business.ProviderId` (`backend/BookSpot.Domain/Entities/Business.cs:5-24`).
- Service is keyed by `Service.Id`, links to `BusinessId`, and also stores denormalized `ProviderId`/`ProviderName` (`backend/BookSpot.Domain/Entities/Service.cs:5-48`).
- Booking is keyed by `Booking.Id`, links to `ServiceId` and `ClientId`, and has an ambiguous `ProviderId` (`backend/BookSpot.Domain/Entities/Booking.cs:5-33`).
- The current create handler writes `service.BusinessId` into `Booking.ProviderId`, proving that this booking field currently contains a business id despite its name (`backend/BookSpot.Application/Features/Bookings/Commands/CreateBookingCommand.cs:82-99`).
- The provider booking route describes its path argument as a provider user id (`backend/BookSpot.API/Controllers/BookingsController.cs:39-60`), while the repository scans the ambiguous `ProviderId` attribute (`backend/BookSpot.Infrastructure/Repositories/DynamoDb/BookingRepository.cs:34-42`). Those two meanings do not line up.

### 2.2 Actual frontend call sites

- Auth state treats `AuthResponse.userId` as the profile id and restores the session from `/profiles/me` (`frontend/src/contexts/AuthContext.tsx:41-56,69-110`).
- Settings incorrectly sends `user.id` (profile id) to `getBusiness`, `getBusinessServices`, and `updateBusiness` (`frontend/src/pages/Settings.tsx:62-93,109-138,193-205`).
- BookingPage names the route parameter `providerId`, but uses it as a business id when filtering services and loading a business (`frontend/src/pages/BookingPage.tsx:30-33,56-99`).
- BookingPage uses a hard-coded slot list rather than server availability (`frontend/src/pages/BookingPage.tsx:131-135`).
- The frontend create type currently accepts client-controlled `endTime` and `providerName` (`frontend/src/types/api.ts:98-123`); the booking page supplies profile/contact presentation fields that must not be authoritative (`frontend/src/pages/BookingPage.tsx:137-156`).
- The API client expects registration although the controller has no route (`frontend/src/services/api.ts:105-131`; `backend/BookSpot.API/Controllers/AuthController.cs:24-120`).
- The frontend search type expects a paged envelope while the controller returns a bare array (`frontend/src/types/api.ts:249-268`; `backend/BookSpot.API/Controllers/ServicesController.cs:34-67`).

### 2.3 Security and lifecycle defects that shape the target

- Profile mutating actions are anonymous and return `Profile` entities (`backend/BookSpot.API/Controllers/ProfilesController.cs:24-74`).
- The profile update command can overwrite email and role (`backend/BookSpot.Application/Features/Profiles/Commands/UpdateProfileCommand.cs:7-23`).
- Business create derives its owner from claims correctly (`backend/BookSpot.Application/Features/Businesses/Commands/CreateBusinessCommand.cs:34-72`), while service create incorrectly compares `businessId` directly with the current profile id (`backend/BookSpot.Application/Features/Services/Commands/CreateServiceCommand.cs:40-96`).
- Service delete performs no ownership check (`backend/BookSpot.Application/Features/Services/Commands/DeleteServiceCommand.cs:8-18`).
- Booking update overwrites start, end, and status together with no ownership or transition rule (`backend/BookSpot.Application/Features/Bookings/Commands/UpdateBookingCommand.cs:7-24`).
- The current global exception handler already emits RFC 7807 `application/problem+json`, but has no 403/409 domain mapping or stable machine code (`backend/BookSpot.Infrastructure/Middleware/GlobalExceptionHandler.cs:19-69`).
- Client dashboard values are fabricated (`backend/BookSpot.Application/Features/Dashboard/Queries/GetClientStatsQuery.cs:40-100`). Provider revenue reads mutable current service prices rather than a booking snapshot (`backend/BookSpot.Application/Features/Dashboard/Queries/GetProviderDashboardStatsQuery.cs:58-85`).
- Availability cannot be queried by business because `IBusinessHourRepository` only supports item-id get/save/delete (`backend/BookSpot.Application/Abstractions/Repositories/IBusinessHourRepository.cs:5-10`).

## 3. Canonical domain model

```text
Profile
  id: profileId
  userType: client | provider

Profile(provider)
  1 ---- owns ---- * Business
                      id: businessId
                      providerProfileId = persisted Business.ProviderId
                      1 ---- offers ---- * Service
                                           id: serviceId
                                           businessId
                                           providerProfileId (denormalized only)
                                           1 ---- selected by ---- * Booking

Profile(client)
  id: clientProfileId ---- creates ---- * Booking

Booking
  id: bookingId
  clientProfileId
  providerProfileId
  businessId
  serviceId
```

### 3.1 Identifier dictionary

| Contract name | Meaning | Source of truth | Never use as |
|---|---|---|---|
| `profileId` | Any user profile id | `Profile.Id` / JWT subject | business or service id |
| `providerProfileId` | Profile id whose immutable role is provider | `Business.ProviderId`; JWT subject for owner | `businessId` |
| `clientProfileId` | Profile id whose immutable role is client | authenticated JWT subject on create | request-controlled owner |
| `businessId` | Provider-owned marketplace business | `Business.Id`; `Service.BusinessId` | profile id |
| `serviceId` | Bookable service | `Service.Id` | capacity owner without resolving business |
| `bookingId` | Stable booking identity | `Booking.Id`, server generated | idempotency key |

All identifiers are opaque, non-empty, case-sensitive JSON strings. The server currently generates GUID strings (`CreateBusinessCommand.cs:56-69`, `CreateServiceCommand.cs:79-93`, `CreateBookingCommand.cs:89-99`); clients must not parse or infer UUID structure.

### 3.2 Cardinality and invariants

- A profile has one immutable role for the current product: `client` or `provider`.
- A provider profile may own zero or many businesses.
- A business has exactly one provider owner. Multi-staff/multi-provider businesses remain a product extension, not part of this remediation.
- A service belongs to exactly one business. Its `providerProfileId` is derived from that business.
- A booking belongs to exactly one service and one client profile. Its business/provider identities are captured server-side from the service chain.
- `pending` and `confirmed` bookings consume capacity. Terminal statuses do not.
- Authorization always resolves authoritative relationships. Denormalized display/name/id fields are not authorization inputs.

### 3.3 Persistence changes: additive only

Add to booking rows, without renaming or deleting existing attributes during the compatibility window:

| Attribute | Nullability for new rows | Purpose |
|---|---:|---|
| `BusinessId` | required | Canonical capacity and business boundary |
| `ProviderProfileId` | required | Canonical provider profile identity |
| `PriceAmountSnapshot` | required | Immutable price used for dashboards/history |
| `Currency` | required, default `ZAR` | Snapshot currency |
| `UpdatedAt` | required | Last state/time mutation timestamp |
| `Version` | required, starts at 1 | Optimistic mutation contract |

Retain legacy `ProviderId` as a deprecated compatibility attribute whose existing meaning is **business id** until data is profiled and readers migrate. Do not silently reinterpret historical values.

Recommended but separable additions:

- Business `TimeZoneId`; until introduced, API behavior uses the explicit interim assumption `Africa/Johannesburg`.
- A booking audit record/list for actor, action, from-status, to-status, and timestamp. The state machine can be enforced before a full audit UI exists, but audit storage is a schema gap.

No relational migration or table rewrite is implied: DynamoDB non-key attributes are additive. Pixel's booking concurrency design owns reservation-table and transaction details (`docs/bookspot-booking-concurrency-design-2026-08-09.md:51-73,144-186,222-252`).

## 4. Contract-wide conventions

### 4.1 Transport and naming

- JSON property names: `camelCase`.
- Content type: `application/json`; errors: `application/problem+json`.
- IDs: opaque strings as defined above.
- Date-time instants: RFC 3339 with explicit offset on input; canonical UTC `Z` on output, whole-second precision, for example `2026-08-10T09:30:00Z`.
- Offset-less date-times are 400. Convert to UTC before ownership-independent validation, conflict checks, and persistence.
- Local business times: `HH:mm` 24-hour strings plus an IANA timezone id.
- Date-only query bounds, when used: ISO `YYYY-MM-DD` interpreted in the response's declared timezone. Booking filters should prefer RFC 3339 instants.
- Money: JSON number representing decimal major units plus ISO 4217 currency (`ZAR`). Never use binary floating-point in backend calculations.
- Collections are non-null arrays. Empty results return `[]`, not `null` and not 404.
- Optional update properties are omitted when unchanged. Explicit `null` clears only fields documented nullable; otherwise it is validation failure.

### 4.2 Enums

```text
UserType       = client | provider
BookingStatus  = pending | confirmed | declined | cancelled | completed | no_show
BookingAction  = confirm | decline | cancel | complete | mark_no_show | reschedule
DayOfWeek      = monday | tuesday | wednesday | thursday | friday | saturday | sunday
```

Persist enum values lowercase. Unknown values are 400; do not silently coerce. Current frontend's four-value enum must expand (`frontend/src/types/api.ts:270-282`).

### 4.3 Standard error envelope

Use RFC 7807 plus stable extensions:

```json
{
  "type": "https://bookspot.example/problems/booking-state-conflict",
  "title": "Booking state conflict",
  "status": 409,
  "detail": "The requested action is not valid for the booking's current state.",
  "instance": "/bookings/b_123/actions",
  "code": "booking_state_conflict",
  "traceId": "00-opaque-correlation-id",
  "errors": {
    "startTime": ["A timestamp with an explicit offset is required."]
  }
}
```

`errors` is present only for field validation and maps camelCase field names to non-empty string arrays. `detail` must not contain passwords, reset tokens, hashes, AWS exception text, rival booking/client identity, or table names.

Stable common codes:

| HTTP | Code examples | Meaning |
|---:|---|---|
| 400 | `validation_failed`, `invalid_request` | Malformed or semantically invalid input |
| 401 | `authentication_required`, `invalid_credentials` | Missing/invalid token or generic login failure |
| 403 | `role_forbidden`, `resource_forbidden` | Authenticated but wrong role/known ownership failure |
| 404 | `profile_not_found`, `business_not_found`, `service_not_found`, `booking_not_found` | Missing or intentionally concealed resource |
| 409 | `email_already_registered`, `booking_slot_conflict`, `booking_state_conflict`, `idempotency_key_reused` | Duplicate or concurrent/domain conflict |
| 410 | `reset_token_expired`, `reset_token_used` | Recovery capability is no longer usable |
| 503 | `persistence_unavailable` | Transient persistence failure; retry with same idempotency key |

Framework-generated model validation, authorization failures, and application exceptions must use the same envelope. This extends, rather than replaces, the existing `ProblemDetails` middleware.

## 5. DTO definitions

### 5.1 Auth and profile DTOs

```ts
type UserType = 'client' | 'provider';

interface RegisterRequest {
  email: string;                 // required; trim + normalize + lowercase
  fullName: string;              // required
  contactNumber: string | null;  // optional/nullable
  password: string;              // required; never echoed
  userType: UserType;            // required and immutable
}

interface LoginRequest { email: string; password: string; }

interface ProfileDto {
  profileId: string;
  email: string;
  fullName: string;
  contactNumber: string | null;
  userType: UserType;
  createdAt: string;
}

interface AuthSessionDto {
  accessToken: string;
  tokenType: 'Bearer';
  expiresAt: string;
  profile: ProfileDto;
}

interface UpdateMyProfileRequest {
  fullName?: string;
  contactNumber?: string | null;
}
```

No update contract accepts `profileId`, `email`, `userType`, `passwordHash`, or `createdAt`. Email/password changes, if later required, need dedicated verified operations.

Compatibility: for one frontend migration window, auth responses may also expose current flattened fields `token`, `userId`, `email`, `fullName`, `contactNumber`, `userType`, `expiresAt` (`AuthResponse.cs:3-11`; `AuthContext.tsx:78-109`). The nested `profile` form is canonical; aliases are deprecated.

### 5.2 Business and service DTOs

```ts
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
  timeZone: string;              // interim constant Africa/Johannesburg
  createdAt: string;
}

interface CreateBusinessRequest {
  businessName: string;
  description: string;
  address: string;
  city: string;
  phone: string;
  email: string;
  website?: string | null;
  imageUrl?: string | null;
  isActive?: boolean;            // defaults true
}

type UpdateBusinessRequest = Partial<Omit<CreateBusinessRequest, 'isActive'>> & {
  isActive?: boolean;
};

interface ServiceDto {
  serviceId: string;
  businessId: string;
  providerProfileId: string;
  providerDisplayName: string;
  name: string;
  description: string;
  category: string | null;
  price: { amount: number; currency: 'ZAR' };
  durationMinutes: number;
  imageUrl: string | null;
  tags: string[];
  location: string | null;
  isActive: boolean;
  createdAt: string;
}

interface CreateServiceRequest {
  businessId: string;
  name: string;
  description: string;
  category?: string | null;
  priceAmount: number;
  durationMinutes: number;
  imageUrl?: string | null;
  tags?: string[];
  location?: string | null;
  isActive?: boolean;
}
```

`providerProfileId` and display name are server-derived. During compatibility, existing `id`, `providerId`, `providerName`, and scalar `price` may remain as aliases in current routes; new consumers use canonical names. Ownership never uses an alias.

### 5.3 Hours and availability DTOs

```ts
interface BusinessHoursDayDto {
  dayOfWeek: DayOfWeek;
  isClosed: boolean;
  openTime: string | null;       // HH:mm; required when open
  closeTime: string | null;      // HH:mm; required when open
}

interface ReplaceBusinessHoursRequest {
  timeZone: string;
  days: BusinessHoursDayDto[];   // exactly one entry per day
}

interface AvailabilitySlotDto {
  startTime: string;             // UTC RFC 3339
  endTime: string;               // UTC RFC 3339
}

interface ServiceAvailabilityDto {
  serviceId: string;
  businessId: string;
  timeZone: string;
  from: string;
  to: string;
  durationMinutes: number;
  slots: AvailabilitySlotDto[];
}
```

Availability is a projection, not a stored aggregate: active business + active service + business hours minus capacity held by `pending`/`confirmed` bookings. Preview availability is advisory; booking creation remains the final conflict authority.

### 5.4 Booking DTOs

```ts
interface CreateBookingRequest {
  serviceId: string;
  startTime: string;             // explicit offset required
}

interface BookingActionRequest {
  action: BookingAction;
  expectedVersion: number;
  startTime?: string;            // required only for reschedule; forbidden otherwise
}

interface BookingDto {
  bookingId: string;
  serviceId: string;
  businessId: string;
  providerProfileId: string;
  clientProfileId: string;
  status: BookingStatus;
  startTime: string;
  endTime: string;
  price: { amount: number; currency: 'ZAR' };
  version: number;
  createdAt: string;
  updatedAt: string;
  service: {
    name: string;
    durationMinutes: number;
  };
  business: {
    businessName: string;
    address: string;
    city: string;
  };
  client: {
    fullName: string;
    email: string;
    contactNumber: string | null;
  };
}

interface BookingPageDto {
  items: BookingDto[];
  nextCursor: string | null;
}
```

The server derives client, business, provider, end time, status, name/display data, and price snapshot. `providerName`, `clientName`, `clientEmail`, `clientPhone`, `endTime`, `businessId`, `providerProfileId`, `clientProfileId`, `status`, and price are not accepted on create.

`client` contact data is returned only to a booking party. Public service/business APIs never expose arbitrary profile contact data.

### 5.5 Dashboard DTOs

```ts
interface ProviderDashboardDto {
  kind: 'provider';
  generatedAt: string;
  timeZone: string;
  todayBookings: number;
  weekBookings: number;
  pendingRequests: number;
  totalClients: number;
  activeServices: number;
  monthlyRevenue: { amount: number; currency: 'ZAR' };
  upcoming: BookingDto[];
  recentClients: Array<{
    clientProfileId: string;
    fullName: string;
    lastBookingAt: string;
    totalBookings: number;
  }>;
}

interface ClientDashboardDto {
  kind: 'client';
  generatedAt: string;
  totalBookings: number;
  completedBookings: number;
  cancelledBookings: number;
  pendingRequests: number;
  totalSpent: { amount: number; currency: 'ZAR' };
  upcoming: BookingDto[];
  recent: BookingDto[];
}

type DashboardDto = ProviderDashboardDto | ClientDashboardDto;
```

All values are query-derived. A new account returns zeroes and empty arrays, never fabricated examples. Revenue/spend use completed booking price snapshots, not current service prices.

## 6. Endpoint contract

Unless marked Public, endpoints require a valid bearer JWT. `owner` means a server-side relationship check, not a path/body-id comparison.

### 6.1 Registration, login, and recovery

| Method and route | Authn/authz | Request -> response | Success | Errors / compatibility |
|---|---|---|---:|---|
| `POST /auth/register` | Public | `RegisterRequest -> AuthSessionDto` | 201 + `Location: /profiles/me` | 400 validation; 409 normalized email duplicate. Adds missing route over existing handler. |
| `POST /auth/login` | Public | `LoginRequest -> AuthSessionDto` | 200 | 401 generic `invalid_credentials`; never reveal email existence. Current route retained. |
| `POST /auth/forgot-password` | Public | `{email:string} -> {message:string}` | 200 | Same response for known/unknown email. Current route retained. |
| `POST /auth/validate-reset-token` | Public | `{token:string} -> {valid:true}` | 200 | 400 invalid; 410 expired/used. Replaces token-in-path route; never log token. |
| `POST /auth/reset-password` | Public | `{token:string,newPassword:string} -> {message:string}` | 200 | 400 invalid/password; 410 expired/used. BCrypt is invariant. |

Remove or return 404 for `GET /auth/validate-reset-token/{token}` after one short frontend transition; do not redirect because that preserves token leakage in paths. The current implementation validates only non-empty input (`AuthController.cs:92-118`) and must not be treated as a compatibility behavior.

### 6.2 Profile, me, and account lifecycle

| Method and route | Authn/authz | Request -> response | Success | Errors / compatibility |
|---|---|---|---:|---|
| `GET /profiles/me` | Client or Provider, self | none -> `ProfileDto` | 200 | 401; 404 stale token/profile. Existing route retained but entity replaced by DTO. |
| `PATCH /profiles/me` | Client or Provider, self | `UpdateMyProfileRequest -> ProfileDto` | 200 | 400 validation; 401. Canonical partial update. |
| `DELETE /profiles/me` | Client or Provider, self | none | 204 | 401; 409 if account deletion cannot preserve required records. |

There is intentionally no separate Account entity or `/settings` aggregate API. “My account” is the authenticated profile plus role-specific resources. Existing `GET/PUT/DELETE /profiles/{id}` should become temporary aliases requiring `id == JWT subject`; `POST /profiles` is removed because registration is the only public account creation path. Existing body ids/roles are ignored/rejected, never trusted.

### 6.3 Businesses

| Method and route | Authn/authz | Request -> response | Success | Errors / compatibility |
|---|---|---|---:|---|
| `GET /businesses/{businessId}` | Public | none -> `BusinessDto` | 200 | 404. Current route retained. |
| `GET /businesses/mine` | Provider | none -> `BusinessDto[]` | 200 | 401/403; empty list is 200 `[]`. New resolver for Settings. |
| `POST /businesses` | Provider | `CreateBusinessRequest -> BusinessDto` | 201 + Location | 400/401/403. Owner comes from JWT. Current route retained. |
| `PATCH /businesses/{businessId}` | Provider + business owner | `UpdateBusinessRequest -> BusinessDto` | 200 | 400/401/403/404. |
| `DELETE /businesses/{businessId}` | Provider + business owner | none | 204 | 401/403/404; 409 if dependent-resource policy blocks deletion. Prefer deactivate when history exists. |

Current `PUT /businesses/{id}` can delegate to partial-update semantics for compatibility; body `id` is ignored or must equal the path, then removed from the frontend contract. Settings selects an actual `businessId` returned by `/businesses/mine`, never `profileId`.

### 6.4 Services and search

| Method and route | Authn/authz | Request -> response | Success | Errors / compatibility |
|---|---|---|---:|---|
| `GET /services/{serviceId}` | Public | none -> `ServiceDto` | 200 | 404. |
| `GET /services/search` | Public | filters -> `{items:ServiceDto[],page,pageSize,totalCount}` | 200 | 400. Add `category`; only active business/service records. |
| `GET /businesses/{businessId}/services` | Public | none -> `ServiceDto[]` | 200 | 404 only if business absent; empty list otherwise. Current auth requirement is relaxed explicitly for browse/booking. |
| `POST /services` | Provider + owner of body `businessId` | `CreateServiceRequest -> ServiceDto` | 201 + Location | 400/401/403/404. Server loads business; never compares business id to profile id. |
| `PATCH /services/{serviceId}` | Provider + owner of service's business | update fields -> `ServiceDto` | 200 | 400/401/403/404. Business ownership resolved each time. |
| `DELETE /services/{serviceId}` | Provider + owner | none | 204 | 401/403/404; preserve historical bookings. Prefer deactivate. |

Current `GET /services` may remain as an unpaged compatibility list. Current `PUT` may alias `PATCH`. Frontend must stop fabricating fallback provider names (`frontend/src/services/api.ts:151-177`) once canonical DTOs land.

### 6.5 Business hours and availability

| Method and route | Authn/authz | Request -> response | Success | Errors / compatibility |
|---|---|---|---:|---|
| `GET /businesses/{businessId}/hours` | Provider + owner | none -> `{timeZone,days}` | 200 | 401/403/404. Settings-oriented raw schedule. |
| `PUT /businesses/{businessId}/hours` | Provider + owner | `ReplaceBusinessHoursRequest -> {timeZone,days}` | 200 | 400/401/403/404. Whole-week replacement prevents partial/dead UI state. |
| `GET /services/{serviceId}/availability?from=<instant>&to=<instant>` | Public | none -> `ServiceAvailabilityDto` | 200 | 400 invalid/range too large; 404; 409/422 only if service schedule is internally invalid. |

Default availability range limit: 31 days. Return only bookable starts. Each start must be on the adopted 15-minute grid and fit fully inside business hours. Existing item-id `/business-hours` endpoints may remain internal compatibility adapters but must be authenticated/owner-checked; they are not the frontend target.

### 6.6 Booking creation, reads, and actions

| Method and route | Authn/authz | Request -> response | Success | Errors / compatibility |
|---|---|---|---:|---|
| `POST /bookings` | Client only | `CreateBookingRequest -> BookingDto` | 201 + Location | Requires `Idempotency-Key`; 400, 401, 403, 404 service/business, 409 slot/idempotency conflict, 503 persistence. |
| `GET /bookings/{bookingId}` | Booking client or owning provider | none -> `BookingDto` | 200 | 401; 404 for absent/non-party to avoid enumeration. Current anonymous read closes. |
| `GET /bookings/client/me` | Client self | filters/cursor -> `BookingPageDto` | 200 | 400/401/403. |
| `GET /bookings/provider/me` | Provider self | optional `businessId`, status, from, to, cursor -> `BookingPageDto` | 200 | 400/401/403; requested business must be owned. |
| `POST /bookings/{bookingId}/actions` | Booking party + action role | `BookingActionRequest -> BookingDto` | 200 | Requires `Idempotency-Key`; 400, 401, 403, 404, 409 stale/illegal/conflict, 503. |

Filters use canonical enum values and UTC instants. Sort listings by `startTime` descending unless `sort=asc` is explicitly supplied. Cursor is opaque and nullable.

Compatibility:

- `GET /bookings/client/{clientId}` remains temporarily but requires `clientId == JWT subject`; canonical frontend route is `/client/me`.
- `GET /bookings/provider/{providerId}` remains temporarily but requires `providerId == JWT subject`; canonical frontend route is `/provider/me`.
- `PUT /bookings/{id}` must not retain its current generic behavior. During migration, either return 410 with a migration code or accept only a strict adapter that maps status/action to the action service without default timestamps. It must never write `DateTime.MinValue`.
- `DELETE /bookings/{id}` becomes the `cancel` action for users. Hard delete is not a user lifecycle operation.

### 6.7 Settings composition

No `GET /settings` or `PUT /settings` is introduced. The frontend Settings screen uses:

1. `GET/PATCH /profiles/me` for account details.
2. `GET /businesses/mine` to select a real `businessId` or show provider onboarding.
3. `PATCH /businesses/{businessId}` for business/location fields.
4. `GET/PUT /businesses/{businessId}/hours` for availability rules.
5. `GET /businesses/{businessId}/services` plus service mutations.

Client settings render only profile/account controls. Provider-only tabs require provider role in both UI and API. This composition avoids an unnecessary cross-aggregate transaction and fixes the current `user.id` misuse without a rewrite.

### 6.8 Dashboard

| Method and route | Authn/authz | Request -> response | Success | Errors / compatibility |
|---|---|---|---:|---|
| `GET /dashboard/me` | Client or Provider, self-scoped | none -> `DashboardDto` discriminated by `kind` | 200 | 401/403; real zero/empty results. |

The server chooses the DTO from the immutable JWT/stored role. No profile id appears in the canonical route. During transition, current `/dashboard/provider/{providerId}/stats`, `/dashboard/providers/{providerId}/insights`, `/dashboard/client/{clientId}/stats`, and `/dashboard/my-stats` may remain but must enforce subject equality and return real data. The frontend should converge on `/dashboard/me` rather than branching path ids (`frontend/src/services/api.ts:317-345`; `frontend/src/hooks/useDashboard.ts:12-24`).

## 7. Booking state machine and action authorization

All unlisted transitions are 409 `booking_state_conflict`. Role is derived from JWT. Ownership is resolved server-side before transition evaluation.

| Source | Action | Target | Permitted actor | Additional invariant |
|---|---|---|---|---|
| none | create | `pending` | client | Caller becomes `clientProfileId`; service/business active; valid future slot |
| `pending` | confirm | `confirmed` | owning provider | Times unchanged |
| `pending` | decline | `declined` | owning provider | Terminal; release capacity |
| `pending` | cancel | `cancelled` | booking client or owning provider | Terminal; release capacity |
| `confirmed` | cancel | `cancelled` | booking client or owning provider | Interim assumption: before start, no fee; release capacity |
| `confirmed` | complete | `completed` | owning provider | `endTime <= now`; terminal; release capacity |
| `confirmed` | mark_no_show | `no_show` | owning provider | `startTime <= now`; terminal; release capacity |
| `pending` | reschedule | `pending` | booking client or owning provider | New start required; derive end; revalidate availability; increment version |
| `confirmed` | reschedule | `pending` | booking client or owning provider | Same as above; confirmation is intentionally reset |
| terminal | any | none | none | `declined`, `cancelled`, `completed`, `no_show` are terminal |

Every successful action:

- verifies `expectedVersion` and increments version;
- sets `updatedAt` from the server clock;
- does not change service/client/business/provider identities;
- records actor profile id, action, source/target status, and time in the audit boundary;
- leaves start/end unchanged except for reschedule;
- returns the canonical booking representation.

Provider ownership rule: load booking, load service by `serviceId`, load business by `service.businessId`, then require `business.providerId == JWT subject`. Do not trust legacy `Booking.ProviderId`, denormalized `Service.ProviderId`, path provider ids, or body ids.

Client ownership rule: require `booking.clientProfileId == JWT subject`. Booking create always derives that value from JWT.

Wrong role returns 403. A non-party direct booking lookup/action returns 404 to avoid confirming existence. An owning party attempting an illegal transition receives 409.

## 8. Booking persistence integration boundary (mechanism intentionally out of scope)

Application/domain contracts should depend on a persistence-neutral mutation boundary such as:

```csharp
Task<CreateBookingResult> CreateAsync(CreateBookingIntent intent, CancellationToken ct);
Task<BookingMutationResult> TransitionAsync(TransitionBookingIntent intent, CancellationToken ct);
Task<BookingMutationResult> RescheduleAsync(RescheduleBookingIntent intent, CancellationToken ct);
```

The intents contain canonical ids/times, actor, expected version where applicable, and idempotency metadata. Results distinguish success/replay from domain conflict and transient persistence failure.

Required invariants at the boundary:

1. Booking row and capacity claim commit atomically.
2. `pending` and `confirmed` consume capacity.
3. The current exclusive capacity boundary is `businessId`, not provider profile id or service id.
4. Create/reschedule derive end from service duration and use half-open intervals `[start,end)`.
5. Transition/release/reschedule are atomic with expected source status/version.
6. Same idempotency key + same fingerprint replays; same key + different fingerprint conflicts.
7. Conditional contention maps to 409; throttling/timeouts/indeterminate infrastructure map to 503.
8. Old mutation writers may not coexist after atomic-writer cutover.

The reservation schema, transaction algorithm, migration freeze/backfill, and conditional-write implementation remain Pixel's design responsibility. This API contract consumes those guarantees but does not duplicate or alter them.

## 9. Schema gaps versus enforcement/call-site bugs

### 9.1 Actual schema/contract gaps

- Booking lacks unambiguous `BusinessId` and `ProviderProfileId`.
- Booking lacks immutable price/currency snapshot, `UpdatedAt`, and version.
- Booking status is an unconstrained string; domain enum/transition policy is absent.
- Booking audit persistence is absent.
- Business timezone is absent; availability needs an explicit timezone rule.
- Business-hours repository cannot list/replace a schedule by business.
- Search has no total-count envelope/category filter and returns entities.
- Public DTOs for profile/business/service/booking are absent.
- Stable application conflict/error codes are absent.

### 9.2 Enforcement and call-site bugs; no schema rewrite required

- Missing `/auth/register` despite an existing handler.
- Reset validation accepts any non-empty token and reset uses SHA-256 rather than BCrypt (`AuthController.cs:92-118`; `ResetPasswordCommand.cs:31-88`). Existing token records already have expiry/use semantics.
- Anonymous profile/business/service/hour/review mutations and entity leakage.
- Role/email can be changed by normal profile update.
- Service create compares `businessId` to profile id rather than loading Business.
- Settings and BookingPage use misleading `providerId`/`user.id` values as business ids.
- Booking create accepts end/name fields that the server can derive.
- Booking read/update/delete lack party ownership checks.
- Status-only booking update overwrites timestamps.
- Pending bookings are excluded from the current conflict candidate set (`BookingRepository.cs:17-31`).
- Dashboard returns fabricated client data and values historical revenue at current service price.
- Frontend HTTP client assumes every response has JSON, including 204 (`frontend/src/services/api.ts:59-101`).

## 10. ADR: incremental contract rather than rewrite

### Context

The existing clean-architecture solution has usable MediatR handlers/repositories and a React adapter layer, but routes expose entities, authorization is inconsistent, and identifiers are conflated. DynamoDB data is schema-flexible and the working tree contains unrelated concurrent changes.

### Decision

Adopt an additive, self-scoped API evolution:

- introduce DTOs and canonical names at API/application boundaries;
- add self-resolving routes while temporarily securing old id routes;
- add booking canonical attributes rather than renaming ambiguous data;
- introduce explicit booking actions rather than extending generic `PUT`;
- compose Settings from existing aggregates;
- use one role-discriminated self dashboard;
- keep persistence mechanics behind interfaces.

### Alternatives considered

1. **Rename every `ProviderId` in place. Rejected.** Existing booking values have a different meaning from business/service values; an in-place rename can silently corrupt authorization.
2. **Create a brand-new `/api/v2` and rewrite both applications. Rejected now.** It increases parallel scope and migration risk. Additive routes/DTOs close the verified defects with less disruption. Versioning can be added when external consumers require it.
3. **Treat provider profile as the business. Rejected.** `Business.Id` and `Business.ProviderId` already model separate identities and support many businesses per provider.
4. **Create a Settings aggregate endpoint. Rejected.** Settings spans profile, business, hours, and services with different ownership/lifecycle rules; an aggregate endpoint would introduce unnecessary coupling.
5. **Keep generic booking `PUT`. Rejected.** Optional status/times cannot express actor-specific transitions safely and caused timestamp erasure. Explicit action semantics are auditable and conflict-aware.
6. **Authorize from denormalized provider ids for speed. Rejected.** Correctness and tenant isolation require the authoritative Business relationship. Query optimization can be added without changing authorization semantics.

### Consequences

- Temporary response aliases and secured route adapters add short-lived complexity.
- Existing booking data must be profiled/backfilled before canonical fields become required for all reads.
- Frontend can migrate feature by feature instead of in one cutover.
- Ownership and lifecycle logic become testable at the application boundary.
- No unnecessary new Account, Settings, or Availability persistence aggregate is introduced.

## 11. Incremental rollout and compatibility plan

1. **Contract foundation:** standard ProblemDetails extensions; DTOs; enum/date serialization tests; global authenticated-by-default policy with explicit public routes.
2. **Auth/profile:** expose register; fix BCrypt recovery and body token validation; return `ProfileDto`; add `PATCH/DELETE /profiles/me`; secure legacy profile routes.
3. **Business resolution:** add `/businesses/mine`; migrate Settings from `user.id` to selected `businessId`; secure business/service mutations by authoritative ownership.
4. **Service/search:** return DTOs/paged search; add category; keep old list shape only as a documented compatibility route/adapter.
5. **Hours/availability:** add business schedule read/replace and service availability projection; replace hard-coded frontend slots.
6. **Booking expand:** add canonical booking fields/version/snapshot and persistence-neutral mutation interfaces; normalize existing records as Pixel's migration requires.
7. **Booking actions:** enable atomic create/action/reschedule contracts; move frontend to `/client/me` and `/provider/me`; retire unsafe `PUT/DELETE` semantics.
8. **Dashboard:** compute real self-scoped DTOs from canonical booking values; migrate frontend to `/dashboard/me`.
9. **Contract cleanup:** remove flattened auth aliases, legacy id-scoped self routes, token-in-path validation, scalar price/provider aliases, and ambiguous booking `ProviderId` only after telemetry/tests confirm no consumers.

No step requires a broad cross-repository rewrite. Each vertical slice should add backend contract tests, frontend types/client changes, and a real flow check before legacy behavior is removed.

## 12. Technical risks and controls

| Risk | Control |
|---|---|
| Historical `Booking.ProviderId` has mixed/unknown semantics | Never authorize from it; derive through Service/Business; migration exception report |
| Old booking writer bypasses new invariants | Feature-gated mutation cutover; old instances read-only after cutover |
| Route aliases live forever | Deprecation headers/telemetry and dated removal criteria |
| DTO aliasing creates conflicting fields | Canonical-field precedence, contract tests, aliases response-only |
| Availability preview races create | Document preview as advisory; atomic create is authority |
| Profile/business enumeration via errors | Self routes; 404 for non-party direct booking/profile access |
| Dashboard totals drift with current service changes | Immutable booking price snapshot and explicit timezone/currency |
| Business timezone assumption is wrong for expansion | Return timezone explicitly; add configurable `TimeZoneId` before multi-region launch |
| Hard delete breaks booking history | Prefer deactivate/cancel; 409 destructive operations with dependents |
| Concurrent sibling work in dirty tree | Modify only this document; implementation tasks must rebase/reinspect current source |

## 13. Explicit assumptions and open product decisions

Adopted interim assumptions, aligned with `docs/bookspot-product-requirements-2026-08-09.md:19-26,121-133`:

- One immutable role per account; providers do not book through provider accounts.
- One provider owner and one exclusive resource per business; no staff/room resource model.
- Provider may own many businesses.
- Both clients and providers self-register; no provider vetting/email verification in MVP.
- Currency is ZAR; reporting/business timezone is `Africa/Johannesburg` until configurable.
- Revenue/spend recognize completed bookings at booking price snapshot.
- Business hours only; no booking lead time or buffer.
- Client/provider may cancel before start without fee; `no_show` is recorded but has no commercial consequence.
- Pending bookings do not expire automatically.
- Search and public business/service browsing are anonymous; all mutations are deny-by-default.

Decisions that require product confirmation before expanding beyond the interim contract:

- dual-role accounts;
- providers booking other providers;
- client cancellation cutoff/fee;
- staff/room resources and multi-provider businesses;
- provider verification;
- multi-currency/multi-timezone reporting;
- lead time, buffers, holidays/blocked dates, and pending expiry.

## 14. Minimum contract verification matrix for implementers

- Register client/provider -> 201, token works on `/profiles/me`, no password hash.
- Case/space-equivalent email duplicate -> 409; wrong login -> generic 401.
- Recovery token valid/expired/used paths and BCrypt reset-login round trip.
- Anonymous profile/business/service/hour/booking mutations -> 401.
- Cross-profile/cross-business/cross-booking attempts -> 403 or concealed 404 as specified.
- Provider with two businesses: `/businesses/mine` returns distinct business ids; Settings round-trips one selected id.
- Service create authorizes via `Business.ProviderId`, never `businessId == profileId`.
- Availability uses business hours and excludes pending/confirmed capacity.
- Booking create ignores/rejects spoofed derived fields and returns canonical ids/snapshot.
- Every state-machine row succeeds only for the permitted party; all unlisted transitions -> 409.
- Status action leaves times unchanged; reschedule resets to pending and increments version.
- Client/provider booking lists are self-scoped and contain no other tenant's data.
- New-account dashboard returns real zeroes/empty arrays; completed booking snapshot changes totals deterministically.
- All 204 responses are accepted by the frontend without JSON parsing failure.

---

This draft deliberately defines domain meaning, API behavior, authorization, and integration invariants while leaving DynamoDB atomicity implementation to the dedicated concurrency design. It is suitable as the contract baseline for incremental vertical-slice implementation and OpenAPI/frontend type alignment.
