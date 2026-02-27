using DecisionOS.Distribution.Domain;

namespace DecisionOS.Distribution.Tests;

public class DriverRankingServiceTests
{
    private readonly DriverRankingService _sut = new();

    private static DriverValue MakeDriver(string pillarCode, decimal current, string name = "Driver") => new()
    {
        TenantId = Guid.NewGuid(),
        PeriodEnd = new DateOnly(2026, 2, 27),
        PillarCode = pillarCode,
        DriverName = name,
        Current = current,
        Rank = 0,
        Status = "GREEN",
        WhyItMatters = "Test"
    };

    [Fact]
    public void RankDriversForPillar_FiltersToCorrectPillar()
    {
        var drivers = new List<DriverValue>
        {
            MakeDriver("SALES", 100m, "Sales1"),
            MakeDriver("OPS", 200m, "Ops1"),
            MakeDriver("SALES", 150m, "Sales2"),
            MakeDriver("OPS", 50m, "Ops2")
        };

        var result = _sut.RankDriversForPillar(drivers, "SALES");

        Assert.All(result, d => Assert.Equal("SALES", d.PillarCode));
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void RankDriversForPillar_OrdersByCurrentDescending()
    {
        var drivers = new List<DriverValue>
        {
            MakeDriver("SALES", 50m, "Low"),
            MakeDriver("SALES", 200m, "High"),
            MakeDriver("SALES", 100m, "Mid")
        };

        var result = _sut.RankDriversForPillar(drivers, "SALES");

        Assert.Equal(200m, result[0].Current);
        Assert.Equal(100m, result[1].Current);
        Assert.Equal(50m, result[2].Current);
    }

    [Fact]
    public void RankDriversForPillar_TakesTopN()
    {
        var drivers = new List<DriverValue>
        {
            MakeDriver("SALES", 10m),
            MakeDriver("SALES", 20m),
            MakeDriver("SALES", 30m),
            MakeDriver("SALES", 40m),
            MakeDriver("SALES", 50m)
        };

        var result = _sut.RankDriversForPillar(drivers, "SALES", topN: 3);

        Assert.Equal(3, result.Count);
        Assert.Equal(50m, result[0].Current);
        Assert.Equal(40m, result[1].Current);
        Assert.Equal(30m, result[2].Current);
    }

    [Fact]
    public void RankDriversForPillar_ReassignsRank()
    {
        var drivers = new List<DriverValue>
        {
            MakeDriver("SALES", 300m),
            MakeDriver("SALES", 100m),
            MakeDriver("SALES", 200m)
        };

        var result = _sut.RankDriversForPillar(drivers, "SALES");

        Assert.Equal(1, result[0].Rank);
        Assert.Equal(2, result[1].Rank);
        Assert.Equal(3, result[2].Rank);
    }

    [Fact]
    public void RankDriversForPillar_EmptyList_ReturnsEmpty()
    {
        var result = _sut.RankDriversForPillar(new List<DriverValue>(), "SALES");

        Assert.Empty(result);
    }
}
