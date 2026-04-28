# Decision OS (Distribution V1): How Everything Links Up + How Imports Are Analyzed

## Purpose of this document

This document explains:

- How the **projects/layers** in `DecisionOS.Distribution.V1.sln` connect (Domain / Infrastructure / Import / Web / Tests)
- How an uploaded Excel becomes **validated weekly business signals** (KPI status, Top Alert, ranked drivers, Weekly Focus)
- Where the **profile-based configuration framework** (Vertical → Business Profile → Tenant Overrides) is applied

> Note: the Import tool currently reads **CSV** files. If your source is Excel, the expected operational flow is **Excel → export to CSV → import**.

---

## Solution topology (what depends on what)

### Projects

- **`src/DecisionOS.Distribution.Domain`**
  - Entities, enums, and business rules (no EF, no web, no file IO)
- **`src/DecisionOS.Distribution.Infrastructure`**
  - EF Core + PostgreSQL `DecisionOsDbContext`
  - ASP.NET Identity integration and seed
  - Definition resolution logic used by Import and Admin UI
- **`src/DecisionOS.Distribution.Import`**
  - CLI pipeline to validate and import weekly data
  - Computes deterministic weekly outputs (snapshots, alert, focus)
- **`src/DecisionOS.Distribution.Web`**
  - Razor Pages UI + minimal JSON API (`/api/*`)
  - Admin UI pages for configuration management
- **`tests/DecisionOS.Distribution.Tests`**
  - Unit tests for domain services and infrastructure behavior

### Dependency graph (high level)

```
Domain
  ↑
Infrastructure  (EF Core / Identity / resolver)
  ↑            ↑
Web            Import
  ↑
Tests (references Domain + Infrastructure for unit testing)
```

---

## Data model: what gets stored and why

### Core weekly outcome tables

- **`KpiDefinition`**: the “standard” for a KPI (thresholds, direction, narratives, priority)
- **`KpiSnapshot`**: one week’s KPI value + computed status + delta fields used on the dashboard
- **`DriverValue`**: one week’s driver values explaining KPI performance (ranked + narrative fields)
- **`Alert`**: the selected “Top Alert” KPI for the week (one per tenant/week)
- **`WeeklyFocus`**: the generated weekly action guidance (one per tenant/week)
- **`ImportRun`**: audit trail + idempotency fingerprint + validation summary

### Profile Configuration Framework tables (config layers)

These enable “KPI structure varies by industry/company” without custom code per tenant:

- **`VerticalLibrary`**: broad category (e.g., Distribution)
- **`BusinessProfile`**: specific profile template (industry/business type)
- **`Tenant`**: client/distributor, optionally assigned to a `BusinessProfile`
- **`TenantKpiOverride`**: controlled per-tenant adjustments to KPI standards
- **`TenantDriverOverride`**: controlled per-tenant adjustments to driver catalog entries
- **`InfluencerDefinition`**: catalog of upstream influencers that can affect drivers (framework support)

---

## Business health analysis: what we use (signals, standards, and rules)

This is the **exact set of inputs + logic** Decision OS uses to analyze weekly business health after import.

### 1) KPI health (RYG status per pillar)

- **Inputs (per tenant/week)**:
  - Imported KPI values (`kpi_code`, `value`)
  - Effective KPI standards (`KpiDefinition`) resolved via Profile → Global → Tenant override
- **Standards used**:
  - `KpiDefinition.Direction` (HigherIsBetter / LowerIsBetter)
  - `KpiDefinition.Target`, `AmberThreshold`, `RedThreshold`
  - Optional import validation bounds: `KpiDefinition.MinValue`, `MaxValue`
- **Computed outputs**:
  - `KpiSnapshot.Status` = **GREEN / YELLOW / RED** (rule-based, deterministic)
  - `KpiSnapshot.WeekOverWeekDelta` (if prior week exists)
  - Optional UI detail lines: `CardDetailLine1`, `CardDetailLine2`

Where implemented:
- Status scoring: `src/DecisionOS.Distribution.Domain/KpiStatusService.cs`
- Weekly snapshot storage: `src/DecisionOS.Distribution.Import/Program.cs`

### 2) “Top Alert” selection (what to focus on first)

From all non-green KPIs in the week, the system picks **one** KPI as the top priority.

- **Inputs**:
  - The week’s KPI snapshots + their statuses
  - Effective KPI definitions (targets, units, priority)
- **Selection rules**:
  - Severity first: RED beats YELLOW
  - Tie-break: `KpiDefinition.AlertPriority` (business priority ordering)
  - Next tie-break: relative deviation from target
- **Output**:
  - `Alert` row (one per tenant/week), with a human-readable `ReasonSummary`

Where implemented:
- `src/DecisionOS.Distribution.Domain/AlertService.cs`

### 3) Driver analysis (what’s causing the KPI movement)

Drivers explain *why* a pillar KPI is performing the way it is.

- **Inputs**:
  - Imported driver rows (pillar_code, driver_name, current, rank, status, why_it_matters, etc.)
  - Optional driver catalog (`DriverDefinition`) + controlled tenant overrides
- **Rules used**:
  - Driver row validation (required fields, rank, status values, fix_progress bounds)
  - Optional `driver_code` requirement depending on tenant/profile catalog presence
  - Driver ranking/filter logic used for “top drivers” display
- **Outputs**:
  - Stored `DriverValue` rows for the tenant/week
  - Dashboard views:
    - “Top drivers” for the top alert pillar
    - “Holdover improvements” view across pillars (more rows)

Where implemented:
- Validation: `src/DecisionOS.Distribution.Domain/Import/ImportRowValidator.cs`
- Driver ranking: `src/DecisionOS.Distribution.Domain/DriverRankingService.cs`
- Web API holdover view: `src/DecisionOS.Distribution.Web/Program.cs` (`?view=holdover`)

### 4) Weekly Focus (action guidance)

Weekly Focus translates the data into **one recommended action path**.

- **Inputs**:
  - The selected Top Alert KPI
  - Drivers for the week (and their statuses/ranks)
  - The definition narratives:
    - `KpiDefinition.RecommendedAction`
    - `KpiDefinition.DiagnosticChecks`
- **Output**:
  - `WeeklyFocus` row (one per tenant/week) used by dashboard + API

Where implemented:
- `src/DecisionOS.Distribution.Domain/WeeklyFocusService.cs`

### 5) Profile-aware business rules (the “standards layer”)

The system is **not hard-coded** to a single KPI structure across all clients.

- **What varies by industry/profile**:
  - Which KPI definitions exist for a profile
  - Their thresholds, target, direction, narratives, and alert priority
  - Which drivers exist per pillar (via `DriverDefinition`)
- **What varies by tenant (controlled)**:
  - Limited adjustments via `TenantKpiOverride` and `TenantDriverOverride`

Where implemented:
- `src/DecisionOS.Distribution.Infrastructure/DefinitionResolver.cs`

---

## Configuration layers: how “effective standards” are determined

When the system needs KPI/Driver standards for a tenant, it resolves them in this order:

1. **Business Profile-specific definitions** (if the tenant is assigned a profile AND such definitions exist)
2. Otherwise fall back to **Global/default definitions**
3. Overlay **Tenant overrides** (controlled edits only; stored as data)

This logic is implemented in:

- `src/DecisionOS.Distribution.Infrastructure/DefinitionResolver.cs`

This resolver is used by:

- **Import**: to validate and interpret incoming values using the effective standards
- **Admin UI**: to display effective values and edit overrides

---

## Import input: what the “Excel” must contain (via CSV export)

### KPI CSV (required)

Minimum columns:

- `kpi_code`
- `value`

Optional columns (supported by samples):

- `card_detail_line1`
- `card_detail_line2`

### Driver CSV (optional but recommended)

Minimum columns:

- `pillar_code`
- `driver_name`
- `current`
- `rank`
- `status`
- `why_it_matters`

Optional columns (supported by samples and dashboard holdover table):

- `driver_code` (required for some tenants/profiles when a driver catalog exists)
- `owner`
- `assigned_summary`
- `target_summary`
- `current_summary`
- `fix_progress`

---

## Import pipeline: step-by-step (what happens when you run the CLI)

Entry point:

- `src/DecisionOS.Distribution.Import/Program.cs`

### Step 0 — connect and ensure schema exists

- Builds configuration (from `appsettings.json` + environment variables)
- Creates `DecisionOsDbContext`
- Runs `db.Database.MigrateAsync()` so the DB schema is current

### Step 1 — ensure baseline “catalog” data exists

The import tool seeds any missing baseline standards so the system is usable without manual SQL:

- `VerticalLibrary` seed (needed before profiles)
- `BusinessProfile` seed (depends on vertical)
- `KpiDefinition` seed (global defaults)
- `DriverDefinition` seed (global defaults)

### Step 2 — resolve/create tenant context

- Finds tenant by `clientId`
- If missing, creates the tenant and assigns a default profile (e.g., `DISTRIBUTION_DEFAULT`) if present

### Step 3 — idempotency guard (fingerprint)

The import computes a SHA-256 fingerprint using:

- `clientId`, `periodEnd`, KPI CSV bytes, Driver CSV bytes

If an import run exists with the same fingerprint and `Status == Completed`, the import **skips** unless `--force` is passed.

### Step 4 — header validation

Before reading data, it validates the CSV header contains required columns.

### Step 5 — effective standards resolution (profile-aware)

The import resolves:

- Effective KPI definitions for the tenant (profile/global + tenant overrides)
- Effective driver catalog for the tenant (profile/global + tenant overrides)

### Step 6 — row-level validation (robustness)

Validation happens before committing the week:

- KPI values checked against expected formats and min/max bounds (where configured)
- Driver rows validated (required fields, rank/status, fix progress bounds, and optional driver_code rules)

If validation fails, the import:

- Marks the `ImportRun` as failed
- Stores a validation summary
- Does **not** write partial weekly results

### Step 7 — write weekly facts (snapshots and drivers)

When validation succeeds:

- Writes/updates weekly `KpiSnapshot` rows for the tenant/week
- Writes/updates weekly `DriverValue` rows for the tenant/week
- Computes `WeekOverWeekDelta` fields (where prior week exists)

### Step 8 — compute weekly interpretation outputs

Using the Domain services:

- **`KpiStatusService`**: converts KPI values into GREEN/YELLOW/RED
- **`AlertService`**: selects the weekly **Top Alert** KPI (severity + priority tie-break)
- **`DriverRankingService`**: filters/ranks drivers for explanation
- **`WeeklyFocusService`**: produces “decision + action guidance” for the week

The results are persisted into:

- `Alert` (1 per tenant/week)
- `WeeklyFocus` (1 per tenant/week)

### Step 9 — finalize audit trail

- Import completes by writing `ImportRun.Status`, `CompletedAt`, processed counts, and a summary.

---

## How the Web app uses imported data

### Dashboard (Razor Pages)

Main user flow:

- User logs in (`/Account/Login`)
- Dashboard is accessed with a tenant + week:
  - `/Dashboard?clientId=...&periodEnd=YYYY-MM-DD`

The dashboard reads:

- KPI snapshots for the week (`KpiSnapshots` + `KpiDefinition`)
- Top alert for the week (`Alerts` + `KpiDefinition`)
- Driver values for the week (`DriverValues`)
- Weekly focus (`WeeklyFocuses` + `KpiDefinition`)

### JSON API (`/api/*`)

The API is used by the frontend dashboard and any integration/testing:

- `/api/tenants`
- `/api/tenants/{clientId}/weeks`
- `/api/tenants/{clientId}/weeks/{periodEnd}`
  - Supports `?view=holdover` to return a broader driver set for operations view

All `/api/*` endpoints require `AnyDistributionRole`.

---

## Admin UI: where configuration is managed

These pages exist to keep configuration data-driven:

- **Vertical libraries**: `/Admin/VerticalLibraries`
- **Business profiles**: `/Admin/Profiles`
- **Tenants** (assign profile): `/Admin/Tenants`
- **Profile KPI definitions**: `/Admin/KpiDefinitions` (with profile selector)
- **Profile driver definitions**: `/Admin/DriverDefinitions` (with profile selector)
- **Influencer catalog**: `/Admin/Influencers`
- **Tenant overrides**:
  - KPI overrides: `/Admin/Tenants/Overrides/Kpis?tenantId=...`
  - Driver overrides: `/Admin/Tenants/Overrides/Drivers?tenantId=...`

---

## Operational scripts (running, ports, database setup)

Key scripts in `/scripts`:

- `free-port.ps1`: stops the web host and frees the default ports
- `setup-database.ps1`: applies migrations + runs imports (data load)
- `run.ps1`: orchestrates setup and runs the web app

See `README.md` for the exact commands and examples.

---

## “Where to look” cheat-sheet (common questions)

- **Import is failing validation**:
  - `DecisionOS.Distribution.Domain/Import/*`
  - `ImportRun.ValidationSummary` in the DB
- **Top Alert selection**:
  - `DecisionOS.Distribution.Domain/AlertService.cs`
- **Why a KPI is GREEN/YELLOW/RED**:
  - `DecisionOS.Distribution.Domain/KpiStatusService.cs`
  - `KpiDefinition.Direction + thresholds`
- **Which definitions apply to a tenant**:
  - `DecisionOS.Distribution.Infrastructure/DefinitionResolver.cs`
  - tenant’s `BusinessProfileId` + `TenantKpiOverride` / `TenantDriverOverride`

