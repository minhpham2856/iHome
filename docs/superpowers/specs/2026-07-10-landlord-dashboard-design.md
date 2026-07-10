# Landlord Dashboard — Design

**Date:** 2026-07-10
**Module:** Landlord (role-based)
**Status:** Approved-for-build (generated from brainstorming alignment)

## Goal

Make `Views/Landlord/DashboardPage` a fully functional landing page for the
Landlord role: a row of KPI stat cards plus three charts, all fed by real
aggregated data from the SQL Server `iHomeDB` via EF Core. Anything that
depends on a not-yet-built feature (e.g. drilling into an empty detail page)
is left as a single-line `//` placeholder, per project convention.

## Scope & assumptions

- **Single landlord for now** — data is global (all rows in the DB), keyed only
  on the role string. No per-user filtering / session concept is required, so
  `DashboardWindow` passing only `Role` to the page is sufficient.
- **Status strings** (`Room.Status`, `Contract.Status`, `Invoice.Status`) must
  match the seeded DB. Their literal values are centralized as `const` in
  `DashboardService` so they are trivial to correct if wrong.

## What the dashboard shows

### KPI stat cards (top row)
- Total Buildings, Total Rooms
- Occupied rooms, Vacant rooms (Maintenance rooms also counted for totals)
- Total Tenants, Active Contracts
- This month's Revenue, Outstanding (unpaid) amount
- Alert metrics: Overdue invoices count, Contracts expiring soon

### Charts (LiveCharts2, bottom row)
1. **Revenue over time** — column chart, last 12 months
2. **Room status breakdown** — doughnut (Occupied / Vacant / Maintenance)
3. **Contract status breakdown** — pie (Active / Expired / Terminated)

## Architecture & data flow

```
DashboardPage.xaml(.cs)            UI (net8.0-windows, WPF)
   │  calls on load, binds result
   ▼
DashboardService                  iHome.BLL/Services   (news up repos, no DI)
   │  returns
   ▼
DashboardData                    iHome.BLL/DTOs       (namespace iHome.BLL.DTOs)
   │  built from
   ▼
RoomRepository / ContractRepository / TenantRepository /
InvoiceRepository / PaymentRepository / BuildingRepository
   │  each: parameterless ctor → new IHomeDbContext()
   ▼
IHomeDbContext  →  SQL Server iHomeDB
```

- No dependency injection. `DashboardService` instantiates each repository with
  `new` (matching `AuthService`). Each repository builds its own
  `IHomeDbContext` in a parameterless constructor (the project convention the
  user re-affirmed — constructor injection was removed on purpose).
- `DashboardService.GetDashboardData()` returns one `DashboardData` DTO; the
  page binds cards and chart series to it. UI code-behind stays thin.

## Conventions applied (from ihome-coding-conventions skill)

- Single-line `//` comments only; generous on aggregation / chart-series logic;
  no `///` in hand-written code, no `/* */`.
- Repositories: `<Entity>Repository` in `iHome.DAL.Repositories`, ctor
  `public Repo() { _context = new IHomeDbContext(); }`. (Watch the
  `new HomeDbContext()` typo — compile error.)
- DTOs in `iHome.BLL/DTOs`, namespace `iHome.BLL.DTOs` (not `Models`).
- Services in `iHome.BLL/Services`, `new` up repos.
- Shared styles live in `Styles.xaml`; reuse the color palette there.

## Error handling

- The service call in the page's load is wrapped in try/catch. On failure
  (e.g. DB unreachable — the connection string is still hardcoded to
  `DESKTOP-2JNFOT5` / `iHomeDB`) show a friendly
  "Không thể tải dữ liệu" message instead of crashing.
- No test framework exists; verification is manual (build + run, log in as
  Landlord, confirm cards/charts populate).

## Placeholders (single-line `//` comments)

Left for not-yet-built dependencies:
- KPI card click → drill into a detail page (BuildingsPage / ContractsPage / etc.
  are currently empty stubs).
- A refresh button.
- Any navigation to the still-empty role pages.

The dashboard's **data and charts are fully functional**; only cross-page links
are stubbed.
