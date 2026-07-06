# Catalog Engine — Agent Implementation Plan

**Product:** DecisionOS Distribution V1  
**Audience:** AI agents and developers implementing post–client-test enhancements  
**Status:** Authoritative implementation spec for catalog-driven scoring (Jul 2026)  
**Client inputs:** `DPOS_Distribution_KPI_Driver_Influencer_Developer_Catalog_v1.0.xlsx`, Terry/Syd test feedback, Jul 2026 meeting notes

---

## 0. How to use this document

1. Read **Section 1** (rules) and **Section 2** (baseline) before any code change.
2. Implement **one phase at a time** (Section 8). Do not skip phases or merge epics.
3. Keep **feature flags OFF** by default until that phase’s acceptance tests pass.
4. After each phase: `dotnet build` + `dotnet test` + simplified import smoke (Section 10).
5. If behavior is not described here or in cited repo files → grep/read; do not invent.

**Related docs**

| Doc | Purpose |
|-----|---------|
| [AGENTS.md](../AGENTS.md) | Agent entry point |
| [.cursor/rules/decisionos-domain-truth.mdc](../.cursor/rules/decisionos-domain-truth.mdc) | Tenant ≠ end customer; V1 scope |
| [.cursor/rules/decisionos-distribution.mdc](../.cursor/rules/decisionos-distribution.mdc) | Stack, layers, auth |
| [docs/Solution-Linkage-and-Import-Analysis.md](Solution-Linkage-and-Import-Analysis.md) | Upload → KPI pipeline |
| [.cursor/skills/decisionos-distribution/reference.md](../.cursor/skills/decisionos-distribution/reference.md) | Routes, services, entities |

---

## 1. Non-negotiable project rules

### 1.1 Layer boundaries

| Layer | Project | May contain |
|-------|---------|-------------|
| Domain | `src/DecisionOS.Distribution.Domain` | Entities, interfaces, domain services (`IKpiStatusService`, `IAlertService`, …). **No EF, no Npgsql, no ASP.NET.** |
| Infrastructure | `src/DecisionOS.Distribution.Infrastructure` | `DecisionOsDbContext`, migrations, import/scoring implementations, `DefinitionResolver` |
| Web | `src/DecisionOS.Distribution.Web` | Razor Pages, `Program.cs`, minimal APIs under `/api` |
| Tests | `tests/DecisionOS.Distribution.Tests` | xUnit, EF InMemory where needed |

### 1.2 Domain truth (do not violate)

- **Tenant** = one distributor company. **Not** an end customer (buyer).
- End customers exist only as import row fields (`Customer_ID`, `Customer_Name`). **No Customer DbSet.**
- V1 = **manual upload only**. No live ERP/POS integration.
- Classic and Simplified upload **both** must keep working unless a phase explicitly retires behavior behind a flag.

### 1.3 Auth conventions

Policies (defined in `Program.cs`): `AdminOnly`, `OpsPolicy`, `AnyDistributionRole`.

| New UI area | Policy | Also update |
|-------------|--------|-------------|
| `/Admin/*` | `AdminOnly` | `Program.cs` Razor conventions |
| `/Operations/*` | `OpsPolicy` | same |
| Dashboard, `/api/*` | `AnyDistributionRole` | same |

### 1.4 Memory / performance

- **No** static mutable caches for catalog or workbook data.
- Import: stream/dispose files; batch `SaveChanges` (~100 rows).
- Reads: `AsNoTracking()`; list endpoints use `Take(N)` (see existing `/api` in `Program.cs`).
- DI: prefer **scoped** services (match existing `Program.cs` registration).

### 1.5 Database

- Provider: PostgreSQL via Npgsql (EF Core 8).
- Connection string name: **`DecisionOs`**.
- Migrations: `--project src/DecisionOS.Distribution.Infrastructure --startup-project src/DecisionOS.Distribution.Web`

---

## 2. Current baseline (verified in repo)

### 2.1 What works today (do not break)

| Capability | Primary files |
|------------|---------------|
| Simplified upload | `SimplifiedWorkbookImportService.cs`, `WorkbookAnalyzer.cs`, `Pages/Operations/Uploads/Simplified/*` |
| Classic upload | `UploadBatchImportService.cs`, `Pages/Operations/Uploads/Map` |
| Weekly scoring (7 KPIs) | `WeeklyScoringService.cs` — hardcoded keys: `GrossMargin%`, `AR_PastDue31p%`, `AP_PastDue31p%`, `DOH`, `CCC`, `NetProfit%`, `PerfectOrderRate` |
| R/Y/G status | `KpiStatusService.cs` |
| Top alert | `AlertService.cs` — severity + `AlertPriority` + deviation |
| Dashboard (7 tiles) | `Pages/Dashboard.cshtml.cs` — `SevenPillarCodes`, `EnsureSevenPillarDisplay` |
| Tenant/profile overrides | `DefinitionResolver.cs`, `TenantKpiOverride`, `TenantDriverOverride` |
| Holdover import | `SimplifiedWorkbookImportService.ImportHoldoversForAllPeriodsAsync` → `DriverValue` |
| Seed data | `ReferenceDataSeeder.cs` — 7 KPIs, 13 drivers, 0 influencers seeded |

### 2.2 Already delivered (pre-catalog sprint)

| Item | Location |
|------|----------|
| Full recommendation text on KPI cards (no 90-char cap) | `KpiScoringNarrative.cs` |
| Plain-language DSO/DIO/DPO helper | `Domain/OwnerLanguage.cs` |
| Owner-friendly KPI modal + holdover copy | `OwnerLanguage.cs`, `DashboardKpiInsightBuilder`, `Dashboard.cshtml` |
| Profile Apply Defaults clones influencers | `Pages/Admin/Profiles/ApplyDefaults.cshtml.cs` |
| Holdover detail modal + assign + comment thread | `Dashboard.cshtml`, `IHoldoverWorkflowService`, workflow APIs in `Program.cs` |
| In-app notifications (bell + page) | `UserNotification`, `/Notifications`, `_Layout.cshtml` |
| Action item status/owner/notes API | `Program.cs` (separate from holdover comments) |
| Scoring uses `DefinitionResolver` for tenant thresholds | `WeeklyScoringService.cs` |

### 2.3 What is NOT implemented

| Gap | Evidence |
|-----|----------|
| Full 24-KPI catalog in DB | Only 7 `KpiDefinition` rows in `ReferenceDataSeeder.cs` |
| Dynamic top-7 selection | `SevenPillarCodes` fixed array in `Dashboard.cshtml.cs` |
| Influencer runtime scoring | `InfluencerDefinition` admin CRUD only; no use in `WeeklyScoringService` |
| Module routing queues | Behind `Routing.Enabled` — entity + `/Operations/ModuleQueue` exist; default off |
| Intelligent P&L term mapping | `ColumnSynonymMatcher` — template columns only |
| LLM ingestion | Not in repo |

### 2.4 Client catalog workbook (source of truth for new tables)

File: `DPOS_Distribution_KPI_Driver_Influencer_Developer_Catalog_v1.0.xlsx`

| Sheet | Data rows | Key columns |
|-------|-----------|-------------|
| `KPI_Catalog` | 24 | `KPI_ID`, `KPI_Name`, `Primary_Data_Needs`, `Mgmt_Layer_Candidate`, … |
| `Driver_Catalog` | 36 | `Driver_ID`, `Driver_Name`, `Related_KPIs`, `Evidence_Fields`, … |
| `Influencer_Catalog` | 60 | `Influencer_ID`, `Evidence_Fields`, `Default_Severity`, … |
| `KPI_Driver_Map` | 84 | `KPI_ID`, `Driver_ID`, `Map_Type`, … |
| `Driver_Influencer_Map` | 60 | `Driver_ID`, `Influencer_ID`, `Default_Weight`, … |
| `Scoring_Logic` | 10 | Component weights; formula: Severity 30% + Cash 20% + Financial 20% + Urgency 15% + Actionability 10% + Confidence 5% |
| `Module_Routing` | 9 | `Module`, `Primary_KPIs`, `Default_Output`, … |
| `Output_Assignment` | 7 | Management Layer, Drill-Down, Watchlist, Data Gap Queue, Module Action Queue, Holdover, Internal Reviewer |

**Rule:** Use `KPI_ID` / `Driver_ID` / `Influencer_ID` as stable keys. Never key logic on display names.

---

## 3. Target outcomes

When fully delivered (all flags ON):

1. Catalog xlsx imports into dedicated tables (idempotent).
2. System **evaluates all catalog KPIs** where data + calculator exist.
3. Dashboard **Management Layer** shows **top 7** by priority score (not fixed list).
4. Tenant can **pin/exclude** KPIs via override UI.
5. **Drivers** run only when parent KPI is RED/YELLOW (per catalog maps).
6. **Influencers** attach as evidence on triggered drivers (drill-down / holdover modal).
7. Issues route to **queues** (watchlist, data-gap, module action, drill-down).
8. **In-app** assignment, comments, notifications — no email.
9. With flags **OFF**, Terry/Syd simplified upload behavior is unchanged.

---

## 4. Architecture

### 4.1 Dual-path scoring (strangler)

```
                    ┌─────────────────────────────────────┐
  Upload ──────────►│ Normalized staging rows             │
                    └──────────────┬──────────────────────┘
                                   │
              DecisionOs:Scoring:UseCatalogEngine = false
                                   │
                                   ▼
                    ┌─────────────────────────────────────┐
                    │ WeeklyScoringService (UNCHANGED)    │
                    │ 7 formulas → KpiSnapshot            │
                    └─────────────────────────────────────┘

              DecisionOs:Scoring:UseCatalogEngine = true
                                   │
                                   ▼
                    ┌─────────────────────────────────────┐
                    │ KpiCalculationOrchestrator          │
                    │ → IKpiCalculator per legacy/catalog │
                    └──────────────┬──────────────────────┘
                                   ▼
                    ┌─────────────────────────────────────┐
                    │ PriorityRankingService            │
                    │ → IssuePriorityScore (all issues) │
                    └──────────────┬──────────────────────┘
                                   ▼
              ┌────────────────────┴────────────────────┐
              │ Top 7 → Dashboard Management Layer      │
              │ Rest  → Watchlist / Drill-down queues   │
              └─────────────────────────────────────────┘
                                   │
                                   ▼ (RED/YELLOW KPIs)
                    ┌─────────────────────────────────────┐
                    │ DriverEvaluationService             │
                    └──────────────┬──────────────────────┘
                                   ▼
                    ┌─────────────────────────────────────┐
                    │ InfluencerEvidenceService           │
                    └──────────────┬──────────────────────┘
                                   ▼
                    ┌─────────────────────────────────────┐
                    │ ModuleRoutingService → queues       │
                    └─────────────────────────────────────┘
```

### 4.2 Feature flags (`appsettings.json`)

Add section (defaults **false**):

```json
{
  "DecisionOs": {
    "Catalog": {
      "Enabled": false
    },
    "Scoring": {
      "UseCatalogEngine": false,
      "UseDynamicTop7": false
    },
    "Routing": {
      "Enabled": false
    },
    "Workflow": {
      "AssignmentsEnabled": true,
      "NotificationsEnabled": true
    },
    "Ingestion": {
      "RuleBasedExpansionEnabled": false
    }
  }
}
```

Bind with `IOptions<DecisionOsFeatureOptions>` in **Domain** (POCO) or Web; inject into orchestrator.

**Agent rule:** Phase N may set its flag true **only in tests**, not in committed `appsettings.json` — **exception:** `Workflow` flags are **on** by default for holdover assign/notify (Phase 7 delivered).

---

## 5. Data model specification

### 5.1 Phase 1 — Catalog tables (new)

Create entities in **Domain**, configure in `DecisionOsDbContext`, migrate in **Infrastructure**.

#### `CatalogKpi`

| Column | Type | Notes |
|--------|------|-------|
| `KpiId` | `string` PK | e.g. `KPI-001` |
| `Name` | `string` | |
| `Definition` | `string` | |
| `Category` | `string?` | |
| `EntityScope` | `string?` | |
| `Cadence` | `string?` | |
| `PrimaryDataNeeds` | `string?` | |
| `DefaultStatusModel` | `string?` | |
| `MgmtLayerCandidate` | `bool` | from sheet |
| `DeveloperNotes` | `string?` | |
| `PrimaryModules` | `string?` | |
| `LegacyCode` | `string?` | maps to existing `KpiDefinition.Code` when applicable |

#### `CatalogDriver`

| Column | Type | Notes |
|--------|------|-------|
| `DriverId` | `string` PK | e.g. `DRV-001` |
| `Name`, `Definition`, `Category` | strings | |
| `EvidenceFields` | `string?` | |
| `RelatedKpis` | `string?` | raw or JSON |
| `PrimaryModules` | `string?` | |

#### `CatalogInfluencer`

| Column | Type | Notes |
|--------|------|-------|
| `InfluencerId` | `string` PK | |
| `Name`, `Definition`, `Category` | strings | |
| `EvidenceFields` | `string?` | |
| `DefaultSeverity` | `string?` | |
| `RelatedKpis` | `string?` | |
| `PrimaryModules` | `string?` | |

#### `CatalogKpiDriverMap`

| Column | Type |
|--------|------|
| `KpiId` | FK → `CatalogKpi` |
| `DriverId` | FK → `CatalogDriver` |
| `MapType` | `string?` |
| `PrimaryModules` | `string?` |
| `RuleNotes` | `string?` |

Composite PK: `(KpiId, DriverId)`.

#### `CatalogDriverInfluencerMap`

Composite PK: `(DriverId, InfluencerId)` + `RelationshipType`, `DefaultWeight`, `RuleNotes`.

#### `CatalogScoreComponent`

From `Scoring_Logic` sheet: `Component`, `ValueRange`, `WeightPercent`, `RequirementLevel`, `ImplementationNotes`.

#### `CatalogModule` / `CatalogOutputArea`

From `Module_Routing` and `Output_Assignment` sheets.

**Do not** merge catalog rows into `KpiDefinitions` on import. Keep parallel until calculators exist.

### 5.2 Phase 3 — Priority & selection

#### `IssuePriorityScore`

| Column | Type |
|--------|------|
| `Id` | `long` PK |
| `TenantId` | `Guid` |
| `PeriodEnd` | `DateOnly` |
| `KpiDefinitionId` | `int` FK (nullable during transition) |
| `CatalogKpiId` | `string?` FK |
| `SeverityScore` … `ConfidenceScore` | `decimal` |
| `FinalScore` | `decimal` |
| `Rank` | `int` |
| `Status` | `string` | R/Y/G/GRAY |

Unique index: `(TenantId, PeriodEnd, KpiDefinitionId)` or `(TenantId, PeriodEnd, CatalogKpiId)`.

#### `TenantKpiSelection`

| Column | Type |
|--------|------|
| `TenantId` | `Guid` |
| `CatalogKpiId` | `string` |
| `IsPinned` | `bool` |
| `IsExcluded` | `bool` |

### 5.3 Phase 5 — Influencer evidence (runtime)

#### `InfluencerEvidence`

| Column | Type |
|--------|------|
| `Id` | `long` |
| `TenantId` | `Guid` |
| `PeriodEnd` | `DateOnly` |
| `DriverValueId` | `int` FK |
| `InfluencerId` | `string` FK → `CatalogInfluencer` |
| `Severity` | `string?` |
| `EvidenceSummary` | `string` |
| `Confidence` | `string?` |
| `Weight` | `int` |

### 5.4 Phase 6 — Routing queues

#### `RoutingQueueItem`

| Column | Type |
|--------|------|
| `Id` | `long` |
| `TenantId` | `Guid` |
| `PeriodEnd` | `DateOnly` |
| `QueueType` | enum/string | `Management`, `DrillDown`, `Watchlist`, `DataGap`, `ModuleAction`, `Review` |
| `CatalogKpiId` | `string?` |
| `CatalogDriverId` | `string?` |
| `ModuleCode` | `string?` |
| `Title` | `string` |
| `Severity` | `string?` |
| `FinalScore` | `decimal?` |
| `Status` | `string` | Open, Closed, … |
| `CreatedAt` | `DateTimeOffset` |

### 5.5 Phase 7 — Workflow (meeting)

#### `HoldoverComment`

`DriverValueId`, `AuthorUserId` (Guid), `Body`, `CreatedAt`.

#### `HoldoverStatusHistory`

`DriverValueId`, `Status`, `FixProgressPercent`, `ChangedByUserId`, `ChangedAt`.

#### `WorkAssignment`

`TenantId`, `PeriodEnd`, `TargetType` (Kpi/Driver/ActionItem), `TargetId`, `AssigneeUserId`, `AssignedByUserId`, `AssignedAt`.

#### `UserNotification`

`UserId`, `Title`, `Body`, `LinkUrl`, `IsRead`, `CreatedAt`.

---

## 6. Service specifications

### 6.1 `ICatalogImportService` (Infrastructure)

**Path:** `Infrastructure/Catalog/CatalogImportService.cs`  
**Interface:** `Domain/Catalog/ICatalogImportService.cs`

```csharp
Task<CatalogImportResult> ImportFromWorkbookAsync(Stream xlsx, CancellationToken ct);
```

- Parse sheets listed in §2.4.
- Upsert by stable ID.
- Return counts + errors; no throw on single bad row (collect errors).

**Admin UI:** `Pages/Admin/Catalog/Import.cshtml` (`AdminOnly`), file upload → import → summary.

**Test fixture:** copy catalog xlsx to `tests/DecisionOS.Distribution.Tests/Fixtures/DPOS_Catalog_v1.0.xlsx`.

### 6.2 `IKpiCalculator` + orchestrator (Infrastructure)

**Path:** `Infrastructure/Scoring/Calculators/*`, `KpiCalculationOrchestrator.cs`

Extract existing methods from `WeeklyScoringService` into:

| Class | `LegacyCode` |
|-------|----------------|
| `GrossMarginKpiCalculator` | `GrossMargin%` |
| `ArPastDueKpiCalculator` | `AR_PastDue31p%` |
| `ApPastDueKpiCalculator` | `AP_PastDue31p%` |
| `DohKpiCalculator` | `DOH` |
| `CccKpiCalculator` | `CCC` |
| `NetProfitKpiCalculator` | `NetProfit%` |
| `PerfectOrderRateKpiCalculator` | `PerfectOrderRate` |

**Context type:** `KpiCalculationContext` — `TenantId`, `UploadBatchId`, `PeriodEnd`, `DirectKpiValues`, `DecisionOsDbContext` access via factory/repository (keep calculators testable).

**Orchestrator behavior:**

```
if (!UseCatalogEngine) → call existing WeeklyScoringService.ScorePeriodAsync (delegate, do not duplicate)
else:
  resolved = DefinitionResolver.ResolveKpiDefinitionsAsync(tenant)
  foreach catalog KPI with calculator OR legacy map:
    compute → write KpiSnapshot (all evaluated)
  invoke PriorityRankingService
  invoke DriverEvaluationService (if enabled chain)
  …
```

**Register calculators:** `Program.cs` — `services.AddScoped<IKpiCalculator, GrossMarginKpiCalculator>();` etc.

### 6.3 `IPriorityRankingService` (Domain interface, Infrastructure impl)

**Path:** `Domain/Scoring/IPriorityRankingService.cs`, `Infrastructure/Scoring/PriorityRankingService.cs`

**Inputs:** `IReadOnlyList<KpiSnapshot>`, tenant, period, `CatalogScoreComponent` weights, `TenantKpiSelection`.

**Algorithm (v1):**

1. Build candidate set: all RED/YELLOW snapshots + pinned GREEN.
2. Exclude tenant-excluded catalog KPIs.
3. Per candidate, compute component scores (0–100):
   - **Severity:** from `KpiStatusService` status + gap vs target (normalize per unit).
   - **Cash / Financial:** category heuristic (AR/AP/CCC/DOH = high; use catalog `Category`).
   - **Urgency:** `WeekOverWeekDelta` deterioration.
   - **Actionability:** 100 if `RecommendedAction` present and status ≠ GRAY.
   - **Confidence:** map `DataConfidence` string → 0–100; if &lt; 40, cap `FinalScore` at 40.
4. `FinalScore = weighted sum` per `Scoring_Logic` sheet.
5. Persist `IssuePriorityScore`; assign `Rank`.
6. Top 7 → management layer; ranks 8+ → watchlist queue (if routing enabled).

### 6.4 `IDriverEvaluationService`

**Trigger:** parent KPI status ∈ {RED, YELLOW} OR holdover carry-forward.

**Steps:**

1. Load `CatalogKpiDriverMap` for KPI’s `CatalogKpiId` (via `LegacyCode` bridge).
2. For each driver, evaluate evidence from normalized rows (`EvidenceFields` — start with keyword/column presence checks).
3. Write `DriverValue` (existing table); add optional `CatalogDriverId` column in migration.
4. Rank with existing `DriverRankingService`.

### 6.5 `IInfluencerEvidenceService`

**Trigger:** active `DriverValue` for period.

1. Load `CatalogDriverInfluencerMap`.
2. Match influencer `EvidenceFields` against normalized data + driver context.
3. Insert `InfluencerEvidence` rows.
4. Surface in KPI modal + holdover modal (“Root causes” section).

### 6.6 `IModuleRoutingService`

**Input:** scored issues + priority scores.

| Condition | Queue |
|-----------|-------|
| Status = GRAY | `DataGap` |
| Confidence &lt; 40 | `Review` |
| Rank 1–7 | `Management` (also dashboard) |
| Rank 8–20 | `Watchlist` |
| Module match from `CatalogModule` | `ModuleAction` |
| Drill-down detail | `DrillDown` |

Enqueue `RoutingQueueItem`; do not duplicate holdover (`DriverValue` remains source for holdover UI).

---

## 7. UI changes by phase

| Phase | Page / area | Change |
|-------|-------------|--------|
| 1 | `/Admin/Catalog/Import` | Upload catalog xlsx, show counts |
| 1 | `/Admin/Catalog/Index` | Read-only list KPI/Driver/Influencer counts |
| 3 | `/Dashboard` | When `UseDynamicTop7`: tiles from `IssuePriorityScore` rank ≤ 7 |
| 3 | `/Admin/Tenants/Overrides/Kpis` | Pin/exclude catalog KPIs (extend existing) |
| 6 | `/Operations/ModuleQueue` | List `ModuleAction` queue (`OpsPolicy`) |
| 6 | `/Dashboard/DrillDown` | All issues rank &gt; 7 or drill-down queue |
| 7 | `/Notifications` | Bell in `_Layout.cshtml` → unread count + list |
| 7 | Holdover modal | Comment thread + status history |
| 7 | Dashboard | Assign holdover/KPI to Identity user |

**Dashboard change detail (`Dashboard.cshtml.cs`):**

```csharp
// When UseDynamicTop7:
Snapshots = await LoadTop7SnapshotsAsync(tenant, periodEnd);
// Else: existing EnsureSevenPillarDisplay path
```

---

## 8. Phased implementation plan

### Phase 0 — Governance (2–3 days)

| Task | Files | Done when |
|------|-------|-----------|
| Remove placeholder `BusinessProfile` rows | migration or `ReferenceDataSeeder` cleanup script | Only industry templates remain |
| Archetype: remove or rename UI field | `Pages/Admin/Tenants/Edit.cshtml`, `Create.cshtml`, `Tenant.cs` | No misleading “controls logic” copy |
| Copy catalog xlsx to test fixtures | `tests/.../Fixtures/DPOS_Catalog_v1.0.xlsx` | File committed |
| Add `DecisionOs` config section | `appsettings.json`, options class | Flags default false |

**No scoring changes.**

---

### Phase 1 — Catalog import (4–5 days)

| # | Task | Location |
|---|------|----------|
| 1.1 | Domain entities §5.1 | `Domain/Catalog/*.cs` |
| 1.2 | EF configuration + migration | `DecisionOsDbContext.cs`, `Migrations/` |
| 1.3 | `CatalogImportService` | `Infrastructure/Catalog/` |
| 1.4 | `ICatalogImportService` | `Domain/Catalog/` |
| 1.5 | Admin Import page | `Pages/Admin/Catalog/Import.cshtml` |
| 1.6 | Register DI | `Program.cs` |
| 1.7 | Tests | `CatalogImportServiceTests.cs` |

**Acceptance**

- [ ] Import → 24 / 36 / 60 KPI/Driver/Influencer rows
- [ ] Maps: 84 + 60 rows
- [ ] Re-import idempotent
- [ ] `Catalog.Enabled=false` → no runtime use
- [ ] All 91+ tests pass

**Agent prompt**

```
Implement Phase 1 of docs/Catalog-Engine-Implementation-Plan.md only.
Do not modify WeeklyScoringService scoring behavior.
```

---

### Phase 2 — KPI calculator extraction (4–5 days)

| # | Task | Location |
|---|------|----------|
| 2.1 | `IKpiCalculator`, `KpiCalculationContext`, `KpiCalculationResult` | `Domain/Scoring/` |
| 2.2 | Seven calculator classes (extract from `WeeklyScoringService`) | `Infrastructure/Scoring/Calculators/` |
| 2.3 | `KpiCalculationOrchestrator` with flag guard | `Infrastructure/Scoring/` |
| 2.4 | Wire orchestrator from `SimplifiedWorkbookImportService` / `UploadBatchImportService` **behind flag** | existing import services |
| 2.5 | Parity tests | `KpiCalculatorParityTests.cs` |

**Acceptance**

- [ ] Flag OFF: byte-identical snapshots for fixture workbook (7 KPIs)
- [ ] Flag ON: same 7 values; orchestrator path used
- [ ] `WeeklyScoringService` body preserved as fallback delegate

---

### Phase 3 — Dynamic top-7 (3–4 days)

| # | Task | Location |
|---|------|----------|
| 3.1 | `IssuePriorityScore`, `TenantKpiSelection` entities + migration | Domain + Infrastructure |
| 3.2 | `PriorityRankingService` | Infrastructure/Scoring/ |
| 3.3 | Seed weights from `CatalogScoreComponent` | import or migration seed |
| 3.4 | Dashboard top-7 loader | `Dashboard.cshtml.cs` |
| 3.5 | Tenant pin/exclude UI | extend `Pages/Admin/Tenants/Overrides/Kpis*` |
| 3.6 | Tests | `PriorityRankingServiceTests.cs` |

**Acceptance**

- [ ] Different RED KPIs → different top-7 order
- [ ] Pinned GREEN KPI appears in top 7
- [ ] Flag OFF: `SevenPillarCodes` unchanged

---

### Phase 4 — Catalog driver evaluation (3–4 days)

| # | Task | Location |
|---|------|----------|
| 4.1 | `CatalogDriverId` on `DriverValue` (nullable) | migration |
| 4.2 | `DriverEvaluationService` | Infrastructure/Scoring/ |
| 4.3 | Integrate after scoring in orchestrator | |
| 4.4 | Tests with map fixture | `DriverEvaluationServiceTests.cs` |

**Acceptance**

- [ ] Drivers not created for all-GREEN week (except holdover import)
- [ ] RED `AR_PastDue31p%` triggers mapped AR drivers per catalog

---

### Phase 5 — Influencer runtime (3–4 days)

| # | Task | Location |
|---|------|----------|
| 5.1 | `InfluencerEvidence` entity + migration | |
| 5.2 | `InfluencerEvidenceService` | Infrastructure/Scoring/ |
| 5.3 | Dashboard modal “Root causes” | `Dashboard.cshtml` + insight JSON |
| 5.4 | Tests | `InfluencerEvidenceServiceTests.cs` |

**Acceptance**

- [ ] At least one KPI→Driver→Influencer chain produces evidence on fixture data
- [ ] No evidence when driver not triggered

---

### Phase 6 — Module routing queues (3–4 days)

| # | Task | Location |
|---|------|----------|
| 6.1 | `RoutingQueueItem` + migration | |
| 6.2 | `ModuleRoutingService` | Infrastructure/Routing/ |
| 6.3 | `/Operations/ModuleQueue` page | Pages/Operations/ |
| 6.4 | Watchlist panel on dashboard | `Dashboard.cshtml` |
| 6.5 | Tests | `ModuleRoutingServiceTests.cs` |

**Acceptance**

- [ ] GRAY KPI → `DataGap` queue row, not in top 7
- [ ] `Routing.Enabled=false` → no queue rows

---

### Phase 7 — Workflow & notifications (5–7 days) ✅ delivered

| # | Task | Location |
|---|------|----------|
| 7.1 | Comment + history tables | `HoldoverComment`, `HoldoverStatusHistory`, migration `CatalogEngine` |
| 7.2 | Holdover modal comment thread | `Dashboard.cshtml` |
| 7.3 | `WorkAssignment` + assign UI | `HoldoverWorkflowService`, Dashboard + API |
| 7.4 | `UserNotification` + `/Notifications` page | `NotificationService`, `Pages/Notifications` |
| 7.5 | Wire bell icon | `_Layout.cshtml` + `/api/notifications/unread-count` |
| 7.6 | Tests | `HoldoverWorkflowServiceTests`, `OwnerLanguageTests` |

**Acceptance**

- [x] Assign holdover → assignee sees bell notification
- [x] Comment visible on holdover thread
- [x] **No email** sent

---

### Phase 8 — Intelligent ingestion spike (5+ days, decision gate)

| # | Task | Location |
|---|------|----------|
| 8.1 | Synonym map: Gross Profit, Net Profit, AR, AP, COGS | `ColumnSynonymMatcher.cs` |
| 8.2 | P&L row-label detection in `WorkbookAnalyzer` | |
| 8.3 | Spike doc | `docs/Ingestion-Rule-Based-Spike.md` |

**Do not add LLM** until client approves. Report coverage % on Terry + Syd files.

---

## 9. Meeting requirements traceability

| Meeting item | Phase | Spec section |
|--------------|-------|--------------|
| Full KPI library (24) | 1 | §5.1, §8 Phase 1 |
| Dynamic top-7 + tenant override | 3 | §6.3, §5.2 |
| Driver catalog (36) | 4 | §6.4 |
| Influencer library (60) runtime | 5 | §6.5 |
| Module routing queues | 6 | §6.6, §5.4 |
| Plain language UI | 0 | `OwnerLanguage.cs` (extend as needed) |
| Expandable notes | 0/3 | KPI modal + cards (done partial) |
| Holdover comments/history | 7 | §5.5 — **done** (`HoldoverWorkflowService`) |
| Task assignment + bell | 7 | §5.5 — **done** |
| In-app only (no email) | 7 | §8 Phase 7 — **done** |
| Business profile cleanup | 0 | §8 Phase 0 |
| Intelligent ingestion | 8 | §8 Phase 8 |
| LLM vs rules | 8 | spike doc only |

---

## 10. Testing requirements

### 10.1 Commands (every PR)

```powershell
cd <repo-root>
dotnet build
dotnet test tests/DecisionOS.Distribution.Tests
```

### 10.2 Regression tests (must pass every phase)

| Test class | Why |
|------------|-----|
| `SimplifiedWorkbookImportServiceTests` | Terry/Syd upload path |
| `UploadBatchImportServiceTests` | Classic path |
| `SevenPillarKpiStatusTests` | 7 KPI status logic |
| `HoldoverDashboardFilterTests` | Holdover display |
| `DefinitionResolverTests` | Tenant/profile overrides |

### 10.3 New tests by phase

| Phase | New test file |
|-------|---------------|
| 1 | `CatalogImportServiceTests` |
| 2 | `KpiCalculatorParityTests` |
| 3 | `PriorityRankingServiceTests` |
| 4 | `DriverEvaluationServiceTests` |
| 5 | `InfluencerEvidenceServiceTests` |
| 6 | `ModuleRoutingServiceTests` |
| 7 | `HoldoverWorkflowServiceTests`, `OwnerLanguageTests` |

### 10.4 Manual smoke

1. Start web: `dotnet run --project src/DecisionOS.Distribution.Web`
2. Simplified import: `tests/.../Fixtures/Steves_Bowling_Supply_DPOS_Distribution_Test_Data.xlsx`
3. Dashboard: 7 tiles, holdover rows clickable, assign + comment modal, week selector works
4. Notifications: assign holdover as admin → log in as assignee → bell shows unread

---

## 11. Legacy code bridge

Map catalog KPIs to existing calculators (Phase 2):

| LegacyCode (`KpiDefinition.Code`) | Calculator class | Data source |
|-----------------------------------|------------------|-------------|
| `GrossMargin%` | `GrossMarginKpiCalculator` | `NormalizedSalesRows` |
| `AR_PastDue31p%` | `ArPastDueKpiCalculator` | `NormalizedArRows` |
| `AP_PastDue31p%` | `ApPastDueKpiCalculator` | `NormalizedApRows` |
| `DOH` | `DohKpiCalculator` | Inventory + sales COGS |
| `CCC` | `CccKpiCalculator` | Sales + AR + AP + DOH |
| `NetProfit%` | `NetProfitKpiCalculator` | Direct rollup / `Weekly_Financials` |
| `PerfectOrderRate` | `PerfectOrderRateKpiCalculator` | Direct rollup |

Catalog KPIs **without** calculator + data → `GRAY` + `DataGap` queue (Phase 6). **Do not invent formulas.**

Populate `CatalogKpi.LegacyCode` on import where names/codes match seed.

---

## 12. Explicit out of scope

- Email, SMS, Teams, Slack
- LLM/OpenAI mapping (until Phase 8 approval)
- Live ERP integration
- `Customer` entity or Admin Create Customer
- New KPI codes on dashboard without catalog row + calculator
- Retargeting off `net8.0`
- Redis, SignalR, microservices

---

## 13. Per-PR agent checklist

```
[ ] Read decisionos-domain-truth.mdc + this doc phase section
[ ] Feature flag introduced default OFF in appsettings
[ ] Layer boundaries respected (no EF in Domain)
[ ] Migration added if schema changed
[ ] Program.cs updated for DI + auth conventions if new pages
[ ] Tests added/updated for phase
[ ] dotnet build && dotnet test pass
[ ] Simplified import smoke test noted in PR
[ ] No static unbounded caches
[ ] Cited file paths in PR description
```

---

## 14. Definition of done (program)

- [ ] Catalog xlsx imports idempotently via Admin
- [ ] `UseCatalogEngine=true`: all mapped KPIs evaluated; 7 legacy KPIs match prior values
- [ ] `UseDynamicTop7=true`: dashboard shows top 7 by `FinalScore`; pin/exclude works
- [ ] Drivers triggered from catalog maps on RED/YELLOW KPIs
- [ ] Influencer evidence on triggered drivers
- [ ] Routing queues populated per rules
- [ ] In-app assignment, comments, notifications (no email)
- [ ] All flags OFF: identical behavior to pre-catalog Terry test

---

## 15. Document history

| Date | Change |
|------|--------|
| 2026-07-06 | Initial agent implementation plan |
