---
name: decisionos-distribution
description: >-
  DecisionOS Distribution V1 (.NET 8): tenants, weekly KPI dashboard, classic
  and simplified workbook import. Use when working in this repo, onboarding,
  uploads, KPIs, drivers, PostgreSQL/EF, or when the user mentions Decision OS,
  distributors, tenants, or anti-hallucination project context.
---

# DecisionOS Distribution

## First actions

1. Read [.cursor/rules/decisionos-domain-truth.mdc](../../rules/decisionos-domain-truth.mdc) — **Tenant ≠ end customer**.
2. Read [reference.md](reference.md) for routes, services, and file map.
3. **Grep before claiming** behavior; cite `path` or user guide.

## Core product idea

Manual weekly import → validate/map → compute **7 KPIs** → top **Alert** + **WeeklyFocus** + **DriverValue** rows → **Dashboard** per distributor (`Tenant`) and **PeriodEnd**.

## Implementation checklist

| Task | Where |
|------|--------|
| New distributor | `Pages/Admin/Tenants`, `Tenant` entity |
| End buyer data | Import CSV/workbook only — no Customer admin UI |
| Classic upload | `UploadBatchImportService`, `Pages/Operations/Uploads/Details`, `Map` |
| Simplified upload | `SimplifiedWorkbookImportService`, `Pages/Operations/Uploads/Simplified/*` |
| Scoring | `WeeklyScoringService`, `KpiStatusService`, `AlertService`, `WeeklyFocusService` |
| CLI import | `DecisionOS.Distribution.Import/Program.cs` |
| DB migrate | `dotnet ef database update` — Infrastructure + Web startup |

## Do not invent

See domain-truth rule: no Customer DbSet, no live integrations, no extra KPI codes without seed/migration check.

## Verify commands

```powershell
dotnet build
dotnet test tests/DecisionOS.Distribution.Tests
dotnet ef database update --project src/DecisionOS.Distribution.Infrastructure --startup-project src/DecisionOS.Distribution.Web
```

## Deep dive

- Operator flows: `_client_docs/DecisionOS_Distribution_User_Guide.md`
- Architecture: `docs/Solution-Linkage-and-Import-Analysis.md`
- Agent prompts: [AGENTS.md](../../../AGENTS.md)
