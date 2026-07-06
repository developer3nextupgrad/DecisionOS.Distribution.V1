using DecisionOS.Distribution.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DecisionOS.Distribution.Infrastructure;

public static class ReferenceDataSeeder
{
    public static async Task SeedDefaultsIfNeededAsync(
        DecisionOsDbContext db,
        ILogger logger,
        CancellationToken ct = default)
    {
        await SeedVerticalLibrariesIfNeeded(db, logger, ct);
        await SeedBusinessProfilesIfNeeded(db, logger, ct);
        await SeedKpiDefinitionsIfNeeded(db, logger, ct);
        await SeedDriverDefinitionsIfNeeded(db, logger, ct);
    }

    private static async Task SeedVerticalLibrariesIfNeeded(DecisionOsDbContext db, ILogger logger, CancellationToken ct)
    {
        if (await db.VerticalLibraries.AnyAsync(ct)) return;

        db.VerticalLibraries.Add(new VerticalLibrary
        {
            Id = Guid.NewGuid(),
            Code = "DISTRIBUTION",
            Name = "Distribution",
            Description = "Distribution vertical library (broad business family)."
        });

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded VerticalLibraries defaults.");
    }

    private static async Task SeedBusinessProfilesIfNeeded(DecisionOsDbContext db, ILogger logger, CancellationToken ct)
    {
        if (await db.BusinessProfiles.AnyAsync(ct)) return;

        var distributionId = await db.VerticalLibraries
            .Where(v => v.Code == "DISTRIBUTION")
            .Select(v => v.Id)
            .FirstOrDefaultAsync(ct);

        if (distributionId == Guid.Empty)
            distributionId = Guid.NewGuid();

        db.BusinessProfiles.Add(new BusinessProfile
        {
            Id = Guid.NewGuid(),
            VerticalLibraryId = distributionId,
            Code = "DISTRIBUTION_DEFAULT",
            Name = "Distribution (Default)",
            Description = "Default pilot KPI/driver standards for distribution-style businesses.",
            ActiveKpiProfileCode = "PILOT_7",
            LocationStructure = "single-location",
            ChannelStructure = "internal-external",
            ThresholdProfileCode = "PILOT_DEFAULT"
        });

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded BusinessProfiles defaults.");
    }

    private static async Task SeedKpiDefinitionsIfNeeded(DecisionOsDbContext db, ILogger logger, CancellationToken ct)
    {
        if (await db.KpiDefinitions.AnyAsync(ct)) return;

        var defs = new[]
        {
            new KpiDefinition
            {
                Code = "CCC",
                Name = "Cash Conversion Cycle (CCC)",
                Unit = "days",
                Direction = KpiDirection.LowerIsBetter,
                Target = 45m,
                AmberThreshold = 55m,
                RedThreshold = 70m,
                AlertPriority = 10,
                RecommendedAction = "Tighten cash cycle: accelerate collections, reduce slow inventory, and protect payables terms.",
                DiagnosticChecks = OwnerLanguage.CccDiagnosticChecks
            },
            new KpiDefinition
            {
                Code = "GrossMargin%",
                Name = "Gross Margin %",
                Unit = "pct",
                Direction = KpiDirection.HigherIsBetter,
                Target = 0.28m,
                AmberThreshold = 0.265m,
                RedThreshold = 0.25m,
                AlertPriority = 40,
                RecommendedAction = "Protect margin: adjust pricing/discounts, attack freight-in/COGS leakage, and fix low-margin customers/SKUs.",
                DiagnosticChecks = "Review margin by customer, SKU, channel; look for discount creep and supplier cost changes."
            },
            new KpiDefinition
            {
                Code = "NetProfit%",
                Name = "Net Profit %",
                Unit = "pct",
                Direction = KpiDirection.HigherIsBetter,
                Target = 0.06m,
                AmberThreshold = 0.045m,
                RedThreshold = 0.03m,
                AlertPriority = 30,
                RecommendedAction = "Stabilize profit: control Opex, fix margin leakage, and prioritize high-contribution work.",
                DiagnosticChecks = "Decompose: GM$ and Opex; identify the top 2 expense overruns and top margin leaks."
            },
            new KpiDefinition
            {
                Code = "AR_PastDue31p%",
                Name = "A/R Health",
                Unit = "pct",
                Direction = KpiDirection.LowerIsBetter,
                Target = 0.12m,
                AmberThreshold = 0.15m,
                RedThreshold = 0.20m,
                AlertPriority = 20,
                RecommendedAction = "Stop overdue growth: implement collections cadence, tighten credit holds, and resolve dispute queues.",
                DiagnosticChecks = "Look at 31–60 and 90+ buckets; top 10 past-due accounts; dispute/short-pay reasons."
            },
            new KpiDefinition
            {
                Code = "DOH",
                Name = "Inventory Health (DOH)",
                Unit = "days",
                Direction = KpiDirection.LowerIsBetter,
                Target = 45m,
                AmberThreshold = 55m,
                RedThreshold = 70m,
                AlertPriority = 50,
                RecommendedAction = "Balance inventory: reduce excess (slow movers) and eliminate stockouts on top sellers.",
                DiagnosticChecks = "Check DOH by category; stockouts list; excess value; inbound lead-time variability."
            },
            new KpiDefinition
            {
                Code = "AP_PastDue31p%",
                Name = "A/P & Purchasing Efficiency",
                Unit = "pct",
                Direction = KpiDirection.LowerIsBetter,
                Target = 0.10m,
                AmberThreshold = 0.12m,
                RedThreshold = 0.18m,
                AlertPriority = 60,
                RecommendedAction = "Protect vendor relationships and cash: schedule payments, negotiate terms, and prevent 60+ drift.",
                DiagnosticChecks = "Check AP 31+ %, 60+ $, missed discounts, vendor holds, and upcoming large invoices."
            },
            new KpiDefinition
            {
                Code = "PerfectOrderRate",
                Name = "Service / Fulfillment (Perfect Order)",
                Unit = "pct",
                Direction = KpiDirection.HigherIsBetter,
                Target = 0.93m,
                AmberThreshold = 0.91m,
                RedThreshold = 0.89m,
                AlertPriority = 70,
                RecommendedAction = "Run service recovery: fix the biggest service driver (late, short, or damaged) and stabilize throughput.",
                DiagnosticChecks = "Decompose: On-time %, Fill rate %, Damage rate; identify top WH/carrier/SKU exceptions."
            }
        };

        db.KpiDefinitions.AddRange(defs);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded KpiDefinitions defaults.");
    }

    private static async Task SeedDriverDefinitionsIfNeeded(DecisionOsDbContext db, ILogger logger, CancellationToken ct)
    {
        if (await db.DriverDefinitions.AnyAsync(ct)) return;

        IEnumerable<DriverDefinition> defs = new[]
        {
            new DriverDefinition { PillarCode = "AR_PastDue31p%", DriverCode = "ar_acme", DisplayName = "Acme Corp", SortOrder = 10 },
            new DriverDefinition { PillarCode = "AR_PastDue31p%", DriverCode = "ar_beta", DisplayName = "Beta Industries", SortOrder = 20 },
            new DriverDefinition { PillarCode = "AR_PastDue31p%", DriverCode = "ar_gamma", DisplayName = "Gamma LLC", SortOrder = 30 },
            new DriverDefinition { PillarCode = "AR_PastDue31p%", DriverCode = "ar_delta", DisplayName = "Delta Supply", SortOrder = 40 },
            new DriverDefinition { PillarCode = "AR_PastDue31p%", DriverCode = "ar_epsilon", DisplayName = "Epsilon Co", SortOrder = 50 },
            new DriverDefinition { PillarCode = "CCC", DriverCode = "ccc_dso", DisplayName = "Days Sales Outstanding", SortOrder = 10 },
            new DriverDefinition { PillarCode = "CCC", DriverCode = "ccc_dio", DisplayName = "Days Inventory Outstanding", SortOrder = 20 },
            new DriverDefinition { PillarCode = "CCC", DriverCode = "ccc_dpo", DisplayName = "Days Payable Outstanding", SortOrder = 30 },
            new DriverDefinition { PillarCode = "GrossMargin%", DriverCode = "gm_mix", DisplayName = "Product Mix", SortOrder = 10 },
            new DriverDefinition { PillarCode = "GrossMargin%", DriverCode = "gm_freight", DisplayName = "Freight Costs", SortOrder = 20 },
            new DriverDefinition { PillarCode = "GrossMargin%", DriverCode = "gm_discount", DisplayName = "Discounting", SortOrder = 30 },
            new DriverDefinition { PillarCode = "PerfectOrderRate", DriverCode = "por_late", DisplayName = "Late Shipments", SortOrder = 10 },
            new DriverDefinition { PillarCode = "DOH", DriverCode = "doh_slow", DisplayName = "Industrial slow movers", SortOrder = 10 }
        };

        db.DriverDefinitions.AddRange(defs);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded DriverDefinitions defaults.");
    }
}

