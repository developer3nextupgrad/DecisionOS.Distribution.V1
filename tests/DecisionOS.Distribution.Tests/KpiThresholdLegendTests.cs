using DecisionOS.Distribution.Domain;
using Xunit;

namespace DecisionOS.Distribution.Tests;

public class KpiThresholdLegendTests
{
    [Fact]
    public void DescribeBands_DOH_MatchesKpiStatusServiceBoundaries()
    {
        var def = new KpiDefinition
        {
            Code = "DOH",
            Unit = "days",
            Direction = KpiDirection.LowerIsBetter,
            Target = 45m,
            AmberThreshold = 55m,
            RedThreshold = 70m,
            RecommendedAction = "",
            DiagnosticChecks = ""
        };

        var sut = new KpiStatusService();
        Assert.Equal("GREEN", sut.ComputeStatus(def, 40m));
        Assert.Equal("YELLOW", sut.ComputeStatus(def, 55m));
        Assert.Equal("RED", sut.ComputeStatus(def, 80m));

        var bands = KpiThresholdLegend.DescribeBands(def);
        Assert.Contains("Green ≤ 45 days", bands);
        Assert.Contains("Yellow ≤ 55 days", bands);
        Assert.Contains("Red > 55 days", bands);
    }

    [Fact]
    public void DescribeForSnapshot_Gray_ExplainsNotScored()
    {
        var def = new KpiDefinition
        {
            Code = "DOH",
            Unit = "days",
            Direction = KpiDirection.LowerIsBetter,
            Target = 45m,
            AmberThreshold = 55m,
            RedThreshold = 70m,
            RecommendedAction = "",
            DiagnosticChecks = ""
        };

        var text = KpiThresholdLegend.DescribeForSnapshot(def, "GRAY");
        Assert.Contains("GRAY", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not scored", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Green ≤ 45 days", text);
    }
}
