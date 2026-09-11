# Accounting System

A full double-entry accounting system built on **.NET 8 / ASP.NET Core Web API**, using Clean Architecture (Domain / Application / Infrastructure / Api).

## Modules

- **General Ledger** — Chart of Accounts, manual & system-generated journal entries, balanced double-entry validation, reversals, fiscal year/period management and year-end closing.
- **Accounts Receivable** — Customers, invoices (with line-level tax and item integration), customer payments with multi-invoice application, credit memos, AR aging.
- **Accounts Payable** — Vendors, bills, vendor payments with multi-bill application, vendor credits, AP aging.
- **Banking** — Bank accounts, transactions, bank reconciliation workflow.
- **Inventory** — Inventory/service/non-inventory items, weighted-average costing, automatic COGS posting on sale, automatic stock receipt on purchase.
- **Fixed Assets** — Straight-line and declining-balance depreciation, monthly depreciation runs, disposals.
- **Budgeting** — Budgets by account/fiscal period.
- **Tax** — Configurable tax codes applied to invoice/bill lines, posted to tax payable/receivable accounts.
- **Multi-currency** — Per-document currency and exchange rate fields; base-currency company setting.
- **Reporting** — Trial Balance, Income Statement (P&L), Balance Sheet, General Ledger detail, AR/AP Aging, simplified Cash Flow Statement.
- **Security** — ASP.NET Core Identity + JWT bearer auth, role-based authorization (Admin, Accountant, ARClerk, APClerk, Viewer).
- **Audit** — Created/modified by & at on every record; append-only journal entries (voided via reversal, never deleted).

## Architecture

```
src/
  AccountingSystem.Domain          Entities, enums, domain exceptions. No dependencies.
  AccountingSystem.Application     Business logic services, DTOs, IApplicationDbContext abstraction.
  AccountingSystem.Infrastructure  EF Core DbContext + configurations, ASP.NET Identity, migrations.
  AccountingSystem.Api             REST controllers, JWT auth, Swagger.
tests/
  AccountingSystem.Application.Tests  Unit tests (EF Core InMemory) for posting logic and reports.
```

Every financial transaction (invoice, bill, payment, depreciation run, period close) posts through a single `JournalEntryService` that enforces:
- lines balance (total debits == total credits)
- the entry date falls in an **open** fiscal period
- entries are never hard-deleted — corrections are made by reversal

## Running locally

Requires the .NET 8 SDK.

```bash
cd src/AccountingSystem.Api
dotnet run
```

On first run the API automatically applies EF Core migrations and seeds:
- a demo company with a full chart of accounts (tagged with the system-account keys the posting engine relies on: AR, AP, Sales Tax Payable, Purchase Tax Receivable, Retained Earnings, Opening Balance Equity, default COGS/Revenue accounts)
- 12 open fiscal periods for the current year
- USD/EUR/GBP currencies
- an admin user: **admin@demo.local / Admin@12345**

Swagger UI is available at `/swagger` in Development. Authenticate via `POST /api/auth/login`, then use the returned JWT as a Bearer token for every other endpoint.

### Switching database provider

The API defaults to SQLite (`accounting.db`, zero setup). For SQL Server, set in `appsettings.json`:

```json
"DatabaseProvider": "SqlServer",
"ConnectionStrings": { "DefaultConnection": "Server=...;Database=Accounting;..." }
```

### Adding a migration

```bash
cd src/AccountingSystem.Api
dotnet ef migrations add <Name> --project ../AccountingSystem.Infrastructure --startup-project .
```

## Typical flow

1. `POST /api/auth/login` → JWT.
2. `POST /api/customers`, `POST /api/items`, `POST /api/taxcodes` as needed.
3. `POST /api/invoices` (draft) → `POST /api/invoices/{id}/post` (posts AR/Revenue/Tax/COGS to the GL).
4. `POST /api/customerpayments` with `applications` to settle one or more invoices — posts Bank/AR.
5. `GET /api/reports/trial-balance?asOfDate=...`, `.../income-statement`, `.../balance-sheet`, `.../ar-aging`, etc.
6. `POST /api/fiscalperiods/periods/{id}/close` and `.../years/{id}/close` at period/year end.

## Tests

```bash
cd tests/AccountingSystem.Application.Tests
dotnet test
```
