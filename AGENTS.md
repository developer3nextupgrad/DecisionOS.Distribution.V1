# AGENTS.md — DecisionOS Distribution V1

Instructions for AI agents (Cursor, Copilot, etc.) working in this repository.

## Read first (in order)

1. [.cursor/rules/decisionos-domain-truth.mdc](.cursor/rules/decisionos-domain-truth.mdc) — what exists; tenant vs end customer
2. [.cursor/rules/decisionos-anti-hallucination.mdc](.cursor/rules/decisionos-anti-hallucination.mdc) — verify in repo before claiming
3. [.cursor/rules/decisionos-distribution.mdc](.cursor/rules/decisionos-distribution.mdc) — stack, layers, auth
4. Skill: [.cursor/skills/decisionos-distribution/SKILL.md](.cursor/skills/decisionos-distribution/SKILL.md) + [reference.md](.cursor/skills/decisionos-distribution/reference.md)

## Project in one paragraph

**DecisionOS Distribution** is a .NET 8 web app for **distributor** companies: operators upload weekly files (CSV/Excel), data is validated and mapped, the system computes health **KPIs** (red/yellow/green), selects a top **alert**, ranks **drivers**, and shows a **dashboard** per **tenant** and **week ending date**. V1 is **manual upload only** (no live ERP). Each **tenant** is one distributor; **end customers** (buyers) appear only inside import data, not as separate tenants.

## Starter prompts (copy for users or agents)

### Understand the codebase

```
MODE: BALANCED
Explore DecisionOS Distribution V1. Summarize: solution layers, weekly data flow (upload → KPI → dashboard), Classic vs Simplified upload. Cite files. Do not invent features — grep to verify. Apply decisionos-domain-truth and anti-hallucination rules.
```

### Onboarding / tenant vs customer

```
Explain who is a Tenant vs an end customer in this project. How do we onboard a new distributor? How do end customers get into the system? Only use facts from decisionos-domain-truth.mdc and the user guide — no Admin Customer UI unless you verify it in Pages/.
```

### Implement upload/import change

```
Task: [describe change]

Constraints:
- .NET 8, existing layer boundaries (Domain / Infrastructure / Web)
- Preserve Classic upload behavior unless asked to change it
- Simplified flow uses WorkbookAnalyzer + SimplifiedWorkbookImportService + IWeeklyScoringService
- Grep UploadBatchImportService and SimplifiedWorkbookImportService before editing
- Add/update tests in DecisionOS.Distribution.Tests
- Run dotnet build and dotnet test
```

### Fix a bug without hallucinating

```
Bug: [symptoms]

Before fixing: read the code path (Razor page → service → DbContext). State root cause with file:line citations. Do not add new tables or APIs unless required. Match existing patterns.
```

### Database / migration

```
Need EF migration for [entity/field].

Use DecisionOsDbContext in Infrastructure, connection string DecisionOs. dotnet ef migrations add [Name] --project src/DecisionOS.Distribution.Infrastructure --startup-project src/DecisionOS.Distribution.Web. Do not hardcode passwords.
```

### Run and test locally

```
Build, apply migrations, run tests (including Workbook/Simplified filters if import-related), start Web on port 5276. Report pass/fail with commands used. Fixture: tests/DecisionOS.Distribution.Tests/Fixtures/Steves_Bowling_Supply_DPOS_Distribution_Test_Data.xlsx
```

## Rules index

| Rule | Scope |
|------|--------|
| `decisionos-domain-truth.mdc` | Always — domain facts |
| `decisionos-anti-hallucination.mdc` | Always — verify before claim |
| `decisionos-distribution.mdc` | Always — stack & conventions |
| `decisionos-upload-import.mdc` | Upload/import/workbook files |
| `high-efficiency-rule.mdc` | Output style |

## When the agent must say "unknown"

- UI routes not under `Pages/` or `Program.cs`
- KPI codes not in seed / `KpiDefinitions`
- Features described in client emails but not in `src/`
- Future Phase 2 items (customer master table, live integrations) unless implemented

Ask the user or search the repo — do not fabricate.
