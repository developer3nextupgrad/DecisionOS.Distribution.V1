using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Normalized;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Tests;

public class DashboardContextServiceTests
{
    private static DecisionOsDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<DecisionOsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DecisionOsDbContext(opts);
    }

    [Fact]
    public async Task GetCustomersAsync_UnionsSalesAndAr()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        db.NormalizedSalesRows.Add(new NormalizedSalesRow
        {
            TenantId = tenantId,
            PeriodEnd = new DateOnly(2026, 1, 10),
            UploadBatchId = 1,
            UploadedFileId = 1,
            SourceRowNumber = 1,
            Status = RowStatus.Valid,
            RawJson = "{}",
            CustomerId = "C1",
            CustomerName = "Acme"
        });
        db.NormalizedArRows.Add(new NormalizedArRow
        {
            TenantId = tenantId,
            PeriodEnd = new DateOnly(2026, 1, 10),
            UploadBatchId = 1,
            UploadedFileId = 1,
            SourceRowNumber = 1,
            Status = RowStatus.Valid,
            RawJson = "{}",
            CustomerId = "C2",
            CustomerName = "Beta"
        });
        await db.SaveChangesAsync();

        var sut = new DashboardContextService(db);
        var customers = await sut.GetCustomersAsync(tenantId);

        Assert.Equal(2, customers.Count);
        Assert.Contains(customers, c => c.CustomerId == "C1");
        Assert.Contains(customers, c => c.CustomerId == "C2");
    }

    [Fact]
    public async Task GetWeeksAsync_ForCustomer_UsesStagingPeriods()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        db.NormalizedSalesRows.Add(new NormalizedSalesRow
        {
            TenantId = tenantId,
            PeriodEnd = new DateOnly(2026, 2, 7),
            UploadBatchId = 1,
            UploadedFileId = 1,
            SourceRowNumber = 1,
            Status = RowStatus.Valid,
            RawJson = "{}",
            CustomerId = "C1"
        });
        db.KpiSnapshots.Add(new KpiSnapshot
        {
            TenantId = tenantId,
            PeriodEnd = new DateOnly(2026, 1, 31),
            KpiDefinitionId = 1,
            Value = 1m,
            Status = "GREEN"
        });
        await db.SaveChangesAsync();

        var sut = new DashboardContextService(db);
        var weeks = await sut.GetWeeksAsync(tenantId, "C1");

        Assert.Single(weeks);
        Assert.Equal(new DateOnly(2026, 2, 7), weeks[0]);
    }
}
