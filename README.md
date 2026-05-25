# Decision OS -- Distribution V1

## What Is This?

A .NET 8 pilot system that gives each distributor a **weekly, actionable view of their business** through:

- **Status:** 7 KPIs scored GREEN / YELLOW / RED against configurable thresholds
- **Priority:** One **Top Alert** KPI per week with ranked drivers explaining why
- **Action:** One **Weekly Focus** -- a concrete decision and recommended action

Designed for **3-5 distributors** in pilot mode with **manual weekly file upload** (CSV/Excel) and deterministic scoring logic.

**Operator guide:** `_client_docs/DecisionOS_Distribution_User_Guide.md` (screen-by-screen, Classic vs Simplified upload, dashboard, **`Company_Profile` tab → Business profile + Tenant** setup).

**Tenant vs buyer:** Each **tenant** is one distributor company (`ClientId`, e.g. `DIST-001`). **End customers (buyers)** exist only on imported rows (`Customer_ID` / `Customer_Name`) — not as separate tenants.

---

## Solution Architecture

```
DecisionOS.Distribution.V1.sln
├── src/
│   ├── DecisionOS.Distribution.Domain/          # Entities, enums, business logic services
│   ├── DecisionOS.Distribution.Infrastructure/   # EF Core DbContext, data access
│   ├── DecisionOS.Distribution.Import/           # CLI tool -- CSV import + processing pipeline
│   └── DecisionOS.Distribution.Web/              # ASP.NET Core (Razor Pages UI + JSON API)
│       ├── Pages/                                # Razor Pages (Index, Dashboard, Layout)
│       └── wwwroot/                              # CSS, JavaScript static assets
├── tests/
│   └── DecisionOS.Distribution.Tests/            # xUnit test suite (workbook + import + dashboard tests)
└── samples/                                      # Sample CSV files for import
```

### Deep dive: how imports become weekly insights

See `docs/Solution-Linkage-and-Import-Analysis.md` for an end-to-end explanation of how the projects connect and how Excel/CSV imports are validated, resolved (Profile → Global → Tenant overrides), and converted into KPI status, Top Alert, drivers, and Weekly Focus.

### Web upload flows (Operations → Uploads)

| Mode | UI path | Input | Outcome |
|------|---------|-------|---------|
| **Simplified** | Create → Simplified → **Detect** → **Verify** → Import all periods | One multi-tab `.xlsx` | Auto-detect sheets/columns; import **many** week-ending dates in one run; holdovers applied to each KPI week |
| **Classic** | Create → Classic → **Details** → Map per file → Validate → Run import | CSV/Excel per report type, one `PeriodEnd` per batch | Operator maps columns; import **one** week per batch |

**Simplified detection rules (V1):** Authoritative week-ending dates come from **Weekly rollup** and **Sales** week columns (plausible years only). AR invoice/due dates are not used as reporting weeks. AR/AP/Inventory detail is staged for the **latest operational week** (last week with sales); rollup supplies per-week KPI inputs where present.

**Dashboard:** `/` selects tenant, optional buyer, and week; `/Dashboard` shows KPIs, top alert, weekly focus, and holdover improvements. Week list excludes invalid legacy dates. Buyer filter scopes the holdover table; KPI cards stay distributor-wide unless data is missing for that week.

### Layer Responsibilities

| Layer | Project | Purpose |
|-------|---------|---------|
| **Domain** | `DecisionOS.Distribution.Domain` | Entities (`Tenant`, `KpiDefinition`, `KpiSnapshot`, `DriverValue`, `Alert`, `WeeklyFocus`), enums (`KpiDirection`), and all business logic services (`KpiStatusService`, `AlertService`, `WeeklyFocusService`, `DriverRankingService`) |
| **Infrastructure** | `DecisionOS.Distribution.Infrastructure` | `DecisionOsDbContext` (EF Core over PostgreSQL), entity configuration, indexes |
| **Import CLI** | `DecisionOS.Distribution.Import` | Console app that reads KPI + driver CSVs, validates, upserts, computes status, WoW deltas, top alerts, and weekly focus |
| **Web** | `DecisionOS.Distribution.Web` | ASP.NET Core with Razor Pages dashboard UI + JSON API endpoints |
| **Tests** | `DecisionOS.Distribution.Tests` | xUnit tests covering all domain services and DbContext |

---

## The 7 Pillar KPIs

| # | Pillar KPI | Code | Unit | Direction | Target | Amber | Red |
|---|-----------|------|------|-----------|--------|-------|-----|
| 1 | Cash Conversion Cycle (CCC) | `CCC` | days | Lower is better | 45 | 55 | 70 |
| 2 | Gross Margin % | `GrossMargin%` | pct | Higher is better | 0.28 | 0.265 | 0.25 |
| 3 | Net Profit % | `NetProfit%` | pct | Higher is better | 0.06 | 0.045 | 0.03 |
| 4 | A/R Health | `AR_PastDue31p%` | pct | Lower is better | 0.12 | 0.15 | 0.20 |
| 5 | Inventory Health (DOH) | `DOH` | days | Lower is better | 45 | 55 | 70 |
| 6 | A/P & Purchasing Efficiency | `AP_PastDue31p%` | pct | Lower is better | 0.10 | 0.12 | 0.18 |
| 7 | Service / Fulfillment | `PerfectOrderRate` | pct | Higher is better | 0.93 | 0.91 | 0.89 |

Definitions are seeded on first import run. Thresholds, narratives, and recommended actions are stored in the database and can be modified.

---

## Domain Services

### KpiStatusService
Computes GREEN / YELLOW / RED per KPI snapshot using the definition's direction and thresholds.

- **Higher is better:** `value >= target` = GREEN, `value >= redThreshold` = YELLOW, else RED
- **Lower is better:** `value <= target` = GREEN, `value <= amberThreshold` = YELLOW, else RED

### AlertService
Selects the **Top Alert** for a tenant-week:

1. Filters to RED or YELLOW snapshots
2. Scores: RED = 3, YELLOW = 2
3. Tie-breaks by largest relative deviation from target: `|value - target| / target`
4. Returns null if all KPIs are GREEN

### WeeklyFocusService
Generates one **Weekly Focus** per tenant-week from the Top Alert:

- `DecisionQuestion` = "Will we address {Pillar Name} this week?"
- `RecommendedAction` = from pillar's KpiDefinition
- `WhyNow` = "{Pillar Name} is {Severity}. {DiagnosticChecks}"
- `Owner` = "Operations", `Cadence` = "Weekly"

### DriverRankingService
Ranks drivers for the Top Alert pillar:

1. Filters to matching `PillarCode`
2. Orders by `Current` descending (highest impact first)
3. Takes top N (default 5)
4. Re-assigns `Rank` = 1..N

---

## How to Run

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL (for Import CLI and Web app -- tests use InMemory provider, no database needed)

### Step 1: Build

```bash
dotnet restore
dotnet build
```

### Step 2: Run Tests (no database needed)

```bash
dotnet test
```

Expected: all tests pass (run `dotnet test` for current count).

### Step 3: Set Up PostgreSQL

Create a PostgreSQL database and user:

```sql
CREATE USER decisionos WITH PASSWORD 'decisionos';
CREATE DATABASE decisionos OWNER decisionos;
```

The Import CLI runs `MigrateAsync()` automatically on first run to create all tables.

**Connection string** is configured in `src/DecisionOS.Distribution.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DecisionOs": "Host=localhost;Port=5432;Database=decisionos;Username=decisionos;Password=decisionos"
  }
}
```

Both the Web app and Import CLI read from `appsettings.json`. The Import CLI also falls back to environment variables or a built-in default if no config file is present.

### Step 4: Import Sample Data

Sample CSV files are included in the `samples/` folder. Run the import CLI:

```bash
dotnet run --project src/DecisionOS.Distribution.Import -- "DIST-001" "2026-02-28" samples/kpi_snapshots_upload.csv samples/driver_values_upload.csv
```

**CLI arguments:**

| # | Argument | Description | Required |
|---|----------|-------------|----------|
| 1 | `client_id` | Distributor identifier (tenant auto-created if new) | Yes |
| 2 | `period_end` | Week ending date in `YYYY-MM-DD` format | Yes |
| 3 | `kpi_csv_path` | Path to KPI snapshots CSV file | Yes |
| 4 | `drivers_csv_path` | Path to driver values CSV file | No |

**What the import pipeline does:**

1. Upserts the tenant (creates if new)
2. Seeds 7 KPI definitions with thresholds and narratives (first run only)
3. Imports KPI snapshots and computes GREEN / YELLOW / RED status
4. Imports driver values (if CSV provided)
5. Computes week-over-week deltas (if previous week's data exists)
6. Generates the Top Alert for the week
7. Generates the Weekly Focus recommendation

### Step 5: Start the Web Application

```bash
dotnet run --project src/DecisionOS.Distribution.Web
```

Default URL with `run.ps1` is **http://localhost:5276** (or the port you set). Bare `dotnet run` on the Web project may use port 5000.

### Step 6: Use the Dashboard

1. The **Index page** shows distributor, optional **buyer**, and **reporting week** selectors
2. Select a distributor -- week and buyer lists load from imported data
3. Select a week -- you are redirected to the **Dashboard page** showing:
   - **Summary tiles** (GREEN / YELLOW / RED counts)
   - **Top Alert banner** with severity and reason
   - **KPI Grid** (7 tiles with values, status badges, targets, WoW deltas)
   - **Weekly Focus card** (decision question, recommended action, why now, owner)
   - **Holdover Improvements** table (imported holdover drivers; optional buyer filter)
   - **Driver drilldown** / ranked drivers for the alert pillar where configured

### Step 7: Simplified workbook import (Web UI)

For a multi-week distributor workbook (pilot template):

1. Sign in as **Operator** or **Admin**
2. **Operations → Uploads → New Batch** → **Simplified** → tenant + anchor date → **Continue**
3. Upload `.xlsx` on **Detect** → **Analyze workbook**
4. On **Verify**: review periods and sheets → **Validate package** → **Import all periods**
5. Open **Dashboard** for that tenant; choose a week from the dropdown (week-ending dates from your file, e.g. Saturdays)

Fixture used in tests: `tests/DecisionOS.Distribution.Tests/Fixtures/Steves_Bowling_Supply_DPOS_Distribution_Test_Data.xlsx`

**Reset tenant import data (PostgreSQL):** `scripts/sql/clear-tenant-import-data.sql` (edit `ClientId` if not `DIST-001`). Optional: `cleanup-bad-periods.sql`, `holdover-dashboard-diagnostic.sql`.

---

## Sample CSV Formats

### `kpi_snapshots_upload.csv`

One row per KPI, 7 rows total:

```csv
kpi_code,value
CCC,52
GrossMargin%,0.27
NetProfit%,0.05
AR_PastDue31p%,0.14
DOH,48
AP_PastDue31p%,0.11
PerfectOrderRate,0.92
```

### `driver_values_upload.csv`

Multiple rows per pillar with driver details:

```csv
pillar_code,driver_name,dimension1,dimension2,current,wow,context,rank,status,why_it_matters
AR_PastDue31p%,Acme Corp,Customer,Northeast,45000,-2000,18% of total AR,1,RED,Largest single overdue balance
AR_PastDue31p%,Beta Industries,Customer,Southeast,32000,5000,12% of total AR,2,YELLOW,Fastest growing overdue
CCC,Days Sales Outstanding,DSO Component,,35,2,Drives 67% of CCC,1,YELLOW,DSO up 2 days
```

Pre-built sample files are in the `samples/` folder ready to use.

---

## Dashboard UI

The web application includes a full Razor Pages dashboard:

| Page | URL | Description |
|------|-----|-------------|
| **Index** | `/` | Distributor, buyer, and week selector |
| **Dashboard** | `/Dashboard?clientId=X&periodEnd=Y&customerId=…` | Executive dashboard (optional buyer filter) |
| **Uploads** | `/Operations/Uploads` | Batch list (Classic + Simplified) |
| **Simplified Detect** | `/Operations/Uploads/Simplified/Detect?id=…` | Upload and analyze workbook |
| **Simplified Verify** | `/Operations/Uploads/Simplified/Verify?id=…` | Validate and import all periods |

**Design features:**
- Modern SaaS aesthetic (clean cards, subtle shadows, slate/neutral palette)
- Color-coded KPI tiles with GREEN / YELLOW / RED left borders and status badges
- Responsive CSS Grid layout (4 columns desktop, 2 tablet, 1 mobile)
- Mobile-friendly with hamburger nav, touch-friendly tap targets (44px min)
- Top Alert banner with severity-colored accent
- Weekly Focus card with decision question, action, and metadata pills
- Driver drilldown table with horizontal scroll on mobile
- Print stylesheet (hides nav, optimizes for paper)
- Zero external dependencies (no CDN, no frameworks -- pure CSS + vanilla JS)

---

## API Endpoints

The JSON API is still available alongside the UI:

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/tenants` | List all tenants |
| `GET` | `/api/tenants/{clientId}/weeks` | List available week periods for a tenant |
| `GET` | `/api/tenants/{clientId}/weeks/{periodEnd}` | Full dashboard data for a tenant-week |

### Dashboard Response Shape (`GET /api/tenants/{clientId}/weeks/{periodEnd}`)

```json
{
  "tenant": { "clientId": "DIST-001", "name": "DIST-001", "archetype": null },
  "periodEnd": "2026-02-28",
  "kpis": [
    { "name": "A/R Health", "code": "AR_PastDue31p%", "value": 0.14, "status": "YELLOW", "weekOverWeekDelta": 0.02 }
  ],
  "topAlert": {
    "pillar": "A/R Health",
    "severity": "YELLOW",
    "reasonSummary": "A/R Health is YELLOW at 0.14 (target: 0.12)"
  },
  "drivers": [
    { "pillarCode": "AR_PastDue31p%", "driverName": "Acme Corp", "current": 45000, "rank": 1, "status": "RED", "whyItMatters": "Largest overdue balance" }
  ],
  "weeklyFocus": {
    "decisionQuestion": "Will we address A/R Health this week?",
    "recommendedAction": "Stop overdue growth: implement collections cadence...",
    "whyNow": "A/R Health is YELLOW. Look at 31-60 and 90+ buckets...",
    "owner": "Operations",
    "cadence": "Weekly"
  }
}
```

---

## Test Coverage

| Test File | Tests | What It Covers |
|-----------|-------|----------------|
| `KpiStatusServiceTests` | 7 | Both directions, boundary values, unknown direction |
| `SevenPillarKpiStatusTests` | 7 | All 7 design-doc pillars with exact thresholds |
| `AlertServiceTests` | 5 | All-green, single RED, severity ranking, tie-breaking, field correctness |
| `WeeklyFocusServiceTests` | 3 | Null alert, valid generation, correct definition lookup |
| `DriverRankingServiceTests` | 5 | Pillar filtering, sort order, topN, rank reassignment, empty input |
| `DecisionOsDbContextTests` | 3 | Tenant persistence, unique index model validation, KPI snapshot round-trip |
| Workbook / import / dashboard tests | (varies) | Simplified analyzer, date rules, import service, dashboard context |
| **Total** | Run `dotnet test` | Domain logic, data access, upload pipeline |

---

## Project File Structure

```
src/DecisionOS.Distribution.Domain/
    KpiDirection.cs              # Enum: HigherIsBetter, LowerIsBetter
    Tenant.cs                    # Entity: distributor/client
    KpiDefinition.cs             # Entity: pillar definitions + thresholds + narratives
    KpiSnapshot.cs               # Entity: weekly KPI value + status
    DriverValue.cs               # Entity: driver detail rows
    Alert.cs                     # Entity: top alert per tenant-week
    WeeklyFocus.cs               # Entity: weekly focus per tenant-week
    IKpiStatusService.cs         # Interface: status computation
    KpiStatusService.cs          # Implementation: GREEN/YELLOW/RED logic
    IAlertService.cs             # Interface: top alert selection
    AlertService.cs              # Implementation: severity scoring + tie-breaking
    IWeeklyFocusService.cs       # Interface: weekly focus generation
    WeeklyFocusService.cs        # Implementation: rule-based string composition
    IDriverRankingService.cs     # Interface: driver ranking
    DriverRankingService.cs      # Implementation: filter + sort + topN

src/DecisionOS.Distribution.Infrastructure/
    DecisionOsDbContext.cs       # EF Core context with all DbSets and index configuration

src/DecisionOS.Distribution.Import/
    Program.cs                   # CLI entry point: CSV import + full processing pipeline

src/DecisionOS.Distribution.Web/
    Program.cs                   # App startup: Razor Pages + JSON API endpoints
    appsettings.json             # Connection string + logging configuration
    appsettings.Development.json # Development logging overrides
    Pages/
        _ViewImports.cshtml      # Namespace imports + tag helpers
        _ViewStart.cshtml        # Default layout reference
        Shared/_Layout.cshtml    # HTML5 layout: navbar, container, footer
        Index.cshtml             # Landing page: tenant + week selector
        Index.cshtml.cs          # Index page model: loads tenants and weeks
        Dashboard.cshtml         # Executive dashboard: KPIs, alert, drivers, focus
        Dashboard.cshtml.cs      # Dashboard page model: loads all dashboard data
    wwwroot/
        css/site.css             # Production CSS (1000+ lines, responsive, print-ready)
        js/site.js               # Vanilla JS: mobile nav, animations, interactivity

samples/
    kpi_snapshots_upload.csv     # Sample KPI data (7 rows)
    driver_values_upload.csv     # Sample driver data (10 rows across 3 pillars)

tests/DecisionOS.Distribution.Tests/
    KpiStatusServiceTests.cs     # Status logic tests
    SevenPillarKpiStatusTests.cs # All 7 pillar KPIs against design thresholds
    AlertServiceTests.cs         # Alert selection tests
    WeeklyFocusServiceTests.cs   # Focus generation tests
    DriverRankingServiceTests.cs # Driver ranking tests
    DecisionOsDbContextTests.cs  # DbContext persistence + model tests
```

---

## Technology Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| Runtime | .NET | 8.0 |
| Web Framework | ASP.NET Core (Razor Pages + Minimal API) | 8.0 |
| ORM | Entity Framework Core | 8.0.8 |
| Database | PostgreSQL (via Npgsql) | 8.0.4 |
| CSV Parsing | CsvHelper | 33.1.0 |
| Configuration | Microsoft.Extensions.Configuration | 8.0.0 |
| Testing | xUnit | 2.9.2 |
| Test DB | EF Core InMemory | 8.0.8 |
| Frontend | Pure CSS + Vanilla JavaScript | No frameworks |

---

## Quick Start (Copy-Paste)

```bash
# 1. Build and test
dotnet restore && dotnet build && dotnet test

# 2. Set up PostgreSQL (run in psql)
# CREATE USER decisionos WITH PASSWORD 'decisionos';
# CREATE DATABASE decisionos OWNER decisionos;

# 3. Import sample data
dotnet run --project src/DecisionOS.Distribution.Import -- "DIST-001" "2026-02-28" samples/kpi_snapshots_upload.csv samples/driver_values_upload.csv

# 4. Start the web app
dotnet run --project src/DecisionOS.Distribution.Web

# 5. Open http://localhost:5276 (or use .\scripts\run.ps1 -OpenBrowser)
```

---

## Helper Scripts (PowerShell)

From the repo root (`DecisionOS.Distribution.V1`), you can use these scripts:

### Free a Port

```powershell
cd C:\Users\emran\Downloads\hello\DecisionOS.Distribution.V1
powershell -ExecutionPolicy Bypass -File .\scripts\free-port.ps1 -Port 5276 -Force
```

- **Port**: TCP port to free (e.g. `5276`).
- **-Force**: (optional) force kill the owning process.

### One-Command Run (DB migrate → import sample data → start web UI)

```powershell
cd C:\Users\emran\Downloads\hello\DecisionOS.Distribution.V1

powershell -ExecutionPolicy Bypass -File .\scripts\run.ps1 `
  -WebPort 5276 `
  -DbHost localhost `
  -DbPort 5432 `
  -DbName decisionos `
  -DbUser postgres `
  -DbPassword postgres `
  -ClientId DIST-001 `
  -PeriodEnd 2026-02-28 `
  -KpiCsvPath .\samples\kpi_snapshots_upload.csv `
  -DriversCsvPath .\samples\driver_values_upload.csv `
  -OpenBrowser
```

- **-WebPort**: Port for the web UI (defaults to `5276`).
- **-SkipMigrate**: Skip `dotnet ef database update` if schema is already up to date.
- **-SkipImport**: Skip sample data import.
- **-NoFreePort**: Do not auto-kill any process on `WebPort`.
- **-OpenBrowser**: Automatically open the dashboard in your default browser.

The script sets `ConnectionStrings__DecisionOs` from the DB parameters and runs the web app with `ASPNETCORE_URLS=http://localhost:<WebPort>`.

### SQL helpers (`scripts/sql/`)

| Script | Purpose |
|--------|---------|
| `clear-tenant-import-data.sql` | Delete import/KPI/driver data for one tenant (keeps tenant + admin users) |
| `cleanup-bad-periods.sql` | Remove KPI/driver weeks outside plausible year range |
| `holdover-dashboard-diagnostic.sql` | Compare KPI weeks vs holdover rows per tenant |

---

## Profile Configuration Framework (Vertical → Profile → Tenant)

Decision OS is built to support **different KPI standards and business logic by industry/business type**.
It does this using a configuration hierarchy:

- **Core framework (universal)**: common status modes, RYG severity semantics, and dashboard patterns.
- **Vertical library**: broad business family (e.g., Distribution / Retail / Service).
- **Business profile (operating model)**: an industry/type standard set inside the vertical (e.g., Distribution Specialty Wholesale).
- **Tenant configuration**: a specific company assigned to a business profile.
- **Controlled overrides**: limited tenant-specific adjustments captured as data (not custom code).

### What the Business Profile controls

Within a profile you can configure:
- **KPI set + thresholds + RYG guidance**: `Admin → KPI definitions`
- **Driver catalog**: `Admin → Driver catalog`
- **Influencers** (upstream factors that affect drivers): `Admin → Influencers`
- **Profile metadata**: active KPI profile code, threshold profile code, location/channel structure

### Where to manage this in the UI

- **Vertical libraries**: `/Admin/VerticalLibraries`
- **Business profiles**: `/Admin/Profiles`
  - Use **Apply defaults** to clone the global KPI/Driver standards into a profile, then tailor
- **Assign a tenant to a profile**: `/Admin/Tenants` → Edit → Business profile

**Workbook `Company_Profile` tab:** Pilot Excel files include Field/Value rows (company name, business type, systems, targets). V1 does **not** import this sheet — admins copy values into **Business profile** and **Tenant** per the user guide section *Setting up from `Company_Profile`*.

### Controlled overrides (tenant-specific, limited)

If a tenant needs a controlled adjustment (without creating a new profile), the system supports override tables:
- **`TenantKpiOverrides`**: override KPI thresholds/targets/narratives by tenant + KPI code
- **`TenantDriverOverrides`**: override driver catalog labels/activation by tenant + pillar + driver_code

These are applied during import resolution and keep the system scalable without hard-coded tenant logic.

---

## Design Document Reference

This solution implements the architecture described in `Decision_OS_Distribution_V1_DotNet_Architecture_and_Design.md`:

| Doc Section | Implementation |
|-------------|---------------|
| **Section 2** -- 7 Pillar KPIs | All 7 definitions with exact thresholds, narratives, and actions |
| **Section 3** -- Functional Flow | CSV import -> status computation -> alert -> focus -> dashboard |
| **Section 4** -- Domain Model | All entities mapped to EF Core with proper indexes |
| **Section 5.1** -- Status Logic | `KpiStatusService` with both directions |
| **Section 5.2** -- Top Alert | `AlertService` with severity scoring + deviation tie-breaking |
| **Section 5.3** -- Drivers | `DriverRankingService` filtered by alert pillar |
| **Section 6** -- Weekly Focus | `WeeklyFocusService` with rule-based string composition |
| **Section 7** -- Architecture | Domain / Infrastructure / Import CLI / Web (Razor Pages + API) |
