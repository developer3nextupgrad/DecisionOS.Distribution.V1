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

**No** `Customer` table.

## Auth

- Policies: `AdminOnly`, `OpsPolicy`, `AnyDistributionRole`
- Roles: `AppRoles` — Admin, Operator, Viewer, Developer
- Seed admin: `admin@decisionos.local` / `ChangeMe!DecisionOS1` (from config `SeedAdmin:*`)

## Web routes (main)

| Area | Path |
|------|------|
| Dashboard | `/Dashboard?ClientId=&PeriodEnd=` |
| Upload list | `/Operations/Uploads` |
| New batch | `/Operations/Uploads/Create` |
| Classic batch | `/Operations/Uploads/Details?id=` |
| Map | `/Operations/Uploads/Map?batchId=&fileId=` |
| Simplified detect | `/Operations/Uploads/Simplified/Detect?id=` |
| Simplified verify | `/Operations/Uploads/Simplified/Verify?id=` |
| Admin tenants | `/Admin/Tenants` |
| Health | `/health` (anonymous) |
| API | `/api/*` (`AnyDistributionRole`) |

## Import services (DI in `Program.cs`)

- `UploadBatchImportService`
- `IWeeklyScoringService` → `WeeklyScoringService`
- `IWorkbookAnalyzer` → `WorkbookAnalyzer`
- `ISimplifiedWorkbookImportService` → `SimplifiedWorkbookImportService`
- `IKpiStatusService`, `IAlertService`, `IWeeklyFocusService`, `IDriverRankingService`

## Workbook infrastructure

`Infrastructure/Workbooks/`: `WorkbookParseHelper`, `SheetClassifier`, `ColumnSynonymMatcher`, `PeriodExtractor`, `WorkbookAnalyzer`

## `UploadBatch` (simplified fields)

`ImportMode`, `Cadence`, `AnchorPeriodEnd`, `WorkbookFingerprint`, `DetectionSummaryJson`, `WorkbookStoredRelativePath`

## Scripts

- `scripts/run.ps1` — DB setup + web
- `scripts/setup-database.ps1` — migrate + Import CLI

## Connection string

`ConnectionStrings:DecisionOs` or env `ConnectionStrings__DecisionOs`
