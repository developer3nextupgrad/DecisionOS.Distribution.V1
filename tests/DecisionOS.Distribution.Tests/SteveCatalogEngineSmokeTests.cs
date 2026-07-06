using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Scoring;
using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure;
using DecisionOS.Distribution.Infrastructure.Catalog;
using DecisionOS.Distribution.Infrastructure.Routing;
using DecisionOS.Distribution.Infrastructure.Scoring;
using DecisionOS.Distribution.Infrastructure.Scoring.Calculators;
using DecisionOS.Distribution.Infrastructure.Workbooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DecisionOS.Distribution.Tests;

public class SteveCatalogEngineSmokeTests
{
    private static string? ResolveFixture()
    {
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Steves_Bowling_Supply_DPOS_Distribution_Test_Data.xlsx");
        if (File.Exists(fixture)) return fixture;
        var alt = @"c:\Users\emran\Downloads\Re_ bulk excel Import\Steves_Bowling_Supply_DPOS_Distribution_Test_Data.xlsx";
        return File.Exists(alt) ? alt : null;
    }

    [Fact]
    public async Task SteveWorkbook_CatalogEnginePath_MatchesLegacyAnchorWeek()
    {
        var path = ResolveFixture();
        if (path is null) return;

        var anchor = new DateOnly(2025, 11, 22);
        var legacySnaps = await ImportAnchorWeekAsync(path, anchor, useCatalogEngine: false);
        var catalogSnaps = await ImportAnchorWeekAsync(path, anchor, useCatalogEngine: true);

        Assert.Equal(legacySnaps.Count, catalogSnaps.Count);
        foreach (var leg in legacySnaps)
        {
            var cat = catalogSnaps.First(s => s.KpiDefinition.Code == leg.KpiDefinition.Code);
            Assert.Equal(leg.Status, cat.Status);
            Assert.Equal(leg.Value, cat.Value);
        }
    }

    private static async Task<List<KpiSnapshot>> ImportAnchorWeekAsync(string path, DateOnly anchor, bool useCatalogEngine)
    {
        var dbName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<DecisionOsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        await using var db = new DecisionOsDbContext(options);

        var tenant = new Tenant { Id = Guid.NewGuid(), ClientId = $"STEVE-{useCatalogEngine}", Name = "Steve Catalog Smoke" };
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
            PeriodEnd = anchor,
            AnchorPeriodEnd = anchor,
            Cadence = UploadCadence.Weekly,
            ImportMode = UploadImportMode.Simplified,
            CreatedAt = DateTimeOffset.UtcNow,
            Status = UploadBatchStatuses.Draft
        };
        db.UploadBatches.Add(batch);
        await db.SaveChangesAsync();

        var featureOptions = Options.Create(new DecisionOsFeatureOptions
        {
            Catalog = new CatalogFeatureOptions { Enabled = useCatalogEngine },
            Scoring = new ScoringFeatureOptions
            {
                UseCatalogEngine = useCatalogEngine,
                UseDynamicTop7 = useCatalogEngine
            },
            Routing = new RoutingFeatureOptions { Enabled = useCatalogEngine }
        });

        IWeeklyScoringService scoring = useCatalogEngine
            ? new KpiCalculationOrchestrator(
                db,
                new KpiStatusService(),
                new AlertService(),
                new WeeklyFocusService(),
                new WeeklyScoringService(db, new KpiStatusService(), new AlertService(), new WeeklyFocusService()),
                new IKpiCalculator[]
                {
                    new GrossMarginKpiCalculator(db),
                    new ArPastDueKpiCalculator(db),
                    new ApPastDueKpiCalculator(db),
                    new DohKpiCalculator(db),
                    new CccKpiCalculator(db),
                    new NetProfitKpiCalculator(db),
                    new PerfectOrderRateKpiCalculator(db)
                },
                new PriorityRankingService(db, new KpiStatusService(), featureOptions),
                new DriverEvaluationService(db, new DriverRankingService(), featureOptions),
                new InfluencerEvidenceService(db, featureOptions),
                new ModuleRoutingService(db, featureOptions),
                new CatalogKpiDefinitionSyncService(db),
                featureOptions)
            : new WeeklyScoringService(db, new KpiStatusService(), new AlertService(), new WeeklyFocusService());

        var root = Path.Combine(Path.GetTempPath(), "decisionos-steve-smoke", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var analyzer = new WorkbookAnalyzer();
        var sut = new SimplifiedWorkbookImportService(db, analyzer, scoring);
        var bytes = await File.ReadAllBytesAsync(path);

        await sut.DetectAndPersistAsync(batch.Id, bytes, Path.GetFileName(path), root);
        await sut.ValidateSimplifiedAsync(batch.Id);
        await sut.RunSimplifiedImportAsync(batch.Id, root);

        return await db.KpiSnapshots
            .Include(s => s.KpiDefinition)
            .Where(s => s.TenantId == tenant.Id && s.PeriodEnd == anchor)
            .ToListAsync();
    }
}
