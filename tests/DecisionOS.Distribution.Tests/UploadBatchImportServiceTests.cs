using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Tests;

public class UploadBatchImportServiceTests
{
    private static DecisionOsDbContext MakeDb(string name)
    {
        var options = new DbContextOptionsBuilder<DecisionOsDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new DecisionOsDbContext(options);
    }

    private static void SeedKpiDefinitions(DecisionOsDbContext db)
    {
        db.KpiDefinitions.AddRange(
            new KpiDefinition { Code = "GrossMargin%", Name = "Gross Margin %", Unit = "pct", Direction = KpiDirection.HigherIsBetter, Target = 0.28m, AmberThreshold = 0.265m, RedThreshold = 0.25m, AlertPriority = 40, RecommendedAction = "A", DiagnosticChecks = "D" },
            new KpiDefinition { Code = "AR_PastDue31p%", Name = "AR Health", Unit = "pct", Direction = KpiDirection.LowerIsBetter, Target = 0.12m, AmberThreshold = 0.15m, RedThreshold = 0.20m, AlertPriority = 20, RecommendedAction = "A", DiagnosticChecks = "D" },
            new KpiDefinition { Code = "AP_PastDue31p%", Name = "AP Health", Unit = "pct", Direction = KpiDirection.LowerIsBetter, Target = 0.10m, AmberThreshold = 0.12m, RedThreshold = 0.18m, AlertPriority = 60, RecommendedAction = "A", DiagnosticChecks = "D" },
            new KpiDefinition { Code = "DOH", Name = "DOH", Unit = "days", Direction = KpiDirection.LowerIsBetter, Target = 45m, AmberThreshold = 55m, RedThreshold = 70m, AlertPriority = 50, RecommendedAction = "A", DiagnosticChecks = "D" },
            new KpiDefinition { Code = "CCC", Name = "CCC", Unit = "days", Direction = KpiDirection.LowerIsBetter, Target = 45m, AmberThreshold = 55m, RedThreshold = 70m, AlertPriority = 10, RecommendedAction = "A", DiagnosticChecks = "D" },
            new KpiDefinition { Code = "NetProfit%", Name = "Net Profit %", Unit = "pct", Direction = KpiDirection.HigherIsBetter, Target = 0.06m, AmberThreshold = 0.045m, RedThreshold = 0.03m, AlertPriority = 30, RecommendedAction = "A", DiagnosticChecks = "D" },
            new KpiDefinition { Code = "PerfectOrderRate", Name = "Perfect Order", Unit = "pct", Direction = KpiDirection.HigherIsBetter, Target = 0.93m, AmberThreshold = 0.91m, RedThreshold = 0.89m, AlertPriority = 70, RecommendedAction = "A", DiagnosticChecks = "D" }
        );
    }

    [Fact]
    public async Task ValidateAndRunImport_NormalizesAndCreatesGrayForMissingKpis()
    {
        var root = Path.Combine(Path.GetTempPath(), "decisionos-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        await using var db = MakeDb(nameof(ValidateAndRunImport_NormalizesAndCreatesGrayForMissingKpis));
        var tenant = new Tenant { Id = Guid.NewGuid(), ClientId = "T1", Name = "Tenant 1" };
        db.Tenants.Add(tenant);
        SeedKpiDefinitions(db);

        var batch = new UploadBatch
        {
            TenantId = tenant.Id,
            PeriodEnd = new DateOnly(2026, 4, 28),
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.UploadBatches.Add(batch);
        await db.SaveChangesAsync();

        UploadedFile AddCsv(ReportType type, string fileName, string content)
        {
            var full = Path.Combine(root, "App_Data", "uploads", tenant.ClientId, batch.PeriodEnd.ToString("yyyy-MM-dd"), fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content);
            var rel = Path.GetRelativePath(root, full).Replace('\\', '/');
            var uf = new UploadedFile
            {
                UploadBatchId = batch.Id,
                ReportType = type,
                OriginalFileName = fileName,
                StoredFileName = fileName,
                StoredRelativePath = rel,
                Sha256Hex = UploadedFile.ComputeSha256Hex(File.ReadAllBytes(full)),
                HeaderRowNumber = 1,
                UploadedAt = DateTimeOffset.UtcNow
            };
            db.UploadedFiles.Add(uf);
            db.SaveChanges();
            return uf;
        }

        var sales = AddCsv(ReportType.Sales, "sales.csv",
            "Transaction_Date,Quantity_Sold,Net_Sales\n" +
            "2026-04-28,2,100\n"); // no COGS => GM% should be GRAY, but readiness ok (COGS is preferred)

        var inv = AddCsv(ReportType.Inventory, "inv.csv",
            "Snapshot_Date,SKU_ID,Quantity_On_Hand,Inventory_Value\n" +
            "2026-04-28,SKU1,10,500\n");

        var ar = AddCsv(ReportType.AccountsReceivable, "ar.csv",
            "AR_Snapshot_Date,Customer_ID,Customer_Name,Open_Balance,Days_Past_Due\n" +
            "2026-04-28,C1,Customer1,100,40\n");

        var ap = AddCsv(ReportType.AccountsPayable, "ap.csv",
            "AP_Snapshot_Date,Vendor_ID,Vendor_Name,Open_Balance,Days_Past_Due\n" +
            "2026-04-28,V1,Vendor1,200,0\n");

        // Mappings
        db.UploadedFileColumnMaps.AddRange(
            new UploadedFileColumnMap { UploadedFileId = sales.Id, SourceColumn = "Transaction_Date", SystemField = "Transaction_Date" },
            new UploadedFileColumnMap { UploadedFileId = sales.Id, SourceColumn = "Quantity_Sold", SystemField = "Quantity_Sold" },
            new UploadedFileColumnMap { UploadedFileId = sales.Id, SourceColumn = "Net_Sales", SystemField = "Net_Sales" },

            new UploadedFileColumnMap { UploadedFileId = inv.Id, SourceColumn = "Snapshot_Date", SystemField = "Snapshot_Date" },
            new UploadedFileColumnMap { UploadedFileId = inv.Id, SourceColumn = "SKU_ID", SystemField = "SKU_ID" },
            new UploadedFileColumnMap { UploadedFileId = inv.Id, SourceColumn = "Quantity_On_Hand", SystemField = "Quantity_On_Hand" },
            new UploadedFileColumnMap { UploadedFileId = inv.Id, SourceColumn = "Inventory_Value", SystemField = "Inventory_Value" },

            new UploadedFileColumnMap { UploadedFileId = ar.Id, SourceColumn = "AR_Snapshot_Date", SystemField = "AR_Snapshot_Date" },
            new UploadedFileColumnMap { UploadedFileId = ar.Id, SourceColumn = "Customer_ID", SystemField = "Customer_ID" },
            new UploadedFileColumnMap { UploadedFileId = ar.Id, SourceColumn = "Customer_Name", SystemField = "Customer_Name" },
            new UploadedFileColumnMap { UploadedFileId = ar.Id, SourceColumn = "Open_Balance", SystemField = "Open_Balance" },
            new UploadedFileColumnMap { UploadedFileId = ar.Id, SourceColumn = "Days_Past_Due", SystemField = "Days_Past_Due" },

            new UploadedFileColumnMap { UploadedFileId = ap.Id, SourceColumn = "AP_Snapshot_Date", SystemField = "AP_Snapshot_Date" },
            new UploadedFileColumnMap { UploadedFileId = ap.Id, SourceColumn = "Vendor_ID", SystemField = "Vendor_ID" },
            new UploadedFileColumnMap { UploadedFileId = ap.Id, SourceColumn = "Vendor_Name", SystemField = "Vendor_Name" },
            new UploadedFileColumnMap { UploadedFileId = ap.Id, SourceColumn = "Open_Balance", SystemField = "Open_Balance" },
            new UploadedFileColumnMap { UploadedFileId = ap.Id, SourceColumn = "Days_Past_Due", SystemField = "Days_Past_Due" }
        );
        await db.SaveChangesAsync();

        var sut = new UploadBatchImportService(db, new KpiStatusService(), new AlertService(), new WeeklyFocusService());

        await sut.ValidateAsync(batch.Id, root);
        var loadedBatch = await db.UploadBatches.FirstAsync(b => b.Id == batch.Id);
        Assert.Equal("ReadyWithLimitations", loadedBatch.ReadinessStatus); // COGS missing => preferred-field warning

        await sut.RunImportAsync(batch.Id, root);

        Assert.True(await db.NormalizedSalesRows.AnyAsync(r => r.UploadBatchId == batch.Id));
        Assert.True(await db.NormalizedInventoryRows.AnyAsync(r => r.UploadBatchId == batch.Id));
        Assert.True(await db.NormalizedArRows.AnyAsync(r => r.UploadBatchId == batch.Id));
        Assert.True(await db.NormalizedApRows.AnyAsync(r => r.UploadBatchId == batch.Id));

        var snaps = await db.KpiSnapshots.Include(s => s.KpiDefinition)
            .Where(s => s.TenantId == tenant.Id && s.PeriodEnd == batch.PeriodEnd)
            .ToListAsync();

        Assert.Contains(snaps, s => s.KpiDefinition.Code == "GrossMargin%" && s.Status == "GRAY");
        Assert.Contains(snaps, s => s.KpiDefinition.Code == "NetProfit%" && s.Status == "GRAY");
        Assert.Contains(snaps, s => s.KpiDefinition.Code == "PerfectOrderRate" && s.Status == "GRAY");

        var imported = await db.UploadBatches.FirstAsync(b => b.Id == batch.Id);
        Assert.Equal(UploadBatchStatuses.Imported, imported.Status);
        Assert.NotNull(imported.ImportRunId);
    }
}

