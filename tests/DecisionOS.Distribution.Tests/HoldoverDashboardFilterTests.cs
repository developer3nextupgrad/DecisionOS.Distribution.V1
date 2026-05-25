using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Normalized;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Tests;

public class HoldoverDashboardFilterTests
{
    [Fact]
    public async Task BuyerFilter_KeepsTenantWideHoldoversWithNullDimension1()
    {
        var tenantId = Guid.NewGuid();
        await using var db = new DecisionOsDbContext(new DbContextOptionsBuilder<DecisionOsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var period = new DateOnly(2026, 4, 28);
        db.DriverValues.AddRange(
            new DriverValue
            {
                TenantId = tenantId,
                PeriodEnd = period,
                PillarCode = "AR",
                DriverName = "Fix aging process",
                Rank = 1,
                Status = "YELLOW",
                WhyItMatters = "x"
            },
            new DriverValue
            {
                TenantId = tenantId,
                PeriodEnd = period,
                PillarCode = "AR",
                DriverName = "Buyer-specific",
                Rank = 2,
                Status = "RED",
                WhyItMatters = "x",
                Dimension1 = "CUST-0001"
            },
            new DriverValue
            {
                TenantId = tenantId,
                PeriodEnd = period,
                PillarCode = "AR",
                DriverName = "Other buyer",
                Rank = 3,
                Status = "RED",
                WhyItMatters = "x",
                Dimension1 = "CUST-9999"
            });
        await db.SaveChangesAsync();

        var all = await db.DriverValues.Where(d => d.TenantId == tenantId && d.PeriodEnd == period).ToListAsync();
        var cid = "CUST-0001";
        var filtered = all.Where(d =>
            string.IsNullOrWhiteSpace(d.Dimension1) ||
            string.Equals(d.Dimension1, cid, StringComparison.OrdinalIgnoreCase)).ToList();

        Assert.Equal(2, filtered.Count);
        Assert.Contains(filtered, d => d.DriverName == "Fix aging process");
        Assert.Contains(filtered, d => d.DriverName == "Buyer-specific");
    }
}
