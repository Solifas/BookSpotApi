# Backend remediation migration notes

Status: partial implementation; not production release-ready.

## Implemented compatibility changes

- Authentication now uses a 15-minute JWT containing a security-version claim. Password reset increments `Profile.SecurityVersion`; stale JWTs are rejected during token validation.
- Registration normalizes email with trim/NFC/invariant lowercase and atomically writes the profile plus a unique `identity_claims` row. Duplicate normalized claims return conflict without creating an orphan profile.
- Existing profiles require a migration that populates `EmailNormalized`, `SecurityVersion=1`, and one `identity_claims` row per profile. Registration must remain disabled until collision reporting and one-to-one reconciliation complete.
- Password-reset tokens are stored by digest and consumed in one DynamoDB transaction with the profile password/security-version update. Existing plaintext-keyed tokens must be invalidated rather than copied.
- Booking creation accepts only `serviceId` and `startTime`; ownership, end time, provider/business identifiers, timestamps, status, and version are server-derived.
- Booking creation writes the booking, idempotency record, immutable create audit, and every 15-minute slot reservation in one DynamoDB transaction. `pending` slots now block concurrent creates.
- Generic booking PUT and DELETE routes are removed. `POST /bookings/{id}/actions` now enforces party ownership, role-specific transitions, expected versions, cancellation/completion/no-show timing, and mandatory idempotency keys.
- Booking confirm, decline, cancel, complete, mark-no-show, and reschedule commit the request record, version-conditioned booking update, immutable audit event, and owned slot status/release/move operations in one DynamoDB transaction. Reschedule acquires new cells before releasing old cells within the same transaction.
- Business, service, business-hour, and review mutation handlers now enforce resource-chain ownership rather than trusting route/body or denormalized provider identifiers. Review creation requires the authenticated booking client and a completed booking.

## Additive infrastructure

Provision before enabling the new writers:

- `identity_claims` (PK `ClaimKey`)
- `booking_reservations` (PK `ReservationKey`)
- `booking_audit` (PK `AuditKey`)
- existing `password_reset_tokens` table (PK `Token`) for the transitional digest-only reset implementation

Terraform, Bash LocalStack initialization, and PowerShell table creation declare these tables. No production migration or destructive operation was run.

## Remaining release blockers

The approved contract still requires human decisions HD-01 (production registration enumeration behavior) and HD-02 (final password policy). It also requires production DynamoDB inventory and migration/cutover decisions before all-writer booking activation.

The following are not yet implemented and prevent release certification:

- distributed authentication abuse counters;
- asynchronous encrypted recovery queue, latest-generation `auth_capabilities`, auth audit, encrypted delivery outbox, KMS envelope flow, and same-operation reset replay;
- booking configuration-version condition checks, schedule/timezone enforcement, exact persisted replay bodies/headers, and request tombstone compaction;
- atomic one-review-per-booking persistence and complete canonical public/manage DTO coverage;
- LocalStack and real-AWS concurrency/integration execution (Docker was unavailable in this worker environment).
