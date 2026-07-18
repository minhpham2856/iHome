# Landlord Dashboard — Implementation Plan

**Generated from:** `docs/superpowers/specs/2026-07-10-landlord-dashboard-design.md`
**Branch:** minhpn (not main)

Execute top-to-bottom. Each task: mark in_progress, do the steps, run the
verification, mark completed. Stop and ask if anything is unclear or a build
step fails repeatedly.

---

## Task 1 — Fix the two broken repositories (convention: no injection)

Apply the project rule: every repository builds its own `IHomeDbContext`.

Steps:
1. Open `iHome.DAL/Repositories/BuildingRepository.cs`.
   - Its ctor currently does `_context = context;` (undefined `context` → compile error).
   - Change to `public BuildingRepository() { _context = new IHomeDbContext(); }`.
2. Open `iHome.DAL/Repositories/PropertyRepository.cs`.
   - Its ctor currently does `_context = new HomeDbContext();` (typo, wrong type).
   - Change to `public PropertyRepository() { _context = new IHomeDbContext(); }`.

Verification: file contents match the `new IHomeDbContext()` shape; no
`IHomeDbContext context` constructor parameter remains anywhere in `iHome.DAL`.

## Task 2 — Add the data-layer repositories (DAL)

Each new repo: class `<Entity>Repository` in namespace `iHome.DAL.Repositories`,
parameterless ctor assigning `new IHomeDbContext()`, single-line `//` comments.

1. `RoomRepository.cs` — `GetAll()`, and a way to count by `Status`
   (Occupied / Vacant / Maintenance).
2. `ContractRepository.cs` — `GetAll()`, count by `Status`
   (Active / Expired / Terminated), and `ExpiringSoon(int days)` returning
   contracts with `Status == Active` and `EndDate` within `days` from today.
3. `TenantRepository.cs` — `Count()`.
4. `InvoiceRepository.cs` — `OutstandingTotal()` (sum `TotalAmount` where
   status != paid), `OverdueCount()` (DueDate < today and status != paid).
5. `PaymentRepository.cs` — `SumForMonth(int year, int month)` (sum `Amount`
   for `CreatedAt` in that month).

Verification: `grep -rn "IHomeDbContext context"` in `iHome.DAL` returns
nothing; every new repo ctor uses `new IHomeDbContext()`.

## Task 3 — Create the DashboardData DTO (BLL)

1. Create folder `iHome.BLL/DTOs/`.
2. `DashboardData.cs`, namespace `iHome.BLL.DTOs`. Public class with:
   - KPI fields: `TotalBuildings, TotalRooms, OccupiedRooms, VacantRooms,
     TotalTenants, ActiveContracts, MonthlyRevenue, OutstandingAmount,
     OverdueInvoices, ExpiringContracts` (int/decimal as appropriate).
   - Chart series: `RevenueSeries` (list of month-label + amount),
     `RoomStatusSeries` (label + count), `ContractStatusSeries` (label + count).
   Use a small `ChartPoint` (Label, Value) type in the same namespace.

Verification: file is under `iHome.BLL/DTOs`, namespace is exactly
`iHome.BLL.DTOs`.

## Task 4 — Implement DashboardService (BLL)

1. `iHome.BLL/Services/DashboardService.cs`. Constructor news up the repos
   with `new` (no DI): `private readonly RoomRepository _rooms = new();` etc.
2. `public DashboardData GetDashboardData()`:
   - Compute each KPI from the repos.
   - Build `RevenueSeries` for the last 12 months via `PaymentRepository.SumForMonth`.
   - Build `RoomStatusSeries` (Occupied/Vacant/Maintenance counts) and
     `ContractStatusSeries` (Active/Expired/Terminated counts).
   - Centralize the status literals as `private const string` (e.g.
     `RoomOccupied = "Occupied"` — adjust to match the actual DB values).
   - Single-line `//` comments on the non-obvious aggregation steps.

Verification: method returns a fully-populated `DashboardData` (no null series);
no `///` XML docs; no constructor-injected repos.

## Task 5 — Add LiveCharts2 packages (UI)

1. Edit `iHome.UI/iHome.UI.csproj`: add `PackageReference` for
   `LiveCharts2` and `LiveCharts2.SkiaSharpView.WPF` (matching the
   installed EF Core 8 package-reference style).

Verification: packages referenced; `dotnet build iHome.UI` restores them without
error (or note if NuGet restore needs network).

## Task 6 — Add a CardStyle to Styles.xaml (UI)

1. In `iHome.UI/Styles.xaml`, add a reusable `CardStyle` (surface
   background, border, padding) plus maybe a `KpiValue` text style, reusing
   existing palette keys (`ColorSurface`, `ColorBorder`, `ColorTextPrimary`).
2. Leave the existing `SidebarButtonStyle` untouched.

Verification: XAML still parses (build succeeds); new keys referenced by the
dashboard only.

## Task 7 — Implement DashboardPage.xaml (UI)

1. Replace the placeholder `Views/landlord/DashboardPage.xaml` body with a
   scrollable `Grid`:
   - Top row: a `UniformGrid` / `WrapPanel` of KPI cards. Each card is a
     `Border` using `CardStyle` with a title `TextBlock` + a large value
     `TextBlock`, named so the code-behind can set them.
   - Bottom row: three chart hosts — `CartesianChart` (revenue column),
     `PieChart` (room status doughnut), `PieChart` (contract status pie),
     with the LiveCharts2 WPF namespace declared.
   - Reuse `Styles.xaml` palette colors for any brushes.
2. Leave single-line `//` placeholders for not-yet-built interactions:
   KPI card click → drill into detail page; a refresh button; navigation to the
   still-empty `BuildingsPage`/`ContractsPage`.

Verification: XAML compiles; all named elements the code-behind will touch exist.

## Task 8 — Implement DashboardPage.xaml.cs (UI)

1. In the constructor (or `Loaded` handler), wrap
   `var data = new DashboardService().GetDashboardData();` in try/catch.
2. On success: set each KPI `TextBlock.Text` from `data`; build the three
   chart `Series` from `data.RevenueSeries` / `RoomStatusSeries` /
   `ContractStatusSeries` (assign to the chart controls' `Series` property).
3. On failure: show a friendly `MessageBox` / `TextBlock`
   ("Không thể tải dữ liệu") instead of crashing.
4. Single-line `//` comments on the chart-series construction.

Verification: compiles; logic mirrors `DashboardData` fields exactly.

## Task 9 — Build & fix

1. `dotnet build iHome.sln` (this is Windows; the UI is `net8.0-windows`).
2. Resolve any compile errors (most likely: namespace typos, missing
   LiveCharts2 namespace, wrong DTO namespace).
3. Do NOT run the app if there is no display; report that manual run-verification
   (login as Landlord → dashboard) is the remaining manual step.

Verification: solution builds with zero errors.

## Task 10 — Finish

Invoke `superpowers:finishing-a-development-branch` to verify, present
options, and execute the chosen completion (commit / PR).
