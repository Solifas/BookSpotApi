# BookSpot release-readiness review

**Ticket:** `t_5627583e`  
**Review time:** 2026-08-16 20:17 SAST  
**Decision:** **NOT RELEASE READY**

## Executive decision

Release is blocked. The frontend and backend remediation work is not integrated into a single releasable revision, the backend implementation remains uncommitted in its task worktree, and the backend owner explicitly records unimplemented release-critical authentication and booking work. Docker/LocalStack was unavailable, so datastore-backed end-to-end, authorization, recovery, and concurrency tests could not run. Independent gates also found failing frontend lint and production dependency audits with High vulnerabilities.

The passing unit/component suites are useful evidence of progress, but they do not satisfy the ticket's full-stack, adversarial, or concurrency acceptance criteria.

## Release-blocking evidence

1. **No integrated candidate exists.** Frontend remediation is committed at `dc1541d` on `frontend/t_6cf4f26b`; backend remediation is an uncommitted 38-file change set in `backend/t_4a198fdc` based on `f8b2fd0`. There is no single commit containing and exercising both.
2. **Backend implementation is explicitly partial.** `docs/backend-remediation-migration-notes-2026-08-14.md` records missing abuse counters, recovery queue/capability/audit/outbox/KMS work, transactional booking action/reschedule endpoints, schedule/timezone enforcement, complete ownership remediation, and LocalStack/real-AWS integration execution.
3. **Required runtime environment was unavailable.** Docker Desktop's Linux engine pipe did not exist; LocalStack `:4566` and the API `:5000` were initially unavailable. The API could start independently, but datastore-backed `/services` did not return within the bounded probe because LocalStack was absent.
4. **Concurrency is not release-certified.** Transactional booking-create code and unit-level contract tests exist, but the required two-request LocalStack/real-AWS test was not executable. Explicit action/reschedule transactional paths are not implemented.
5. **Password recovery is incomplete.** An arbitrary token now returns the stable `400 reset_token_invalid` envelope, but encrypted delivery/outbox/capability/audit/replay and abuse-control requirements remain unimplemented.
6. **Frontend lint fails.** `npm run lint` returned 8 errors and 10 warnings.
7. **High dependency vulnerabilities remain.** `npm audit --omit=dev` reported 12 production vulnerabilities (10 High, 2 Moderate), including direct `react-router-dom` and `postcss` findings. The backend audit reported two High transitive vulnerabilities in the test project (`System.Net.Http 4.3.0`, `System.Text.RegularExpressions 4.3.0`).
8. **No E2E/adversarial suite ran.** Registration, login, recovery, profile/settings persistence, dashboards, booking transitions, horizontal access, role escalation, token replay, account enumeration, identifier substitution, cross-user booking access, and double-booking were not exercised against one running integrated candidate.

## Independent verification results

| Gate | Result | Evidence |
|---|---|---|
| Backend build | PASS with warning | `dotnet build BookSpot.sln --no-restore`; 0 errors, unresolved `Microsoft.Extensions.Configuration.Abstractions` reference warning |
| Backend tests | PASS, insufficient scope | 27/27 xUnit tests; predominantly contract/unit tests, not live LocalStack/API E2E |
| Backend dependency audit | FAIL | Two High transitive advisories in `BookSpot.Tests` |
| Frontend tests | PASS | 9 files, 32/32 Vitest tests |
| Frontend TypeScript | PASS | `npx tsc -p tsconfig.app.json --noEmit` |
| Frontend build | PASS with warning | 823.45 kB main JS chunk; stale Browserslist data |
| Frontend lint | FAIL | 8 errors, 10 warnings |
| Frontend production dependency audit | FAIL | 12 vulnerabilities: 10 High, 2 Moderate |
| LocalStack/DynamoDB | BLOCKED | Docker Desktop engine unavailable; `localhost:4566` unreachable |
| API startup | PASS (without datastore) | Kestrel listened on `http://localhost:5000` |
| Swagger surface | PASS/partial | `GET /swagger/v1/swagger.json` returned 200; registration and body-based reset validation are present |
| Anonymous profile administration | PASS for one smoke probe | `POST /profiles` without token returned 401 and `no-store` headers |
| Public diagnostics | PASS for one smoke probe | `GET /test/exception/test` without token returned 401 |
| Invalid reset token | PASS for one smoke probe | `POST /auth/validate-reset-token` with `anything` returned stable 400 `reset_token_invalid` |
| Integrated E2E/security/concurrency | NOT RUN / RELEASE BLOCKER | No datastore and no integrated candidate |
| Payment-reconciliation scope | PASS | Frontend task diff contained no `frontend/payment-reconciliation-mvp` changes; backend task has no payment subtree changes |

## Reconciliation of all 26 assessment findings

`CLOSED` requires independently executed evidence against an integrated candidate. `PARTIAL` means remediation exists but the original exploit/flow was not fully re-tested. `OPEN` means missing implementation or a failing gate remains.

| ID | Severity | Status | Current evidence / residual risk |
|---|---|---|---|
| C1 | Critical | PARTIAL — blocks release | Registration route and tests exist, but no datastore-backed registration E2E ran; backend branch is uncommitted and not integrated with frontend. |
| C2 | Critical | PARTIAL — blocks release | Anonymous `POST /profiles` returned 401 and profile DTO tests exclude hashes. Full anonymous CRUD, horizontal access, role mutation, and leakage matrix was not run. |
| C3 | Critical | OPEN | Generic booking mutation routes were removed, but explicit transactional action/reschedule endpoints are not implemented or E2E-tested. |
| C4 | Critical | PARTIAL — blocks release | Service ownership code was changed, but provider → business → service behavior and identifier substitution were not run against DynamoDB. |
| C5 | Critical | OPEN | Invalid-token handling improved, but recovery delivery/outbox/capability/audit/replay/abuse controls and end-to-end reset-login are incomplete. |
| H1 | High | OPEN | Transactional create code exists; no real LocalStack/AWS two-request proof, and action/reschedule transactions remain absent. |
| H2 | High | PARTIAL — blocks release certification | Reproducible setup was previously reported, but this independent clean-run gate could not start Docker/LocalStack. New additive tables were not independently provisioned. |
| H3 | High | PARTIAL — blocks release | JWT/session controls changed and unit tests pass; production secret/startup behavior and rotation were not executed in a production-like configuration. |
| H4 | High | PARTIAL | Frontend booking route changes are committed and tests pass; browser navigation against a live integrated API was not run. |
| H5 | High | PARTIAL | Frontend booking implementation changed, but persistence confirmation through a browser/network trace was not possible. |
| H6 | High | PARTIAL | Settings tests pass, but live provider business load/update persistence and ownership were not exercised. |
| H7 | High | OPEN | Frontend live-availability work exists, but backend schedule/timezone enforcement is explicitly incomplete and no atomic runtime validation ran. |
| H8 | High | PARTIAL — blocks release | Global fallback authorization and smoke probes improved. Complete business/service/hour/review ownership remediation is explicitly unfinished. |
| H9 | High | PARTIAL | Frontend dashboard tests pass; live DTO parity and proof that all release paths are non-fabricated were not obtained. |
| H10 | High | PARTIAL | API parsing/configuration tests pass; integrated empty/text/malformed/204 behavior was not browser-tested. |
| H11 | High | PARTIAL — blocks release | Minimal booking intent/server-derived fields have tests, but immutable persistence and live historical-record behavior were not independently verified. |
| M1 | Medium | OPEN | No generated-client/OpenAPI parity gate or live search pagination/ordering verification was completed. |
| M2 | Medium | OPEN | Scan-heavy access patterns remain visible; scale-readiness work is not complete. |
| M3 | Medium | PARTIAL | Dashboard/settings remediation exists, but mock service/data-source code remains in the frontend tree and no browser sweep proved all visible release controls are live. |
| M4 | Medium | PARTIAL | Adapter changes exist; live service presentation was not checked against real backend DTOs. |
| M5 | Medium | PARTIAL | Role-specific UI tests exist; integrated client/provider navigation and terminology were not browser-verified. |
| M6 | Medium | PARTIAL | Public test endpoint returned 401 and reset validation moved to the body. Structured path redaction and all diagnostic-production gates were not fully verified. |
| M7 | Medium | OPEN | Automated tests now exist, but no E2E/concurrency suite ran and frontend lint still fails (8 errors, 10 warnings). |
| M8 | Medium | OPEN | Production audit has 12 vulnerabilities, including 10 High; bundle remains 823.45 kB. |
| L1 | Low | OPEN | Logging/noise/redaction was not fully remediated or verified. |
| L2 | Low | PARTIAL | Setup/contract documentation improved, but the clean-run could not be reproduced on this host and source remains split across task branches. |

### Severity disposition

- **Critical:** 0 independently closed; 3 partial; 2 open.
- **High:** 0 independently closed; 9 partial; 2 open.
- **Medium:** 0 independently closed; 4 partial; 4 open.
- **Low:** 0 independently closed; 1 partial; 1 open.

## Exact reproduction commands

Backend task worktree:

```bash
cd C:/Users/Optimus/AppData/Local/hermes/kanban/workspaces/t_4a198fdc
dotnet build BookSpot.sln --no-restore
dotnet test BookSpot.Tests/BookSpot.Tests.csproj --no-build --logger 'console;verbosity=normal'
dotnet list BookSpot.sln package --vulnerable --include-transitive
git status --short --branch
docker version --format '{{.Server.Version}}'
dotnet run --project BookSpot.API/BookSpot.API.csproj --no-build
```

Runtime smoke probes after the API listens:

```bash
curl -i -X POST http://localhost:5000/profiles \
  -H 'Content-Type: application/json' -d '{}'
curl -i http://localhost:5000/test/exception/test
curl -i -X POST http://localhost:5000/auth/validate-reset-token \
  -H 'Content-Type: application/json' -d '{"token":"anything"}'
```

Frontend task worktree:

```bash
cd C:/Users/Optimus/AppData/Local/hermes/kanban/workspaces/t_6cf4f26b/frontend
npm test -- --run
npx tsc -p tsconfig.app.json --noEmit
npm run build
npm run lint
npm audit --omit=dev --json
```

Integration-state proof:

```bash
git -C C:/Repository worktree list --porcelain
git -C C:/Users/Optimus/AppData/Local/hermes/kanban/workspaces/t_4a198fdc status --short --branch
git -C C:/Users/Optimus/AppData/Local/hermes/kanban/workspaces/t_6cf4f26b log -1 --oneline
```

## Minimum path to a release candidate

1. Finish, review, commit, and push backend remediation; resolve the protected project-reference warning without overwriting unrelated user work.
2. Implement the remaining recovery, abuse-control, ownership, booking-action/reschedule, schedule/timezone, and migration/cutover requirements.
3. Integrate backend and frontend branches into one candidate revision.
4. Start the documented clean LocalStack environment and prove additive table provisioning.
5. Run live API integration tests and browser E2E for every ticket journey plus the full adversarial matrix.
6. Run a real two-request slot collision test and prove exactly one success plus immutable idempotent replay.
7. Fix frontend lint and disposition/upgrade all High production dependency advisories.
8. Re-run this review against the exact release-candidate commit.

## Recommendation

**NOT RELEASE READY.** Critical and High security/integrity requirements remain open or only partially verified, and the required integrated runtime evidence does not exist. Do not promote this revision to UAT or production approval.
