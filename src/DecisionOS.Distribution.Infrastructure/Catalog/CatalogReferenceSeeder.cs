using DecisionOS.Distribution.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DecisionOS.Distribution.Infrastructure.Catalog;

/// <summary>Seeds the 24-KPI reference catalog when the database has no catalog rows yet.</summary>
public static class CatalogReferenceSeeder
{
    private static readonly (string Id, string Name, string? Legacy, bool Mgmt)[] SeedKpis =
    [
        ("KPI-001", "Gross Margin %", "GrossMargin%", true),
        ("KPI-002", "A/R Health", "AR_PastDue31p%", true),
        ("KPI-003", "A/P & Purchasing Efficiency", "AP_PastDue31p%", true),
        ("KPI-004", "Inventory Health (DOH)", "DOH", true),
        ("KPI-005", "Cash Conversion Cycle (CCC)", "CCC", true),
        ("KPI-006", "Net Profit %", "NetProfit%", true),
        ("KPI-007", "Service / Fulfillment (Perfect Order)", "PerfectOrderRate", true),
        ("KPI-008", "Revenue Growth %", null, true),
        ("KPI-009", "Customer Retention %", null, true),
        ("KPI-010", "Order Fill Rate %", null, true),
        ("KPI-011", "On-Time Delivery %", null, true),
        ("KPI-012", "Vendor Fill Rate %", null, true),
        ("KPI-013", "Freight Cost % of Sales", null, true),
        ("KPI-014", "Discount Leakage %", null, true),
        ("KPI-015", "Slow-Mover Inventory %", null, true),
        ("KPI-016", "Stockout Rate %", null, true),
        ("KPI-017", "Customer Concentration %", null, true),
        ("KPI-018", "Bad Debt % of AR", null, true),
        ("KPI-019", "Operating Expense %", null, true),
        ("KPI-020", "Labor Cost % of Sales", null, true),
        ("KPI-021", "Warehouse Productivity", null, true),
        ("KPI-022", "Return Rate %", null, true),
        ("KPI-023", "Purchase Price Variance %", null, true),
        ("KPI-024", "Cash on Hand Days", null, true)
    ];

    public static async Task SeedIfEmptyAsync(
        DecisionOsDbContext db,
        ICatalogKpiDefinitionSyncService syncService,
        ILogger logger,
        CancellationToken ct = default)
    {
        if (await db.CatalogKpis.AnyAsync(ct))
            return;

        foreach (var (id, name, legacy, mgmt) in SeedKpis)
        {
            db.CatalogKpis.Add(new CatalogKpi
            {
                KpiId = id,
                Name = name,
                LegacyCode = legacy,
                MgmtLayerCandidate = mgmt,
                Definition = $"Catalog measure: {name}.",
                Category = legacy is not null ? "Financial" : "Operational",
                Cadence = "Weekly",
                PrimaryDataNeeds = legacy is not null
                    ? "Provided by standard weekly upload tabs."
                    : "Requires additional upload fields — see KPI catalog.",
                DefaultStatusModel = "R/Y/G"
            });
        }

        if (!await db.CatalogScoreComponents.AnyAsync(ct))
        {
            db.CatalogScoreComponents.AddRange(
                new CatalogScoreComponent { Component = "Severity", WeightPercent = 30m, RequirementLevel = "Required" },
                new CatalogScoreComponent { Component = "Cash", WeightPercent = 20m, RequirementLevel = "Required" },
                new CatalogScoreComponent { Component = "Financial", WeightPercent = 20m, RequirementLevel = "Required" },
                new CatalogScoreComponent { Component = "Urgency", WeightPercent = 15m, RequirementLevel = "Required" },
                new CatalogScoreComponent { Component = "Actionability", WeightPercent = 10m, RequirementLevel = "Required" },
                new CatalogScoreComponent { Component = "Confidence", WeightPercent = 5m, RequirementLevel = "Required" }
            );
        }

        await db.SaveChangesAsync(ct);
        await syncService.SyncGlobalDefinitionsAsync(ct);
        logger.LogInformation("Seeded {Count} catalog KPI reference rows.", SeedKpis.Length);
    }
}
