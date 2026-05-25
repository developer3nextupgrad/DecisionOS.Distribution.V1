using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure;
using DecisionOS.Distribution.Infrastructure.Workbooks;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Tests;

/// <summary>Integration test against local PostgreSQL (skipped when DB unavailable).</summary>
public class PostgresSimplifiedImportTests
{
    private static string? ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DecisionOs")
        ?? "Host=localhost;Port=5432;Database=decisionos;Username=postgres;Password=postgres";

    private static string? FixturePath
    {
        get
        {
            var p = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Steves_Bowling_Supply_DPOS_Distribution_Test_Data.xlsx");
            return File.Exists(p) ? p : null;
        }
    }

    private static async Task<DecisionOsDbContext?> TryConnectAsync()
    {
        var options = new DbContextOptionsBuilder<DecisionOsDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        var db = new DecisionOsDbContext(options);
        try
        {
            await db.Database.CanConnectAsync();
            return db;
        }
        catch
        {
            await db.DisposeAsync();
            return null;
        }
    }

    [Fact]
    public async Task Postgres_SimplifiedImport_SteveWorkbook_WritesSnapshots()
    {
        var path = FixturePath;
        if (path is null) return;

        await using var db = await TryConnectAsync();
        if (db is null) return;

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.ClientId == "DIST-001")
            ?? await db.Tenants.OrderBy(t => t.ClientId).FirstOrDefaultAsync();
        if (tenant is null) return;

        if (!await db.KpiDefinitions.AnyAsync())
            return;

        var root = Path.Combine(Path.GetTempPath(), "decisionos-pg-e2e", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

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

        var count = await db.KpiSnapshots.CountAsync(s => s.TenantId == tenant.Id && s.PeriodEnd >= anchor);
        Assert.True(count >= 20);

        var imported = await db.UploadBatches.AsNoTracking().FirstAsync(b => b.Id == batch.Id);
        Assert.Equal(UploadBatchStatuses.Imported, imported.Status);
        Assert.Equal(UploadImportMode.Simplified, imported.ImportMode);
    }
}
