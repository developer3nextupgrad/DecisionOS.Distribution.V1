# DecisionOS Distribution — reference

## Solution projects

| Project | Path |
|---------|------|
| Domain | `src/DecisionOS.Distribution.Domain` |
| Infrastructure | `src/DecisionOS.Distribution.Infrastructure` |
| Web | `src/DecisionOS.Distribution.Web` |
| Import CLI | `src/DecisionOS.Distribution.Import` |
| Tests | `tests/DecisionOS.Distribution.Tests` |

## Key entities (`DecisionOsDbContext`)

`Tenant`, `KpiDefinition`, `KpiSnapshot`, `DriverValue`, `DriverDefinition`, `Alert`, `WeeklyFocus`, `ImportRun`, `UploadBatch`, `UploadedFile`, `UploadedFileColumnMap`, `UploadBatchIssue`, `NormalizedSalesRow`, `NormalizedInventoryRow`, `NormalizedArRow`, `NormalizedApRow`, `ActionItem`, `BusinessProfile`, `VerticalLibrary`, overrides, Identity tables.

**Workflow (holdover execution):** `HoldoverComment`, `HoldoverStatusHistory`, `WorkAssignment`, `UserNotification` — see `Domain/Workflow/`.

**Catalog engine (flags default off):** `CatalogKpi`, `CatalogDriver`, `RoutingQueueItem`, `IssuePriorityScore`, `InfluencerEvidence`, etc. — migration `CatalogEngine`.

**No** `Customer` table.

## Auth

- Policies: `AdminOnly`, `OpsPolicy`, `AnyDistributionRole`
- Roles: `AppRoles` — Admin, Operator, Viewer, Developer
- Seed admin: `admin@decisionos.local` / `ChangeMe!DecisionOS1` (from config `SeedAdmin:*`)
- Provision assignees: **Admin → Users** (`Pages/Admin/Users`)

## Web routes (main)

| Area | Path |
|------|------|
| Dashboard | `/Dashboard?ClientId=&PeriodEnd=` (`&view=holdover` for holdover focus) |
| Notifications | `/Notifications` |
| Upload list | `/Operations/Uploads` |
| New batch | `/Operations/Uploads/Create` |
| Classic batch | `/Operations/Uploads/Details?id=` |
| Map | `/Operations/Uploads/Map?batchId=&fileId=` |
| Simplified detect | `/Operations/Uploads/Simplified/Detect?id=` |
| Simplified verify | `/Operations/Uploads/Simplified/Verify?id=` |
| Admin tenants | `/Admin/Tenants` |
| Admin users | `/Admin/Users` |
| Health | `/health` (anonymous) |
| API | `/api/*` (`AnyDistributionRole`) |

## API — holdover workflow

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/users/assignable` | Users for assign dropdown |
| GET | `/api/notifications/unread-count` | Bell badge |
| GET | `/api/tenants/{clientId}/drivers/{id}/workflow` | Comment thread + assignee |
| POST | `/api/tenants/{clientId}/drivers/{id}/assign` | Assign Identity user; notify |
| POST | `/api/tenants/{clientId}/drivers/{id}/comments` | Add thread message; notify |
| POST | `/api/tenants/{clientId}/drivers/{id}/owner` | Legacy free-text owner (flag off path) |

Requires `DecisionOs:Workflow:AssignmentsEnabled` for assign/comment APIs.

## Import & scoring services (DI in `Program.cs`)

- `UploadBatchImportService`
- `IWeeklyScoringService` → `KpiCalculationOrchestrator` (catalog flag) or `WeeklyScoringService`
- `IWorkbookAnalyzer` → `WorkbookAnalyzer`
- `ISimplifiedWorkbookImportService` → `SimplifiedWorkbookImportService`
- `IKpiStatusService`, `IAlertService`, `IWeeklyFocusService`, `IDriverRankingService`
- `IHoldoverWorkflowService` → `HoldoverWorkflowService`
- `INotificationService` → `NotificationService`

## Owner-facing copy

- `Domain/OwnerLanguage.cs` — plain status labels, gap text, missing-data checklists
- `DashboardKpiInsightBuilder` — KPI modal JSON
- `KpiScoringNarrative` — card lines

## Feature flags (`DecisionOs` in `appsettings.json`)

| Section | Keys | Default (committed) |
|---------|------|---------------------|
| `Catalog` | `Enabled` | false |
| `Scoring` | `UseCatalogEngine`, `UseDynamicTop7` | false |
| `Routing` | `Enabled` | false |
| `Workflow` | `AssignmentsEnabled`, `NotificationsEnabled` | **true** |
| `Ingestion` | `RuleBasedExpansionEnabled` | false |

Bind: `DecisionOsFeatureOptions` via `IOptions<>`.

## Workbook infrastructure

`Infrastructure/Workbooks/`: `WorkbookParseHelper`, `SheetClassifier`, `ColumnSynonymMatcher`, `PeriodExtractor`, `WorkbookAnalyzer`

## `UploadBatch` (simplified fields)

`ImportMode`, `Cadence`, `AnchorPeriodEnd`, `WorkbookFingerprint`, `DetectionSummaryJson`, `WorkbookStoredRelativePath`

## Tests (workflow)

- `HoldoverWorkflowServiceTests.cs`
- `OwnerLanguageTests.cs`, `DashboardKpiInsightBuilderTests.cs`

## Scripts

- `scripts/run.ps1` — DB setup + web
- `scripts/setup-database.ps1` — migrate + Import CLI

## Connection string

`ConnectionStrings:DecisionOs` or env `ConnectionStrings__DecisionOs`
