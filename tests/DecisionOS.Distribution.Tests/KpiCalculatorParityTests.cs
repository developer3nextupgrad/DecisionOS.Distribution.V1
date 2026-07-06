using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Scoring;
using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure;
using DecisionOS.Distribution.Infrastructure.Catalog;
using DecisionOS.Distribution.Infrastructure.Scoring;
using DecisionOS.Distribution.Infrastructure.Scoring.Calculators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DecisionOS.Distribution.Tests;

public class KpiCalculatorParityTests
{
    private static DecisionOsDbContext MakeDb(string name)
    {
        var options = new DbContextOptionsBuilder<DecisionOsDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new DecisionOsDbContext(options);
    }

    private static void SeedTenantAndData(DecisionOsDbContext db, out Tenant tenant, out long batchId)
    {
        tenant = new Tenant { Id = Guid.NewGuid(), ClientId = "PARITY", Name = "Parity" };
        db.Tenants.Add(tenant);
        db.KpiDefinitions.AddRange(
            new KpiDefinition { Code = "GrossMargin%", Name = "GM", Unit = "pct", Direction = KpiDirection.HigherIsBetter, Target = 0.28m, AmberThreshold = 0.265m, RedThreshold = 0.25m, AlertPriority = 40, RecommendedAction = "A", DiagnosticChecks = "D" },
            new KpiDefinition { Code = "AR_PastDue31p%", Name = "AR", Unit = "pct", Direction = KpiDirection.LowerIsBetter, Target = 0.12m, AmberThreshold = 0.15m, RedThreshold = 0.20m, AlertPriority = 20, RecommendedAction = "A", DiagnosticChecks = "D" },
            new KpiDefinition { Code = "AP_PastDue31p%", Name = "AP", Unit = "pct", Direction = KpiDirection.LowerIsBetter, Target = 0.10m, AmberThreshold = 0.12m, RedThreshold = 0.18m, AlertPriority = 60, RecommendedAction = "A", DiagnosticChecks = "D" },
            new KpiDefinition { Code = "DOH", Name = "DOH", Unit = "days", Direction = KpiDirection.LowerIsBetter, Target = 45m, AmberThreshold = 55m, RedThreshold = 70m, AlertPriority = 50, RecommendedAction = "A", DiagnosticChecks = "D" },
            new KpiDefinition { Code = "CCC", Name = "CCC", Unit = "days", Direction = KpiDirection.LowerIsBetter, Target = 45m, AmberThreshold = 55m, RedThreshold = 70m, AlertPriority = 10, RecommendedAction = "A", DiagnosticChecks = "D" },
            new KpiDefinition { Code = "NetProfit%", Name = "NP", Unit = "pct", Direction = KpiDirection.HigherIsBetter, Target = 0.06m, AmberThreshold = 0.045m, RedThreshold = 0.03m, AlertPriority = 30, RecommendedAction = "A", DiagnosticChecks = "D" },
            new KpiDefinition { Code = "PerfectOrderRate", Name = "POR", Unit = "pct", Direction = KpiDirection.HigherIsBetter, Target = 0.93m, AmberThreshold = 0.91m, RedThreshold = 0.89m, AlertPriority = 70, RecommendedAction = "A", DiagnosticChecks = "D" }
        );
        var batch = new UploadBatch
        {
            TenantId = tenant.Id,
            PeriodEnd = new DateOnly(2025, 11, 22),
            CreatedAt = DateTimeOffset.UtcNow,
            Status = UploadBatchStatuses.Draft
        };
        db.UploadBatches.Add(batch);
        db.SaveChanges();
        batchId = batch.Id;

        var period = batch.PeriodEnd;
        db.NormalizedSalesRows.Add(new Domain.Normalized.NormalizedSalesRow
        {
            TenantId = tenant.Id, UploadBatchId = batchId, PeriodEnd = period,
            NetSales = 100000m, Cogs = 72000m, RawJson = "{}"
        });
        db.NormalizedArRows.Add(new Domain.Normalized.NormalizedArRow
        {
            TenantId = tenant.Id, UploadBatchId = batchId, PeriodEnd = period,
            OpenBalance = 50000m, DaysPastDue = 45, RawJson = "{}"
        });
        db.NormalizedApRows.Add(new Domain.Normalized.NormalizedApRow
        {
            TenantId = tenant.Id, UploadBatchId = batchId, PeriodEnd = period,
            OpenBalance = 30000m, DaysPastDue = 10, RawJson = "{}"
        });
        db.NormalizedInventoryRows.Add(new Domain.Normalized.NormalizedInventoryRow
        {
            TenantId = tenant.Id, UploadBatchId = batchId, PeriodEnd = period,
            InventoryValue = 40000m, RawJson = "{}"
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task LegacyAndOrchestratorPaths_ProduceSameSnapshotValues()
    {
        await using var dbLegacy = MakeDb(nameof(LegacyAndOrchestratorPaths_ProduceSameSnapshotValues) + "_legacy");
        await using var dbOrch = MakeDb(nameof(LegacyAndOrchestratorPaths_ProduceSameSnapshotValues) + "_orch");
        SeedTenantAndData(dbLegacy, out var tenantLegacy, out var batchLegacy);
        SeedTenantAndData(dbOrch, out var tenantOrch, out var batchOrch);

        var period = new DateOnly(2025, 11, 22);
        var requestLegacy = new WeeklyScoringRequest
        {
            TenantId = tenantLegacy.Id,
            PeriodEnd = period,
            UploadBatchId = batchLegacy,
            DirectKpiValues = new Dictionary<string, decimal?>
            {
                ["NetProfit%"] = 0.05m,
                ["PerfectOrderRate"] = 0.92m
            }
        };
        var requestOrch = new WeeklyScoringRequest
        {
            TenantId = tenantOrch.Id,
            PeriodEnd = period,
            UploadBatchId = batchOrch,
            DirectKpiValues = requestLegacy.DirectKpiValues
        };

        var legacy = new WeeklyScoringService(dbLegacy, new KpiStatusService(), new AlertService(), new WeeklyFocusService());
        await legacy.ScorePeriodAsync(requestLegacy);

        var options = Options.Create(new DecisionOsFeatureOptions
        {
            Scoring = new ScoringFeatureOptions { UseCatalogEngine = true }
        });
        var calculators = new IKpiCalculator[]
        {
            new GrossMarginKpiCalculator(dbOrch),
            new ArPastDueKpiCalculator(dbOrch),
            new ApPastDueKpiCalculator(dbOrch),
            new DohKpiCalculator(dbOrch),
            new CccKpiCalculator(dbOrch),
            new NetProfitKpiCalculator(dbOrch),
            new PerfectOrderRateKpiCalculator(dbOrch)
        };
        var orchestrator = new KpiCalculationOrchestrator(
            dbOrch,
            new KpiStatusService(),
            new AlertService(),
            new WeeklyFocusService(),
            new WeeklyScoringService(dbOrch, new KpiStatusService(), new AlertService(), new WeeklyFocusService()),
            calculators,
            new PriorityRankingService(dbOrch, new KpiStatusService(), options),
            new DriverEvaluationService(dbOrch, new DriverRankingService(), options),
            new InfluencerEvidenceService(dbOrch, options),
            new Infrastructure.Routing.ModuleRoutingService(dbOrch, options),
            new CatalogKpiDefinitionSyncService(dbOrch),
            options);

        await orchestrator.ScorePeriodAsync(requestOrch);

        var legacySnaps = await dbLegacy.KpiSnapshots.Include(s => s.KpiDefinition).ToListAsync();
        var orchSnaps = await dbOrch.KpiSnapshots.Include(s => s.KpiDefinition).ToListAsync();

        Assert.Equal(legacySnaps.Count, orchSnaps.Count);
        foreach (var leg in legacySnaps)
        {
            var match = orchSnaps.First(s => s.KpiDefinition.Code == leg.KpiDefinition.Code);
            Assert.Equal(leg.Status, match.Status);
            Assert.Equal(leg.Value, match.Value);
        }
    }
}
