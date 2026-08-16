# BookSpot — DynamoDB booking atomicity and identifier persistence contract

**Author:** Pixel (database/concurrency design)
**Date:** 2026-08-09
**Status:** implementation contract; design only; no application code changed

## 0. Decision summary

1. A `pending` booking consumes exclusive capacity exactly like a `confirmed` booking.
2. The exclusivity boundary for the current one-provider-per-business model is `businessId`, not profile/provider id and not service id.
3. Add a `booking_reservations` DynamoDB table. Each live booking owns deterministic 15-minute reservation cells.
4. Create the booking row, all reservation cells, and the persistent idempotency record in one `TransactWriteItems` request. Every reservation Put uses `attribute_not_exists(ReservationKey)`.
5. Do **not** use check-then-save, a Scan, a GSI, or independent conditional Puts with compensation as the authority. Independent multi-cell Puts can make both contenders partially acquire and both fail; that does not prove “exactly one success.”
6. Cancellation/status changes/reschedules update the booking and release/move its cells transactionally, with ownership and expected-version conditions.
7. Conditional contention maps to HTTP 409. Capacity, timeout, and infrastructure failures do not.
8. No index is required for correctness. Query indexes are a separate, online optimization after identifier cleanup.
9. New and old versions may coexist for reads. They may **not** both perform booking mutations after cutover because the old writer never creates reservation guards.
10. The migration is expand → profile/clean data → brief booking-write freeze → backfill guards → switch all booking writers → verify → later contract. It does not lock the existing DynamoDB tables, but it requires an application-level mutation freeze unless a more complex change-capture path is built.

## 1. Verified current state and root cause

### 1.1 Booking persistence and race

- `backend/BookSpot.Domain/Entities/Booking.cs:5-9` maps `Booking` to `bookings` with only `Id` as its hash key. Lines 11-33 are non-key attributes; status is a free string defaulting to `pending`.
- `backend/BookSpot.Infrastructure/Repositories/DynamoDb/BookingRepository.cs:13-15` loads by id and uses blind `DynamoDBContext.SaveAsync`/`DeleteAsync`.
- `BookingRepository.cs:17-31` performs a table Scan, excludes both `cancelled` **and `pending`**, then checks overlap in memory.
- `backend/BookSpot.Application/Features/Bookings/Commands/CreateBookingCommand.cs:82-87` performs that non-atomic check; lines 89-102 generate a new GUID, set status `pending`, and save unconditionally. Two requests can both observe no conflict, and their unrelated GUID keys never collide.
- `CreateBookingCommand.cs:59-80` calculates a duration but still accepts a client-supplied `EndTime`; this is not a trustworthy reservation boundary.
- `backend/BookSpot.Application/Features/Bookings/Commands/CreateBookingCommandValidator.cs:15-23,50-54` already requires starts on 15-minute boundaries. Lines 63-65 permit durations up to eight hours, but do not require the end/duration to align to the same grid.
- `backend/BookSpot.Application/Features/Bookings/Commands/UpdateBookingCommand.cs:14-23` read-modify-saves start, end, and status with no version or transition condition.
- `backend/BookSpot.Application/Features/Bookings/Commands/DeleteBookingCommand.cs:13-18` reads and deletes only the booking row.

The current conflict predicate’s interval mathematics is correct (`A.start < B.end && B.start < A.end`), but its candidate set and concurrency primitive are not. Merely including `pending` in the Scan still leaves a race.

### 1.2 Tables, keys, indexes, and access patterns

| Logical table | Current primary key / evidence | Current observed access | Index state |
|---|---|---|---|
| `profiles` | `Id` (`Profile.cs:5-9`; local init `01-create-dynamodb-tables.sh:50`) | get/save/delete by id; scans elsewhere | Terraform has base key only (`terraform/modules/dynamodb/main.tf:1-25`) |
| `businesses` | `Id` (`Business.cs:5-9`; init :51) | `BusinessRepository.cs:13-20`: by id or Scan all | Terraform declares `ProviderId-index` (`main.tf:27-62`); local init creates no GSI |
| `services` | `Id` (`Service.cs:5-9`; init :52) | `ServiceRepository.cs:13-20`: by id or Scan all | Terraform declares `BusinessId-index` and `ProviderId-index` (`main.tf:64-110`); local init creates no GSI |
| `bookings` | `Id` (`Booking.cs:5-9`; init :54) | by id; three Scans for conflicts/provider/client (`BookingRepository.cs:17-53`) | Terraform declares `ProviderId-index` and `ClientId-index` (`main.tf:112-158`); repository does not query them; local init creates no GSI |
| `business_hours` | `Id` (init :53) | base-table repository pattern | Terraform declares `BusinessId-index` (`main.tf:160-195`); local init creates no GSI |
| `reviews` | `Id` (init :55) | base-table repository pattern | Terraform declares `BusinessId-index` (`main.tf:197-232`); local init creates no GSI |
| `password_reset_tokens` | `Token` (init :56) | token lookup | Terraform declares `Email-index` and TTL (`main.tf:233-259`) |

Important drift: Terraform describes several GSIs, but the current LocalStack bootstrap helper accepts only a table name and one hash key (`01-create-dynamodb-tables.sh:11-25`) and calls it without index definitions (`:49-56`). Source code still Scans. Therefore this report does not assume any GSI exists in a deployed environment without `DescribeTable` evidence.

`Program.cs:156-190` registers the low-level `IAmazonDynamoDB` client and `IDynamoDBContext`; the low-level client is available for `TransactWriteItems`. Development endpoint/region now come from configuration (`Program.cs:160-177`; `appsettings.Development.json:8-16`).

### 1.3 Identifier boundary (canonical, incremental)

Verified entity chain:

- provider profile: `Profile.Id`; role is `Profile.UserType` (`Profile.cs:5-21`)
- business: `Business.Id`; owner is `Business.ProviderId` (a provider **profile id**) (`Business.cs:5-12`)
- service: `Service.Id`; owner aggregate is `Service.BusinessId`; `Service.ProviderId` is a denormalized provider profile id (`Service.cs:5-18`)
- booking: `Booking.Id`, `ServiceId`, `ClientId`, and ambiguous `ProviderId` (`Booking.cs:5-18`)

Current creation assigns `service.BusinessId` to `Booking.ProviderId` (`CreateBookingCommand.cs:83,91-95`). Thus that field currently contains a **business id despite its name**. This also conflicts with `BookingsController.cs:39-60`, whose provider route documents and accepts a provider user id.

Target persistence meanings:

| Name | Meaning | Authority |
|---|---|---|
| `clientProfileId` | authenticated customer profile | JWT/current user; never request body |
| `providerProfileId` | owner profile of the business | `Business.ProviderId` |
| `businessId` | business providing the service; current exclusive resource boundary | `Service.BusinessId`, then load `Business.Id` |
| `serviceId` | product/service booked | `Service.Id` |
| `bookingId` | stable public booking identity | generated once by server; retained for audit |
| `resourceId` | future staff/room resource within a business | interim literal `single`; unresolved product decision E4 |

Incremental booking rows should add schema-less attributes `BusinessId` and `ProviderProfileId`. During compatibility, retain legacy `ProviderId` without silently changing its meaning; treat it as deprecated business-id data until all readers migrate. New authorization must resolve `Booking.ServiceId → Service.BusinessId → Business.ProviderId`, not trust a denormalized field.

## 2. Reservation table and deterministic key contract

Create table `booking_reservations`:

| Attribute | Type | Contract |
|---|---|---|
| `ReservationKey` (PK) | S | slot or idempotency key, formats below |
| `Kind` | S | `slot` or `booking_request` |
| `BookingId` | S | owning booking |
| `BusinessId` | S | canonical exclusive aggregate |
| `ProviderProfileId` | S | denormalized for diagnosis only |
| `ResourceId` | S | `single` for MVP |
| `StartTimeUtc`, `EndTimeUtc` | S | canonical RFC 3339 UTC instants |
| `Status` | S | `pending` or `confirmed` for slot rows |
| `RequestFingerprint` | S | SHA-256 hex/base64url digest on request rows only |
| `CreatedAtUtc` | S | server timestamp |
| `ExpiresAtEpochSeconds` | N | optional only on idempotency rows after documented retention; not correctness TTL for live slots |

No GSI is needed on this table. Repair/backfill jobs should enumerate it with controlled scans; correctness is key-based.

### 2.1 Normalization

1. API accepts RFC 3339 timestamps with an explicit offset (`Z` or `±HH:mm`). Offset-less values are HTTP 400.
2. Convert to UTC before validation or key construction.
3. Persist/return canonical UTC with `Z` and fixed subsecond policy (recommended whole seconds).
4. Start must have seconds/fraction zero and minute divisible by 15, consistent with the current validator.
5. The server derives end from `Service.DurationMinutes`; client `EndTime` is removed/ignored. Duration must be positive, at most eight hours, and divisible by 15. Existing services violating this are migration exceptions; do not round silently.
6. Intervals are half-open `[start,end)`. Adjacent intervals do not conflict.
7. IDs are opaque, case-sensitive strings. Do not lowercase or trim stored IDs. Key components are UTF-8 then base64url encoded without padding, preventing delimiter ambiguity.

### 2.2 Slot key

For every 15-minute cell `t` where `startUtc <= t < endUtc`:

```text
SLOT#v1#<b64url(businessId)>#<b64url(resourceId)>#<t as yyyyMMdd'T'HHmmss'Z'>
```

MVP `resourceId = "single"`. Example shape (illustrative, not a real id):

```text
SLOT#v1#bG9jYWwtYnVzaW5lc3MtMDAx#c2luZ2xl#20260810T090000Z
```

The version marker makes a future key migration explicit. Slot identity excludes `serviceId`: two services from the same one-person business must conflict. It excludes `providerProfileId`: a provider may own multiple businesses, while current product requirements define each business as the exclusive aggregate. If product decision E4 introduces staff/resources, replace only `resourceId`; do not overload provider/business identifiers.

With the current eight-hour maximum and 15-minute grid, creation uses at most 32 slot items. Including one booking and one idempotency item gives at most 34 transaction actions, below DynamoDB’s 100-action transaction limit.

### 2.3 Persistent idempotency key

Require `Idempotency-Key` on create/reschedule/cancel/status-action mutations. Validate it as 16–128 printable ASCII characters after rejecting leading/trailing whitespace. Scope it by authenticated client/actor and operation:

```text
REQ#v1#<operation>#<b64url(actorProfileId)>#<base64url(sha256(raw Idempotency-Key))>
```

Store a request row under that `ReservationKey`. `RequestFingerprint` is SHA-256 over canonical JSON containing the operation and all server-relevant normalized inputs (for create: actor profile id, service id, canonical start UTC). Never include `ProviderName` or client-derived end time.

Behavior:

- absent key: HTTP 400 for mutation endpoints once this contract is active;
- first key/fingerprint: transaction creates the request row and booking effect;
- same key + same fingerprint: strongly read request row and booking, return the original successful representation/status (201 for create) with `Idempotent-Replayed: true`; create no second booking;
- same key + different fingerprint: HTTP 409 `idempotency_key_reused`;
- concurrent same-key requests: one transaction commits; loser resolves the now-visible request row and replays the same booking;
- retain request rows for at least the booking’s live lifecycle plus 24 hours. TTL is cleanup only; after expiry, normal slot conflict still prevents duplicate capacity but replay of the original 201 is no longer guaranteed.

The DynamoDB `ClientRequestToken` may additionally be set from a bounded digest for SDK retries, but its short service-side window is not the persistent API idempotency contract.

## 3. Atomic write algorithms

### 3.1 Create

After authentication and strongly consistent service/business reads:

1. Derive `clientProfileId`, `businessId`, `providerProfileId`, duration/end, `bookingId`, slot keys, idempotency request key, and fingerprint.
2. Execute **one `TransactWriteItems`** containing:
   - Put `bookings[bookingId]` with `attribute_not_exists(Id)`;
   - Put request guard with `attribute_not_exists(ReservationKey)`;
   - Put every slot row with `attribute_not_exists(ReservationKey)`.
3. Return 201 only after commit.

`pending` and `confirmed` both retain all slots. This transaction has all-or-nothing semantics: there is no booking without guards and no orphan guard from a failed create.

Do not retain the pre-write Scan as an authority. It may be used only to generate a friendly availability preview; the transaction decides.

### 3.2 Status changes and release

Booking rows gain numeric `Version`, initially 1. Every mutation condition includes expected `Version` and expected source status, then increments Version.

- `pending → confirmed`: transactionally update booking and each owned slot’s Status, conditioned on `BookingId`; no delete/re-put.
- `pending → declined|cancelled`: transactionally update the booking to terminal and delete every slot conditioned on matching `BookingId`.
- `confirmed → cancelled|completed|no_show`: same atomic update + owned-slot deletes. Historical booking remains; hard delete is not the normal cancellation model.
- Terminal-state retry with the same idempotency key replays. A different operation against an already terminal/stale version returns 409.

There is no `expired` status in the current adopted product lifecycle. Pending bookings block until provider action or cancellation. Adding expiring holds requires an explicit product decision, a state-machine change, and an active expiry worker. DynamoDB TTL must never be the instant release mechanism because TTL deletion is asynchronous.

### 3.3 Reschedule

A reschedule resets status to `pending` per the product requirements. Build old and new slot sets:

- retain intersection cells (condition-check ownership if needed);
- conditionally Put `new − old` cells;
- conditionally Delete `old − new` cells with `BookingId = expected`;
- update booking times/status/version with expected old version/status;
- create the operation idempotency row.

All actions execute in one transaction. Never release old capacity before new capacity is secured. With two eight-hour ranges, booking update, and request row, the union remains below 100 actions (maximum 66 when disjoint).

### 3.4 Hard delete

If administrative hard delete remains, it must transactionally delete the booking and every live slot with ownership conditions. Normal user “delete” should become a lifecycle cancellation so audit history is preserved.

## 4. Failure and HTTP mapping

Current `GlobalExceptionHandler.cs:59-69` maps validation/bad request to 400 and otherwise returns 500; it has no conflict exception or 409 type mapping (`:72-93`). Add a persistence-neutral application conflict exception/error mapping.

| Cause | HTTP | Stable code | Notes |
|---|---:|---|---|
| reservation cell condition failed | 409 | `booking_slot_conflict` | generic safe detail; include requested interval, not rival identity |
| idempotency key exists, same fingerprint | original success | n/a | replay original booking |
| idempotency key exists, different fingerprint | 409 | `idempotency_key_reused` | caller bug |
| expected booking version/status failed | 409 | `booking_state_conflict` | stale or illegal transition |
| transaction canceled for authorization/input condition | 403/400 | contract-specific | decide before persistence where possible |
| throttling, timeout, unavailable DynamoDB, indeterminate transport result | 503 | `persistence_unavailable` | retry with the same idempotency key; never misreport as 409 |
| malformed time/grid/duration | 400 | `validation_failed` | field errors |

Inspect transaction cancellation reasons or perform targeted strongly consistent reads to distinguish request-key replay from slot contention. Do not expose raw AWS exception text, table names, booking ids belonging to another user, or cancellation-reason internals.

## 5. Indexes and query optimization

### Required for atomicity

None. A GSI cannot enforce uniqueness and is eventually consistent. Only conditional writes on base-table keys inside the transaction provide the exclusion guarantee.

### Recommended after identifier cleanup

The existing Terraform-only `ProviderId-index`/`ClientId-index` lack sort keys (`main.tf:133-143`) and do not support efficient date ranges or ordering. After new booking attributes are populated, consider:

- `BusinessStart-index`: PK `BusinessId`, SK `StartTimeUtc` for a business calendar;
- `ProviderStart-index`: PK `ProviderProfileId`, SK `StartTimeUtc` for all businesses owned by a provider, if that access pattern is required;
- `ClientStart-index`: PK `ClientId`, SK `StartTimeUtc`.

Projection should be selected from measured response fields rather than defaulting to `ALL`. These GSIs optimize listing only and must never participate in conflict decisions.

Creating a DynamoDB GSI is online and does not take a relational-style table lock, but backfill consumes capacity and can throttle production traffic. Add one at a time, monitor table/index status and throttling, and do not cut queries over until `IndexStatus=ACTIVE`. Because no index is required for the P0 correctness fix, index rollout can be deferred.

## 6. Migration, existing data, coexistence, rollback

### 6.1 Expand-and-cutover strategy

1. **Inventory (read-only):** use deployed `DescribeTable`, not Terraform assumptions. Export/backup bookings. Count statuses, missing services/businesses, non-UTC/offsetless timestamps, off-grid starts, non-15-minute durations, invalid/missing ids, future live overlaps, and duplicate legacy meanings.
2. **Expand:** create `booking_reservations`; add application support for optional `BusinessId`, `ProviderProfileId`, and `Version` attributes. DynamoDB needs no ALTER for non-key attributes. Deploy read-compatible code with mutation feature flag off.
3. **Normalize mapping:** for each booking, load `ServiceId`, then `Service.BusinessId`, then `Business.ProviderId`. Populate canonical ids; do not infer them from legacy `Booking.ProviderId` alone.
4. **Resolve dirty data:** group future `pending|confirmed` bookings by canonical `(businessId, resourceId)` and detect half-open overlap. A deterministic report may sort by `CreatedAt`, then `Booking.Id`, but **must not auto-cancel a loser**. Human/product operations choose the valid booking; unresolved overlaps block strict cutover.
5. **Mutation freeze:** briefly reject/pause create, reschedule, cancel, status change, and hard delete while reads continue. This is an application-level freeze, not a DynamoDB table lock.
6. **Backfill:** transactionally write reservation cells for every future live booking with nonexistence conditions and update canonical booking attributes/version. Any collision or malformed record goes to an exception report and blocks cutover.
7. **Switch writers:** route 100% of booking mutations to the new transactional writer, then lift the freeze. Blue/green old instances may remain for reads only.
8. **Verify:** reconcile every future live booking to exactly its expected cells, every cell to one booking, no terminal booking to cells, and no overlapping live bookings. Run concurrency tests against LocalStack and a real AWS test table.
9. **Contract later:** after old readers are retired and rollback window closes, remove legacy write/read dependence on `Booking.ProviderId` and the Scan conflict API. Add query GSIs separately if justified.

### 6.2 Can this migration lock production tables?

No relational lock is taken: creating the new table, writing new attributes, and scanning/backfilling are online DynamoDB operations. However, they can consume read/write capacity and throttle production. The proposed brief **booking mutation freeze** is deliberate application behavior needed to avoid a race with old writers during backfill; other tables and read traffic remain available.

### 6.3 Can old and new versions coexist?

- Reads: yes, during expand, while new attributes are optional and legacy attributes remain.
- Non-booking endpoints: yes, subject to their own compatibility.
- Booking mutations after guard cutover: **no**. An old writer can create/update a booking without reservation rows and invalidate the guarantee. Route all mutation traffic to the new writer or make old instances read-only before lifting the freeze.

### 6.4 Rollback

- Before writer cutover: disable the feature, delete/rebuild the new table if necessary; existing booking table is unchanged except additive backfill attributes.
- After cutover with no new mutations: revert traffic and remove backfilled guards only after verification.
- After any new transactional mutations: do **not** roll back to the old writer. Keep the new write path, disable booking mutations if necessary, and roll forward. Old code cannot safely interpret releases/reschedules or maintain guards. The new table and additive attributes should remain through the rollback window.

No automated rollback may delete reservation cells without checking `BookingId`; doing so can free another booking’s capacity.

## 7. Concurrency and migration tests (release-blocking)

Use separate test data per case and a barrier (`TaskCompletionSource`/`Barrier`) so requests reach the transaction concurrently. Tests must assert persisted state, not only HTTP counts.

1. **Identical create, distinct idempotency keys:** N=20 simultaneous requests for same business/resource/start → exactly 1×201 and 19×409; one booking row; expected slot count; every slot points to winner.
2. **Identical retry, same idempotency key/fingerprint:** N=20 → all return the same booking id/representation (one 201 commit plus replayed success semantics); one booking and one cell set.
3. **Same idempotency key, different start/service:** exactly one mutation; others 409 `idempotency_key_reused`.
4. **Partial overlap:** 09:00–10:00 vs 09:45–10:15 → exactly one success. Adjacent 09:00–10:00 and 10:00–10:30 → both succeed.
5. **Pending conflict:** a persisted pending booking blocks create with 409. Confirming it does not release cells.
6. **Cancellation:** booking status and slot deletion commit together; immediate concurrent rebook succeeds only after cancellation commit. Inject transaction failure and assert neither side changes.
7. **Cancel vs confirm:** concurrent operations with same expected version → exactly one commits; final booking/slots match the winning state.
8. **Reschedule vs competing create:** exactly one owns each contested new cell; failed reschedule retains all old cells and old booking times.
9. **Reschedule fault injection:** no mixed old/new cell set is observable after transaction cancellation.
10. **Transient/ambiguous response retry:** retry same idempotency key after simulated response loss returns the committed booking, never a duplicate and never a false 409.
11. **Cross-business isolation:** same time under two business ids succeeds twice. Two services under one business conflict.
12. **Normalization:** equivalent instants with different offsets create identical UTC cell keys; offset-less, off-grid, or non-aligned duration requests fail 400.
13. **Backfill fixture:** includes overlapping live rows, terminal rows, missing Service/Business, malformed timestamps, and legacy `ProviderId` mismatch. Cutover proceeds only when exception count is zero.
14. **Real AWS parity:** repeat cases 1, 4, 6, and 8 against a disposable real DynamoDB environment; LocalStack alone is not proof of AWS transaction/cancellation behavior.

Invariant query after every mutating test:

```text
For every future booking in {pending, confirmed}:
  actual owned slots == deterministic ExpectedSlots(booking)
For every terminal booking:
  owned slots == empty
For every reservation slot:
  exactly one existing live booking owns it
```

## 8. Explicit unknowns / decisions required

| ID | Unknown | Interim contract | Release impact |
|---|---|---|---|
| DB-D1 | Multi-staff/room resources (product E4) | one exclusive resource per business; `resourceId=single` | Changing before launch changes key scope/backfill |
| DB-D2 | Is 15-minute duration alignment acceptable for every service? | yes; matches current start validator; reject/repair exceptions | Must inventory existing service durations before cutover |
| DB-D3 | Do pending holds expire automatically? | no expiry; pending blocks until action/cancel | If yes, define hold duration and active worker; TTL alone is insufficient |
| DB-D4 | Existing overlapping live bookings | no automatic winner/cancellation | Human operations must resolve before strict cutover |
| DB-D5 | Deployed production GSIs and billing/capacity mode | unknown until `DescribeTable` | Does not block atomicity; affects query rollout/backfill rate |
| DB-D6 | Idempotency replay retention beyond lifecycle +24h | lifecycle +24h minimum | Longer retention is cost/product policy |
| DB-D7 | Historical timestamps lacking offset/UTC kind | derive only with documented source timezone; never guess | Unresolvable future live rows block cutover |

## 9. Scope and preserved work

This artifact changes only database/concurrency design. It does not implement code, alter payment reconciliation, or redesign the broader domain/API. `frontend/payment-reconciliation-mvp` is excluded. Existing dirty application/infrastructure work, including `backend/BookSpot.Application/BookSpot.Application.csproj`, was not modified, stashed, reverted, or committed.
