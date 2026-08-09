# Payment Reconciliation MVP (SME demo)

A simple web-based reconciliation tool where users upload:
1. Bank statement CSV
2. Internal system transactions CSV

The tool classifies transactions as:
- Matched
- Missing in bank
- Missing in system

## Project structure

```text
payment-reconciliation-mvp/
  backend/
    src/PaymentReconciliation.Api/
    tests/PaymentReconciliation.Tests/
  frontend/
  sample-data/
```

## Backend (.NET 8 Web API)

### Run

```bash
cd backend/src/PaymentReconciliation.Api
dotnet restore
dotnet run
```

API base URL: `http://localhost:5000/api`

### Endpoints

- `POST /api/upload/bank` (multipart form with `file`)
- `POST /api/upload/system` (multipart form with `file`)
- `POST /api/reconcile`
- `GET /api/results`

## Frontend (React + Vite)

### Run

```bash
cd frontend
npm install
npm run dev
```

UI URL: `http://localhost:5173`

## CSV assumptions and mapping

### Supported columns
- Required: `Date`, `Amount`
- Optional: `Reference`, `Description`

Headers are matched case-insensitively.

### Normalization behavior
- Date parsing supports formats like:
  - `yyyy-MM-dd`
  - `dd/MM/yyyy`
  - `MM/dd/yyyy`
  - `dd-MM-yyyy`
- Amount parsing supports:
  - dot decimal (`1234.56`)
  - comma decimal (`1.234,56`)
- Whitespace is trimmed
- References are compared case-insensitively

### Parse error handling
Rows with parse errors are skipped and surfaced in API/UI parse errors.

## Matching logic v1

- Amount must match exactly (`decimal` safe)
- Date must be within ±2 days
- If multiple candidates exist, matching priority:
  1. Exact reference match (case-insensitive) when reference exists
  2. Closest date
  3. First remaining unmatched candidate by source row
- One-to-many matching is prevented
- Duplicate rows are flagged during parsing and noted in unmatched reasons

## Tests

```bash
cd backend/tests/PaymentReconciliation.Tests
dotnet test
```

Unit tests cover:
- reference-prioritized candidate selection
- closest-date fallback
- unmatched classification

## Sample files

Use the provided CSVs for demo:
- `sample-data/bank-sample.csv`
- `sample-data/system-sample.csv`

## Current limitations

- In-memory state only (single-process, resets on restart)
- No authentication / authorization
- No persistence or audit log
- No asynchronous/background processing for large files
- No pagination for large results

## Next production steps

1. Add persistence (SQL + reconciliation batch history)
2. Add auth/roles and tenant scoping
3. Add stronger CSV schema config and field mapping UI
4. Add idempotent batch IDs and object storage for raw files
5. Add observability (structured logs, metrics, tracing)
6. Add richer test coverage (parsing edge cases and API integration tests)

## Manual steps remaining

Because this environment did not have the .NET SDK installed, the following must be run locally to fully validate:

1. `dotnet restore` and `dotnet run` for backend
2. `dotnet test` for backend unit tests
3. `npm install` and `npm run dev` for frontend
4. End-to-end upload/reconcile demo in browser
