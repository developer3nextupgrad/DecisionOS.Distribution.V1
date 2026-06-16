using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Tests;

public class DashboardPartialDataTests
{
    private static DecisionOsDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<DecisionOsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DecisionOsDbContext(opts);
    }

    [Fact]
    public async Task GetWeeksAsync_AllBuyers_UsesSnapshotWeeksEvenWhenSomeKpisGray()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var period = new DateOnly(2026, 2, 7);

        db.KpiSnapshots.AddRange(
            new KpiSnapshot { TenantId = tenantId, PeriodEnd = period, KpiDefinitionId = 1, Value = 0.28m, Status = "GREEN" },
            new KpiSnapshot { TenantId = tenantId, PeriodEnd = period, KpiDefinitionId = 2, Value = 0m, Status = "GRAY" }
        );
        await db.SaveChangesAsync();

        var sut = new DashboardContextService(db);
        var weeks = await sut.GetWeeksAsync(tenantId, null);

        Assert.Single(weeks);
        Assert.Equal(period, weeks[0]);
    }
}
