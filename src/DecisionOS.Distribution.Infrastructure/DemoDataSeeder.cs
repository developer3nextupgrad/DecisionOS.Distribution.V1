using DecisionOS.Distribution.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DecisionOS.Distribution.Infrastructure;

public static class DemoDataSeeder
{
    public static async Task SeedDemoDashboardIfNeededAsync(
        DecisionOsDbContext db,
        ILogger logger,
        string clientId,
        CancellationToken ct = default)
    {
        // Idempotency: if tenant already has snapshots, assume demo exists.
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.ClientId == clientId, ct);
        if (tenant is not null)
        {
            var hasAnySnapshots = await db.KpiSnapshots.AnyAsync(s => s.TenantId == tenant.Id, ct);
            if (hasAnySnapshots)
            {
                logger.LogInformation("Demo seed skipped (existing snapshots) for {ClientId}.", clientId);
                return;
            }
        }

        var defaultProfileId = await db.BusinessProfiles
            .Where(p => p.Code == "DISTRIBUTION_DEFAULT")
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct);

        tenant ??= new Tenant
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            Name = "Demo Distribution Co",
            Archetype = "demo",
            BusinessProfileId = defaultProfileId
        };

        if (db.Entry(tenant).State == EntityState.Detached)
            db.Tenants.Add(tenant);

        await db.SaveChangesAsync(ct);

        var periodEnd = GetMostRecentSundayUtc();
        var previousPeriodEnd = periodEnd.AddDays(-7);

        var defs = await db.KpiDefinitions.AsNoTracking().ToListAsync(ct);
        if (defs.Count == 0)
        {
            logger.LogWarning("Demo seed aborted: no KPI definitions found.");
            return;
        }

        var kpiStatus = new KpiStatusService();

        decimal GetCurrentValue(string code) => code switch
        {
            "CCC" => 68m,
            "AR_PastDue31p%" => 0.17m,
            "NetProfit%" => 0.042m,
            "GrossMargin%" => 0.257m,
            "DOH" => 62m,
            "AP_PastDue31p%" => 0.14m,
            "PerfectOrderRate" => 0.905m,
            _ => defs.First(d => d.Code == code).Target
        };

        decimal GetPreviousValue(string code) => code switch
        {
            "CCC" => 63m,
            "AR_PastDue31p%" => 0.16m,
            "NetProfit%" => 0.046m,
            "GrossMargin%" => 0.262m,
            "DOH" => 58m,
            "AP_PastDue31p%" => 0.13m,
            "PerfectOrderRate" => 0.912m,
            _ => defs.First(d => d.Code == code).Target
        };

        foreach (var def in defs)
        {
            var prev = GetPreviousValue(def.Code);
            var cur = GetCurrentValue(def.Code);
            db.KpiSnapshots.Add(new KpiSnapshot
            {
                TenantId = tenant.Id,
                PeriodEnd = previousPeriodEnd,
                KpiDefinitionId = def.Id,
                Value = prev,
                Status = kpiStatus.ComputeStatus(def, prev),
                DataConfidence = "High",
                CardDetailLine1 = "Baseline week",
                CardDetailLine2 = "Stable operations"
            });
            db.KpiSnapshots.Add(new KpiSnapshot
            {
                TenantId = tenant.Id,
                PeriodEnd = periodEnd,
                KpiDefinitionId = def.Id,
                Value = cur,
                Status = kpiStatus.ComputeStatus(def, cur),
                WeekOverWeekDelta = cur - prev,
                DataConfidence = "High",
                CardDetailLine1 = "Exec highlight: focus areas",
                CardDetailLine2 = "Drivers & actions below"
            });
        }

        var alertDef = defs.FirstOrDefault(d => d.Code == "CCC") ?? defs[0];
        db.Alerts.Add(new Alert
        {
            TenantId = tenant.Id,
            PeriodEnd = periodEnd,
            KpiDefinitionId = alertDef.Id,
            Severity = "High",
            ReasonSummary = "Cash cycle worsened due to slower collections and higher inventory days."
        });

        db.WeeklyFocuses.Add(new WeeklyFocus
        {
            TenantId = tenant.Id,
            PeriodEnd = periodEnd,
            KpiDefinitionId = alertDef.Id,
            DecisionQuestion = "Which two levers will move CCC fastest this week?",
            RecommendedAction = "Prioritize top 10 past-due accounts + pull-forward slow inventory promotions.",
            WhyNow = "Working capital is tightening; improving cash unlocks operating flexibility.",
            Owner = "Ops Lead",
            Cadence = "Weekly"
        });

        var now = DateTimeOffset.UtcNow;
        db.ActionItems.AddRange(
            new ActionItem
            {
                TenantId = tenant.Id,
                PeriodEnd = periodEnd,
                KpiDefinitionId = alertDef.Id,
                Title = "Collections blitz on top 10 past-due accounts",
                Description = "Assign owner + next-step for each account; escalate disputes older than 14 days.",
                Owner = "AR Manager",
                DueDate = periodEnd.AddDays(3),
                Status = ActionStatuses.InProgress,
                Priority = 10,
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-1),
                Notes = "Start with largest balance and oldest invoices."
            },
            new ActionItem
            {
                TenantId = tenant.Id,
                PeriodEnd = periodEnd,
                Title = "Reduce slow-mover DOH by 10 days",
                Description = "Identify top 20 slow movers; run promo/return plan; stop replenishment.",
                Owner = "Inventory Lead",
                DueDate = periodEnd.AddDays(10),
                Status = ActionStatuses.NotStarted,
                Priority = 30,
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-2)
            });

        db.DriverValues.AddRange(
            new DriverValue
            {
                TenantId = tenant.Id,
                PeriodEnd = periodEnd,
                PillarCode = "AR_PastDue31p%",
                DriverCode = "ar_acme",
                DriverName = "Acme Corp",
                Current = 0.21m,
                WeekOverWeekDelta = 0.02m,
                Context = "Dispute queue spike",
                Rank = 1,
                Status = "RED",
                WhyItMatters = "Top overdue contributor; quick win if resolved.",
                Owner = "AR Manager",
                AssignedSummary = "AR",
                TargetSummary = "< 0.15",
                CurrentSummary = "0.21",
                FixProgressPercent = 35
            },
            new DriverValue
            {
                TenantId = tenant.Id,
                PeriodEnd = periodEnd,
                PillarCode = "GrossMargin%",
                DriverCode = "gm_freight",
                DriverName = "Freight Costs",
                Current = 0.012m,
                WeekOverWeekDelta = 0.002m,
                Context = "Carrier surcharge increase",
                Rank = 1,
                Status = "YELLOW",
                WhyItMatters = "Freight creep is eroding margin; renegotiate lanes.",
                Owner = "Supply Chain",
                AssignedSummary = "SC",
                TargetSummary = "< 0.010",
                CurrentSummary = "0.012",
                FixProgressPercent = 50
            });

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded demo dashboard data for {ClientId} ({PeriodEnd}).", clientId, periodEnd);
    }

    private static DateOnly GetMostRecentSundayUtc()
    {
        var today = DateTime.UtcNow.Date;
        var delta = (int)today.DayOfWeek; // Sunday = 0
        var sunday = today.AddDays(-delta);
        return DateOnly.FromDateTime(sunday);
    }
}

