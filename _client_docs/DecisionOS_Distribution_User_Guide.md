# Decision OS (Distribution) — Client User Guide

This guide explains how to use Decision OS (Distribution) **screen by screen**, what each screen is for, and what to expect when you create, view, update, or delete records.

---

## Who this guide is for

- **Non-technical users** who need to run weekly reporting (view dashboards, upload files, run imports).
- **Admins** who set up tenants (distributors), profiles, KPI/Driver definitions, and user access.

---

## Key concepts (read first)

| Term in this guide | In the system | How it gets into the app |
|--------------------|---------------|---------------------------|
| **Distributor** (company you log in for) | **Tenant** (`ClientId`, e.g. `DIST-001`) | **Admin → Tenants** — one tenant per onboarded distributor |
| **Buyer / account / end customer** | `Customer_ID` / `Customer_Name` on imported sales/AR rows | **Import only** — from workbook/CSV columns. There is **no** “Create Customer” admin screen |
| **Reporting week** | Week-ending date (`PeriodEnd`) | Created when you import data for that week |

**Two upload paths (Operations → Uploads):**

| Mode | Best for | Steps |
|------|----------|--------|
| **Simplified** | One multi-tab **Excel workbook** per distributor (recommended for pilot) | New batch → **Simplified** → upload `.xlsx` → **Detect** → **Verify** → **Import all periods** |
| **Classic** | Separate CSV/Excel files per report type | New batch → **Classic** → one week → upload each file → **Map** → **Validate** → **Run import** |

After import, open the **Dashboard** for the same tenant and pick a **reporting week** that exists in your data (see below).

**Company setup from the workbook:** The distributor Excel template includes a **`Company_Profile`** tab (Field / Value rows). In V1 this tab is **not imported automatically** — use it as the source of truth while you create the **Business profile** and **Tenant** in Admin (see [Setting up from `Company_Profile`](#setting-up-from-company_profile-workbook-tab)).

---

## Access & roles (what you can see)

The app uses roles to control which screens you can access:

- **Admin**: Full access (Dashboard + Operations + Admin setup screens).
- **Operator**: Operations access (Uploads + Import History) + Dashboard.
- **Viewer**: Dashboard only (read-only).
- **Developer**: Operations access + Dashboard (technical support role).

If you click a link and see “Access Denied”, you likely don’t have permission for that area.

---

## Navigation overview (top menu)

Depending on your role, you may see:

- **Dashboard**: Your weekly KPI dashboard.
- **Imports**: Import History (audit trail of runs).
- **Uploads**: Weekly upload batches — **Simplified** (one workbook, all weeks) or **Classic** (multi-file, map columns per file).
- **Admin**: Setup screens (Admin role only).
- **Logout**: Sign out.

---

## Screen-by-screen guide

### 1) Sign in (`/Account/Login`)

**What this screen is for**

- Signing into the system with your email and password.

**What to do**

- Enter **Email address** and **Password**, then click **Sign In**.

**What to expect**

- If successful, you’ll be taken to the main app.
- If incorrect, you’ll see: **“Invalid email or password.”**
- Passwords must meet complexity rules (minimum length and mixed characters). If your password doesn’t work, contact your admin to reset it.

**Note about “Remember this device”**

- The sign-in page shows a “Remember this device” checkbox, but sign-in is configured to remember you by default. If you need stricter sign-in behavior, your admin should adjust the policy.

---

### 2) Set Your Context (Distributor + Week) (`/`)

This is the first screen after login (for most users).

**What this screen is for**

- Choosing which **Distributor (Tenant)**, optional **Buyer**, and which **Reporting Week** you want on the dashboard.

**What to do**

- Select a **Distributor** from the dropdown.
- Optionally select a **Buyer** (or leave **All buyers (distributor total)** for company-wide KPIs).
- Select a **Reporting Week** (week-ending date).
- Click **View Dashboard →**

**What to expect**

- The **Reporting Week** list only appears after you select a distributor. Weeks come from imported KPI data (invalid or stray dates from old imports are hidden).
- **Buyers** in the dropdown come from imported sales data (customer id or name). If the list is empty, run a simplified or classic import that includes sales rows with customer columns.
- If there are no weeks available, you’ll see: **“No weeks found for this distributor.”**

**Read-only vs CRUD**

- This screen is **selection only** (no create/edit/delete here).

---

### 3) Dashboard (`/Dashboard?clientId=…&periodEnd=…`)

**What this screen is for**

- Viewing weekly KPI results for a selected distributor and week.
- Seeing:
  - KPI cards (status + values + targets)
  - Top alert (if any)
  - Weekly focus (if configured)
  - “Holdover Improvements” (drivers/actions table)

**How to use it**

- Use the header controls to change **Distributor**, **Buyer**, and **Week** without returning home.
- **KPI cards**: click a KPI card to open a detail modal showing value/target/WoW change/status and detail lines.
- **Holdover Improvements**: working list of open driver actions (from the workbook **Holdover** sheet when using simplified import).

**Buyer filter behavior**

- **All buyers**: KPI cards are distributor-wide for the selected week; the holdover table shows all rows for that week.
- **Specific buyer**: KPI cards stay distributor-wide; the holdover table filters to rows tied to that buyer when possible. Rows with no buyer key still appear (tenant-wide holdovers).
- If you pick a week with **no imported data**, KPIs may show **GRAY** (no snapshot) and the holdover table may be empty.

**What to expect**

- Pick a week that exists in your import (e.g. after simplified import, weeks are typically **Saturdays** such as `2026-04-25`, not arbitrary dates like `2026-04-28` unless that date is in your file).
- If there is no data for that week you’ll see a message like **“No KPI data for this week”** and a link to change selection.
- If holdovers were imported for a different week than the one you selected, you may see a note that items are **carried forward** from another week-ending date — re-run **Import all periods** on a simplified batch to attach holdovers to every KPI week.

**CRUD notes**

- The dashboard is primarily **read-only viewing**.
- Operational updates (like changing action status) are typically done via the system workflow (imports, mappings, etc.), not by editing KPI cards directly.

---

## Operations (Uploads & Imports)

Operations screens are typically for **Admin / Operator / Developer** roles.

### 4) Upload Batches list (`/Operations/Uploads`)

**What this screen is for**

- Viewing recent upload batches — **Classic** (one week, multiple files) or **Simplified** (workbook, many weeks). Open a batch to continue **Details**, **Detect**, or **Verify**.

**What you can do (CRUD)**

- **Create**: click **New Batch**.
- **Read/View**: click **Open** on a batch.
- **Update/Delete**: batch editing/deleting is not provided in this screen; management is done through the batch workflow (upload, map, validate, import).

**What to expect**

- Each row shows Created time, Tenant, Week ending, Status, Readiness, number of files.

---

### 5) Create new upload batch (`/Operations/Uploads/Create`)

**What this screen is for**

- Starting either a **Classic** (multi-file, one week) or **Simplified** (single workbook, many weeks) import.

**What to do**

1. Choose **Import mode**:
   - **Classic** — multi-file CSV/Excel with column mapping per file.
   - **Simplified** — single workbook, auto-detect sheets and periods.
2. Select **Tenant** (distributor).
3. **Classic only:** select **Week ending (PeriodEnd)** for this package.
4. **Simplified only:** set **Anchor date (first period to include)** and **Cadence** (usually **Weekly**). The anchor is the earliest week-ending date you want imported; weeks before the anchor are skipped.
5. Click **Continue**.

**What to expect**

- **Classic:** redirected to batch **Details** to upload and map files for that one week.
- **Simplified:** redirected to **Detect** to upload the `.xlsx` workbook.
- If Tenant is missing/invalid, you will see validation errors.

---

### 5a) Simplified upload — Detect workbook (`/Operations/Uploads/Simplified/Detect?id=…`)

**What this screen is for**

- Uploading the distributor workbook and running automatic sheet/period detection.

**What to do**

- Upload one **Excel** file (`.xlsx` / `.xls`) with multiple tabs (e.g. `Weekly_Financials`, `Sales_By_SKU_Week`, AR, AP, Inventory, `Customer_Master`, `Holdover_Actions`).
- Click **Analyze workbook**.

**What to expect**

- The system classifies each sheet (sales, AR, weekly rollup, holdover, etc.) and maps columns using known header synonyms.
- **Week-ending dates** used for import come from **sales** and **weekly rollup** columns — not from invoice/due dates on AR/AP (those caused bad weeks in older builds).
- If your anchor is later than all weeks in the file, the anchor may **auto-adjust** to the earliest week found.
- After analysis, continue to **Verify** (link on this page or uploads list).

---

### 5b) Simplified upload — Verify & import (`/Operations/Uploads/Simplified/Verify?id=…`)

**What this screen is for**

- Reviewing detection results, validating, and importing **all periods** in one run.

**What to do**

1. Review **Detected sheets** (name, kind, row count, confidence).
2. Review **Periods in workbook** — count of dates detected vs periods that will import (≥ anchor). Adjust **Anchor** and **Re-detect periods** if needed.
3. Review **Column mapping** (read-only auto-map per tab) and any **Detection warnings**.
4. Click **Validate package**.
5. When readiness allows, click **Import all periods**.

**What to expect**

- One import run writes normalized sales/inventory/AR/AP (and related scoring) for **each** week-ending date in the filtered list.
- **Holdover** rows from the workbook are applied to **every** imported KPI week (not only the latest week).
- **AR/AP/Inventory** detail rows are loaded for the **latest operational week** (last week with sales activity); older weeks still get KPI values from the **weekly rollup** sheet where present.
- **Customer_Master** is detected but not stored as a separate master table in V1 — buyer names/ids on sales and AR rows drive the dashboard buyer list.
- **`Company_Profile`** is detected on upload but **not written to the database** in V1 — complete distributor setup in Admin first (or after) using that tab; see [Setting up from `Company_Profile`](#setting-up-from-company_profile-workbook-tab).
- Other reference tabs (`README_Import_Map`, `KPI_Targets_Expected`, `Source_Notes`, masters) are for documentation or future mapping — not imported as company records in V1.
- On success, status becomes **Imported** and you can open the dashboard for the **latest** week in the batch.

**Typical workbook tabs (pilot template)**

| Sheet (examples) | Role |
|------------------|------|
| `Weekly_Financials` | Rollup KPI inputs per week |
| `Sales_By_SKU_Week` | Sales lines per week-ending date |
| AR / AP / Inventory | Operational detail (latest week on import) |
| `Holdover_Actions` | Holdover improvement drivers |
| `Customer_Master` | Reference only in V1 (buyers come from sales/AR) |

---

### 6) Classic upload — batch details (`/Operations/Uploads/Details?id=…`)

This is the “control center” for a **Classic** weekly upload package (one `PeriodEnd` per batch).

**What this screen is for**

- Uploading report files (CSV/Excel) into the batch.
- Mapping columns to system fields (so the system understands your file).
- Validating the package.
- Running the import pipeline (normalize → score).
- Reviewing any validation issues.

#### A) Status & Readiness section

**What you can do**

- Click **Validate package** to run checks.
- Click **Run import (normalize → score)** to import data and update the dashboard.

**What to expect**

- If the system detects problems, you may see:
  - A **Validation issues** list (warnings/errors)
  - A message like “Not validated yet.” or a validation summary
- If import cannot run, you may see an error banner describing what must be fixed.

#### B) Uploaded files table

**What you can do**

- **Read/View** what you already uploaded (file name, type, header row, upload time).
- Click **Map** to map that file’s columns.

**What to expect**

- You’ll typically map each uploaded file at least once.

#### C) Upload a report (CSV)

**What you can do (Create)**

- Select **Report type** (example: Sales).
- Enter **Header row** (which row in the file contains the column names).
- Choose a file (supported: **.csv, .xlsx, .xls**).
- Click **Upload**.

**What to expect**

- Excel files are converted to CSV internally.
- The system stores the file and shows it in the “Uploaded files” list.
- Common upload messages:
  - “Please select a file.”
  - “Only CSV and Excel (.xlsx/.xls) files are supported.”
  - “Header row must be between 1 and 50.”

**Important**

- If you upload the same report (same tenant + week + report type) with identical content, the system may show a **Duplicate** warning.

---

### 7) Map columns (`/Operations/Uploads/Map?batchId=…&fileId=…`)

**What this screen is for**

- Telling the system which columns in your file correspond to which “system fields”.
- Optionally saving your mapping as a reusable **template** for next week.

**What to do**

- For each **Source column**:
  - Choose a **System field** it maps to, or leave it **Unmapped**.
  - Check **Ignore** for columns you don’t want imported.
- Click **Save**.

**Mapping templates (recommended)**

- Turn on **Save as mapping template**
- Enter a **Template name**
- Click **Save**

**What to expect**

- The system tries to auto-map exact matches (or reuses a previous template for the same tenant + report type).
- If you choose “Save as template” but don’t enter a name, you’ll be asked to provide one.

**CRUD notes**

- **Create/Update**: saving the mapping writes/overwrites mappings for that file.
- **Template**: saving as template creates a reusable mapping for future uploads.

---

### 8) Import History (`/Operations/ImportRuns`)

**What this screen is for**

- An audit trail of import runs (status, row counts, fingerprints).

**What to look at**

- **Status**: Completed / Failed / Running.
- **Readiness**: readiness label (if present).
- **KPI / Driver rows**: how many rows were processed.
- **Fingerprint**: identifies the source content used for that import.

**CRUD notes**

- Read-only (view log). No editing here.

---

## Admin (setup screens)

Admin screens are typically for **Admin role only**.

## Setting up from `Company_Profile` (workbook tab)

Pilot workbooks include a **`Company_Profile`** sheet: two columns, **Field** and **Value**, describing the distributor (company name, systems, targets, test scope). The simplified upload **recognizes** this sheet but **does not import** it into Decision OS in V1 — admins copy the values into **Business profile** and **Tenant** screens.

**Related workbook tabs (reference only in V1)**

| Tab | Use when setting up |
|-----|---------------------|
| **`Company_Profile`** | Primary checklist for profile + tenant fields (below) |
| **`README_Import_Map`** | Narrative context (business type, systems, expected R/Y/G mix) — paste into profile **Description** if helpful |
| **`KPI_Targets_Expected`** | Expected pillar outcomes for **validation** after import — adjust KPI thresholds in Admin if you want to match test targets (e.g. Gross Margin 29%) |
| **`Source_Notes`** | Industry/benchmark notes — optional text for profile **Description** |

### Recommended order (new distributor)

1. Open the workbook **`Company_Profile`** tab (and optionally **`README_Import_Map`**).
2. **Admin → Vertical Libraries** — confirm a vertical exists (e.g. Distribution), or create one if your program uses it.
3. **Admin → Business Profiles → Add Profile** — enter profile from the mapping table below → **Save**.
4. On the profile list, open **Defaults** for that profile → **Apply Defaults** (copies global KPI + driver catalog into the profile without overwriting existing rows).
5. **Admin → KPI definitions** (filtered to the profile) — tune thresholds if needed (e.g. **Target Gross Margin** from the workbook → `GrossMargin%` target ≈ `0.29`).
6. **Admin → Tenants → Add Tenant** — set **Client ID**, **Name**, **Archetype**, and assign the **Business profile** you created.
7. **Operations → Uploads** — run **Simplified** import for that tenant (anchor = first week-ending date in the file; see **Week Ending Day** below).
8. **Dashboard** — confirm KPI colors and holdovers; compare to **`KPI_Targets_Expected`** if you are validating a test workbook.

### Map `Company_Profile` fields → Decision OS (pilot template)

Example values come from the Steve’s Bowling test workbook; your file may use the same field names.

| `Company_Profile` field (Excel) | Where to enter it in Decision OS | Notes |
|--------------------------------|----------------------------------|--------|
| **Company Name** | **Admin → Tenants → Name** | Display name for the distributor (e.g. `Steve's Bowling Supply Company`). |
| *(you define)* **Client ID** | **Admin → Tenants → Client ID** | System key used in URLs and imports (e.g. `DIST-001`). Not on the tab — choose a stable code. |
| **Business Type** | **Business profile → Name** or **Description**; optional **Tenant → Archetype** | e.g. `Bowling supply distributor`. |
| **Customer Scope** | **Business profile → Channel structure** or **Description** | e.g. `Outside customers only`. |
| **POS System** | **Business profile → Description** (or **Location structure**) | e.g. `Keystroke POS`. |
| **Accounting System** | **Business profile → Description** | e.g. `QuickBooks`. |
| **Week Ending Day** | **Simplified upload → Anchor date** | Pick a **week-ending date** that falls on that weekday (e.g. Saturday → `2025-11-22`). Import uses **sales + weekly rollup** week columns, not this text field directly. |
| **Test Period Weeks** | *(informational)* | Tells you how many weeks of data are in the file; does not configure the app. |
| **Annual Sales** | **Business profile → Description** *(reference)* | Not stored as a numeric company metric in V1. |
| **SKU Count** | **Description** *(reference)* | Reference for expectations; SKU detail comes from **`SKU_Master`** / sales import tabs. |
| **Inventory Value Target** | **Description** *(reference)*; KPI **DOH** / inventory thresholds in Admin if you tune them | Operational inventory is imported from the **Inventory** sheet on simplified import. |
| **Target Gross Margin** | **Admin → KPI definitions** (profile-scoped) → **`GrossMargin%`** → **Target** | Workbook may show `0.29` or `29%` — enter as decimal **0.29** in KPI definition. |
| **Expected KPI Mix** | *(validation only)* | e.g. `2 Red / 3 Yellow / 2 Green` — compare after import; not a stored profile field. |

**Business profile screen fields** (`/Admin/Profiles` → Add / Edit)

| Profile field in UI | Typical source from workbook |
|--------------------|------------------------------|
| **Code** | Short unique code you assign (e.g. `BOWLING_DIST`) — not on `Company_Profile`; required by the system. |
| **Name** | **Business Type** or a label derived from **Company Name** |
| **Description** | Combine **Business Type**, **Customer Scope**, **POS System**, **Accounting System**, **Annual Sales**, **SKU Count** from the tab and/or **`README_Import_Map`** |
| **Vertical** | Your program’s distribution vertical (Admin → Vertical Libraries) |
| **Location structure** / **Channel structure** | Optional: **Customer Scope**, warehouse/branch notes from README |
| **Active KPI profile code** / **Threshold profile code** | Optional metadata; leave blank to use copied defaults unless your program defines codes |

**Tenant screen** (`/Admin/Tenants` → Add / Edit)

| Tenant field | Typical source |
|--------------|----------------|
| **Client ID** | Your onboarding code (must match what operators select in Uploads/Dashboard) |
| **Name** | **Company Name** |
| **Archetype** | Short label from **Business Type** (optional) |
| **Business profile** | Profile created above — required for profile-specific KPI/driver standards |

### What simplified import does *not* do (V1)

- Does **not** create or update **Business profiles** from **`Company_Profile`**.
- Does **not** set tenant **Name** from **Company Name** (set tenant in Admin before or after import).
- Does **not** apply **`KPI_Targets_Expected`** rows automatically — KPI status comes from imported **`Weekly_Financials`** + operational data and resolved thresholds.

After tenant + profile are configured, **`Customer_Master`**, **Sales**, **AR**, etc. still load only through **Simplified** or **Classic** import as described above.

---

### 9) Admin home (`/Admin`)

**What this screen is for**

- A menu of setup areas (Vertical Libraries, Business Profiles, Tenants, KPI Definitions, Driver Catalog, Influencers, Users & Roles).

---

### 10) Vertical Libraries (`/Admin/VerticalLibraries`)

**What this screen is for**

- Managing the list of business vertical categories used by business profiles.

**What you can do (CRUD)**

- **Create**: Add Vertical
- **Read**: View list
- **Update**: Edit
- **Delete**: not exposed in the list screen (if needed, handled by admin/support policy)

**What to expect**

- Code, name, description, and active/inactive status.

---

### 11) Business Profiles (`/Admin/Profiles`)

**What this screen is for**

- Defining industry/business-type configurations (used by tenants).
- A profile controls which KPIs/drivers/influencers are used by default for that business type.
- For pilot onboarding, create the profile using the workbook **`Company_Profile`** tab — see [Setting up from `Company_Profile`](#setting-up-from-company_profile-workbook-tab).

**What you can do (CRUD)**

- **Create**: Add Profile
- **Read**: View list
- **Update**: Edit
- **Special actions**:
  - **Defaults** (`/Admin/Profiles/ApplyDefaults?id=…`): copy **global** KPI definitions and driver catalog into this profile (fills missing rows only; does not overwrite existing profile definitions)
  - **KPIs / Drivers / Influencers**: jump into those catalogs filtered for the profile

**What to expect**

- Profiles can be **Active** or **Inactive**.
- Profiles can be linked to a **Vertical**.
- Structures like **Location Structure** and **Channel Structure** can be stored for reporting conventions.
- **Apply Defaults** should be run once on a new profile before you assign tenants, unless you are intentionally using only global KPIs.

---

### 12) Tenants (Distributors) (`/Admin/Tenants`)

**What this screen is for**

- Managing distributor accounts (tenants) and the profile assigned to each tenant.
- **Company Name** and **Client ID** from onboarding usually come from the workbook **`Company_Profile`** tab plus your chosen `ClientId` — see [Setting up from `Company_Profile`](#setting-up-from-company_profile-workbook-tab).

**What you can do (CRUD)**

- **Create**: Add Tenant — set **Name** from **Company Name**, assign **Business profile**, choose **Client ID** used in uploads/dashboard
- **Read**: View list
- **Update**: Edit tenant (name/archetype/profile)
- **Delete**: Delete tenant (only if allowed; see below)
- **Overrides**:
  - KPI overrides
  - Driver overrides

**Delete behavior**

- If the tenant already has KPI data in the system, deletion is **blocked** (you’ll see a “Deletion Blocked” message).

---

### 13) Tenant KPI Overrides (`/Admin/Tenants/Overrides/Kpis?tenantId=…`)

**What this screen is for**

- Seeing the **effective KPI thresholds** for a tenant (global/profile defaults + tenant overrides).

**What you can do**

- Click **Edit Overrides** to adjust thresholds for a specific KPI.

**What to expect**

- A KPI can have:
  - No override (inherits)
  - An active override
  - A disabled override

---

### 14) Edit Tenant KPI Override (`/Admin/Tenants/Overrides/KpisEdit?tenantId=…&kpiCode=…`)

**What this screen is for**

- Editing tenant-specific KPI values like Target, Amber/Red thresholds, priority, min/max bounds, and action text.

**What you can do (CRUD)**

- **Create/Update**: Save Override
- **Delete**: Delete Override (revert back to inherited behavior)

**What to expect**

- “Active” controls whether the override is applied.
- “Delete Override” does not delete the KPI itself; it removes the tenant-specific override.

---

### 15) Tenant Driver Overrides (`/Admin/Tenants/Overrides/Drivers?tenantId=…`)

**What this screen is for**

- Seeing the **effective driver catalog** for a tenant (global/profile + overrides).

**What you can do**

- Click **Edit Overrides** to override display name/order/active status for a driver.

---

### 16) Edit Tenant Driver Override (`/Admin/Tenants/Overrides/DriversEdit?tenantId=…&driverKey=…`)

**What this screen is for**

- Overriding a pillar+driver combination for one tenant.

**What you can do (CRUD)**

- **Create/Update**: Save Override
- **Delete**: Delete Override (revert to inherited values)

**What to expect**

- “Override Status” supports:
  - Inherit (use default)
  - Force Active
  - Force Inactive

---

### 17) KPI Definitions (`/Admin/KpiDefinitions`)

**What this screen is for**

- Managing KPI definitions and action templates (globally or per business profile).

**What you can do (CRUD)**

- **Create**: Add KPI
- **Read**: View list
- **Update**: Edit KPI
- **Delete**: not exposed in the list screen (handled by policy)

**What to expect**

- KPIs have:
  - Code, Name, Unit
  - Direction (higher-is-better vs lower-is-better)
  - Target / Amber / Red thresholds
  - Optional min/max bounds
  - Recommended action + diagnostic checks text

---

### 18) Driver Catalog (`/Admin/DriverDefinitions`)

**What this screen is for**

- Managing drivers that influence KPI performance (global or profile-scoped).

**What you can do (CRUD)**

- **Create**: Add Driver
- **Read**: View list
- **Update**: Edit Driver
- **Delete**: not exposed in the list screen (handled by policy)

**What to expect**

- Each driver has:
  - Pillar code + driver code (identifier)
  - Display name and description
  - Sort order
  - Active/inactive status

---

### 19) Influencers (`/Admin/Influencers`)

**What this screen is for**

- Managing “micro-drivers” under drivers (profile-scoped).

**What you can do (CRUD)**

- **Create**: Add Influencer (only after selecting a profile)
- **Read**: View list
- **Update**: Edit influencer
- **Delete**: not shown in list screen

**What to expect**

- You must select a **Business Profile** first.
- Influencers include pillar/driver/code/name/direction/weight and active status.

---

### 20) Users & Roles (`/Admin/Users`)

**What this screen is for**

- Managing who can sign in and what access they have.

**What you can do (CRUD)**

- **Create**: Add User
- **Read**: View users and their roles
- **Update**: Edit Roles
- **Delete**: not shown in the user list UI

**What to expect**

- A user can have multiple roles (Admin, Operator, Viewer, Developer).

---

## Common workflows (quick “how-to”)

### C) New distributor: `Company_Profile` tab → Admin → import

1. Read workbook tabs **`Company_Profile`** and **`README_Import_Map`**
2. **Admin → Business Profiles → Add Profile** — map fields (table above) → **Save**
3. **Defaults → Apply Defaults** on that profile
4. Optional: **Admin → KPI definitions** — align **`GrossMargin%`** (and other pillars) with **Target Gross Margin** / **`KPI_Targets_Expected`**
5. **Admin → Tenants → Add Tenant** — **Client ID**, **Company Name**, assign **Business profile**
6. **Simplified upload** for that **Client ID** (anchor = first Saturday week-ending in file if **Week Ending Day** = Saturday)
7. **Dashboard** — validate against **`KPI_Targets_Expected`** / **Expected KPI Mix** if using a test workbook

### A) Simplified workbook (recommended for multi-week Excel)

1. **Uploads** → **New Batch** → mode **Simplified** → tenant + **anchor** (first week to include) → **Continue**
2. **Detect** → upload `.xlsx` → **Analyze workbook**
3. **Verify** → review sheets/periods → **Validate package** → **Import all periods**
4. **Dashboard** → same tenant → **All buyers** (or one buyer) → pick a week from the list (e.g. latest Saturday week-ending date)
5. **Import History** — optional audit of the run

### B) Classic multi-file (one week per batch)

1. **Uploads** → **New Batch** → mode **Classic** → tenant + **week ending** → **Continue**
2. **Details** → upload each report (Sales, Inventory, AR, AP, …)
3. **Map** each file → **Save** (optionally save template)
4. **Validate package** → **Run import (normalize → score)**
5. **Dashboard** for that tenant + week
6. **Import History** — audit

### After import — reviewing the dashboard

- Use a **reporting week** that appears in the week dropdown (from `KpiSnapshots`).
- For distributor-wide KPIs, use **All buyers**.
- Use a **specific buyer** to focus the holdover table; KPI tiles remain company-wide for that week.

---

## Troubleshooting (what to check first)

- **I can’t see Admin / Uploads / Imports**: you likely need a different role.
- **No weeks found**: no import yet for that tenant, or only invalid legacy dates in the database — re-import via simplified flow or run cleanup SQL (see repo `scripts/sql/`).
- **Strange years in the week list (e.g. 8818)**: old bad imports; clear tenant import data and re-import the workbook (`scripts/sql/clear-tenant-import-data.sql`).
- **KPIs all GRAY for a week I expect**: that exact week-ending date may not be in sales/rollup (pick a nearby week from the dropdown, often the Saturday before/after).
- **Holdover table empty**: no drivers for that week — confirm simplified import completed and try **All buyers**; buyer-specific filter hides rows without a buyer key.
- **No buyers in dropdown**: sales import missing `Customer_ID` / `Customer_Name` (or equivalent mapped columns).
- **Validation issues (Classic)**: open batch **Details**; fix mappings/headers, re-validate, re-import.
- **Validation issues (Simplified)**: open **Verify** warnings; fix workbook tabs/headers and re-upload on **Detect**.
- **Mapping looks wrong (Classic)**: re-open **Map Columns**, adjust mappings, save, validate, import again.
- **Company tab did nothing after import**: expected in V1 — **`Company_Profile`** is manual Admin setup only; see [Setting up from `Company_Profile`](#setting-up-from-company_profile-workbook-tab).
- **KPI colors don’t match workbook “Expected KPI Mix”**: thresholds come from **Business profile** / global KPI definitions + imported **`Weekly_Financials`**, not from **`KPI_Targets_Expected`** automatically.

