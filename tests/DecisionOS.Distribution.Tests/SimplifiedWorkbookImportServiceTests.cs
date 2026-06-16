using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure;
using DecisionOS.Distribution.Infrastructure.Workbooks;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Tests;

public class SimplifiedWorkbookImportServiceTests
{
    private static string? ResolveFixture()
    {
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Steves_Bowling_Supply_DPOS_Distribution_Test_Data.xlsx");
        if (File.Exists(fixture)) return fixture;
        var alt = @"c:\Users\emran\Downloads\Steves_Bowling_Supply_DPOS_Distribution_Test_Data.xlsx";
        return File.Exists(alt) ? alt : null;
    }

    private static DecisionOsDbContext MakeDb(string name)
    {
        var options = new DbContextOptionsBuilder<DecisionOsDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new DecisionOsDbContext(options);
    }

    private static void SeedKpis(DecisionOsDbContext db)
    {
        db.KpiDefinitions.AddRange(
            new KpiDefinition { Code = "GrossMargin%", Name = "GM", Unit = "pct", Direction = KpiDirection.HigherIsBetter, Target = 0.29m, AmberThreshold = 0.27m, RedThreshold = 0.25m, AlertPriority = 40, RecommendedAction = "A", DiagnosticChecks = "D" },
            new KpiDefinition { Code = "AR_PastDue31p%", Name = "AR", Unit = "pct", Direction = KpiDirection.LowerIsBetter, Target = 0.15m, AmberThreshold = 0.18m, RedThreshold = 0.22m, AlertPriority = 20, RecommendedAction = "A", DiagnosticChecks = "D" },
            new KpiDefinition { Code = "AP_PastDue31p%", Name = "AP", Unit = "pct", Direction = KpiDirection.LowerIsBetter, Target = 0.12m, AmberThreshold = 0.15m, RedThreshold = 0.18m, AlertPriority = 60, RecommendedAction = "A", DiagnosticChecks = "D" },
            new KpiDefinition { Code = "DOH", Name = "DOH", Unit = "days", Direction = KpiDirection.LowerIsBetter, Target = 45m, AmberThreshold = 55m, RedThreshold = 70m, AlertPriority = 50, RecommendedAction = "A", DiagnosticChecks = "D" },
            new KpiDefinition { Code = "CCC", Name = "CCC", Unit = "days", Direction = KpiDirection.LowerIsBetter, Target = 45m, AmberThreshold = 55m, RedThreshold = 70m, AlertPriority = 10, RecommendedAction = "A", DiagnosticChecks = "D" },
            new KpiDefinition { Code = "NetProfit%", Name = "NP", Unit = "pct", Direction = KpiDirection.HigherIsBetter, Target = 0.06m, AmberThreshold = 0.045m, RedThreshold = 0.03m, AlertPriority = 30, RecommendedAction = "A", DiagnosticChecks = "D" },
            new KpiDefinition { Code = "PerfectOrderRate", Name = "POR", Unit = "pct", Direction = KpiDirection.HigherIsBetter, Target = 0.93m, AmberThreshold = 0.91m, RedThreshold = 0.89m, AlertPriority = 70, RecommendedAction = "A", DiagnosticChecks = "D" }
        );
    }

    [Fact]
    public async Task RunSimplifiedImport_SteveWorkbook_CreatesMultiWeekSnapshots()
    {
        var path = ResolveFixture();
        if (path is null) return;

        var root = Path.Combine(Path.GetTempPath(), "decisionos-simplified", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        await using var db = MakeDb(nameof(RunSimplifiedImport_SteveWorkbook_CreatesMultiWeekSnapshots));
        var tenant = new Tenant { Id = Guid.NewGuid(), ClientId = "DIST-STEVE", Name = "Steve Test" };
        db.Tenants.Add(tenant);
        SeedKpis(db);

        var anchor = new DateOnly(2025, 11, 22);
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

        var analyzer = new WorkbookAnalyzer();
        var scoring = new WeeklyScoringService(db, new KpiStatusService(), new AlertService(), new WeeklyFocusService());
        var sut = new SimplifiedWorkbookImportService(db, analyzer, scoring);

        var bytes = await File.ReadAllBytesAsync(path);
        await sut.DetectAndPersistAsync(batch.Id, bytes, Path.GetFileName(path), root);
        await sut.ValidateSimplifiedAsync(batch.Id);
        await sut.RunSimplifiedImportAsync(batch.Id, root);

        var snapshots = await db.KpiSnapshots
            .Where(s => s.TenantId == tenant.Id)
            .ToListAsync();

        Assert.True(snapshots.Count >= 20 * 5); // ~26 weeks × ~7 KPIs
        Assert.Contains(snapshots, s => s.PeriodEnd >= anchor && s.Status != "GRAY");

        var drivers = await db.DriverValues.Where(d => d.TenantId == tenant.Id).ToListAsync();
        Assert.True(drivers.Count >= 3);
        var driverWeeks = drivers.Select(d => d.PeriodEnd).Distinct().Count();
        Assert.True(driverWeeks >= 10, "Holdovers should be copied to each imported KPI week.");

        var imported = await db.UploadBatches.FirstAsync(b => b.Id == batch.Id);
        Assert.Equal(UploadBatchStatuses.Imported, imported.Status);
    }

    [Fact]
    public async Task RunSimplifiedImport_SteveWorkbook_DerivesSixKpis_NetProfitGray()
    {
        var path = ResolveFixture();
        if (path is null) return;

        var root = Path.Combine(Path.GetTempPath(), "decisionos-simplified", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        await using var db = MakeDb(nameof(RunSimplifiedImport_SteveWorkbook_DerivesSixKpis_NetProfitGray));
        var tenant = new Tenant { Id = Guid.NewGuid(), ClientId = "DIST-STEVE", Name = "Steve Test" };
        db.Tenants.Add(tenant);
        SeedKpis(db);

        var anchor = new DateOnly(2025, 11, 22);
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

        var analyzer = new WorkbookAnalyzer();
        var scoring = new WeeklyScoringService(db, new KpiStatusService(), new AlertService(), new WeeklyFocusService());
        var sut = new SimplifiedWorkbookImportService(db, analyzer, scoring);

        var bytes = await File.ReadAllBytesAsync(path);
        await sut.DetectAndPersistAsync(batch.Id, bytes, Path.GetFileName(path), root);
        await sut.ValidateSimplifiedAsync(batch.Id);
        await sut.RunSimplifiedImportAsync(batch.Id, root);

        var weekSnapshots = await db.KpiSnapshots
            .Include(s => s.KpiDefinition)
            .Where(s => s.TenantId == tenant.Id && s.PeriodEnd == anchor)
            .ToListAsync();

        Assert.Equal(7, weekSnapshots.Count);

        var byCode = weekSnapshots.ToDictionary(s => s.KpiDefinition.Code, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("GRAY", byCode["NetProfit%"].Status);
        Assert.NotEqual("GRAY", byCode["GrossMargin%"].Status);
        Assert.NotEqual("GRAY", byCode["AR_PastDue31p%"].Status);
        Assert.NotEqual("GRAY", byCode["AP_PastDue31p%"].Status);
        Assert.NotEqual("GRAY", byCode["DOH"].Status);
        Assert.NotEqual("GRAY", byCode["CCC"].Status);
        Assert.NotEqual("GRAY", byCode["PerfectOrderRate"].Status);
    }
}
