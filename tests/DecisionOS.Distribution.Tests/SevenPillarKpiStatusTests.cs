using DecisionOS.Distribution.Domain;

namespace DecisionOS.Distribution.Tests;

/// <summary>
/// Tests that the seven pillar KPIs from the architecture document produce correct status
/// (GREEN / YELLOW / RED) for given values per the design thresholds.
/// </summary>
public class SevenPillarKpiStatusTests
{
    private readonly KpiStatusService _sut = new();

    [Fact]
    public void CCC_LowerIsBetter_MatchesDesignThresholds()
    {
        var def = new KpiDefinition
        {
            Code = "CCC",
            Name = "Cash Conversion Cycle (CCC Days)",
            Unit = "days",
            Direction = KpiDirection.LowerIsBetter,
            Target = 45m,
            AmberThreshold = 55m,
            RedThreshold = 70m,
            RecommendedAction = "",
            DiagnosticChecks = ""
        };
        Assert.Equal("GREEN", _sut.ComputeStatus(def, 40m));
        Assert.Equal("GREEN", _sut.ComputeStatus(def, 45m));
        Assert.Equal("YELLOW", _sut.ComputeStatus(def, 50m));
        Assert.Equal("YELLOW", _sut.ComputeStatus(def, 55m));
        Assert.Equal("RED", _sut.ComputeStatus(def, 75m));
    }

    [Fact]
    public void GrossMargin_HigherIsBetter_MatchesDesignThresholds()
    {
        var def = new KpiDefinition
        {
            Code = "GrossMargin%",
            Name = "Gross Margin %",
            Unit = "pct",
            Direction = KpiDirection.HigherIsBetter,
            Target = 0.28m,
            AmberThreshold = 0.265m,
            RedThreshold = 0.25m,
            RecommendedAction = "",
            DiagnosticChecks = ""
        };
        Assert.Equal("GREEN", _sut.ComputeStatus(def, 0.30m));
        Assert.Equal("GREEN", _sut.ComputeStatus(def, 0.28m));
        Assert.Equal("YELLOW", _sut.ComputeStatus(def, 0.27m));
        Assert.Equal("YELLOW", _sut.ComputeStatus(def, 0.265m));
        Assert.Equal("RED", _sut.ComputeStatus(def, 0.20m));
    }

    [Fact]
    public void NetProfit_HigherIsBetter_MatchesDesignThresholds()
    {
        var def = new KpiDefinition
        {
            Code = "NetProfit%",
            Name = "Net Profit %",
            Unit = "pct",
            Direction = KpiDirection.HigherIsBetter,
            Target = 0.06m,
            AmberThreshold = 0.045m,
            RedThreshold = 0.03m,
            RecommendedAction = "",
            DiagnosticChecks = ""
        };
        Assert.Equal("GREEN", _sut.ComputeStatus(def, 0.07m));
        Assert.Equal("YELLOW", _sut.ComputeStatus(def, 0.05m));
        Assert.Equal("RED", _sut.ComputeStatus(def, 0.02m));
    }

    [Fact]
    public void AR_PastDue31p_LowerIsBetter_MatchesDesignThresholds()
    {
        var def = new KpiDefinition
        {
            Code = "AR_PastDue31p%",
            Name = "A/R Health",
            Unit = "pct",
            Direction = KpiDirection.LowerIsBetter,
            Target = 0.12m,
            AmberThreshold = 0.15m,
            RedThreshold = 0.20m,
            RecommendedAction = "",
            DiagnosticChecks = ""
        };
        Assert.Equal("GREEN", _sut.ComputeStatus(def, 0.10m));
        Assert.Equal("YELLOW", _sut.ComputeStatus(def, 0.14m));
        Assert.Equal("RED", _sut.ComputeStatus(def, 0.25m));
    }

    [Fact]
    public void DOH_LowerIsBetter_MatchesDesignThresholds()
    {
        var def = new KpiDefinition
        {
            Code = "DOH",
            Name = "Inventory Health (DOH)",
            Unit = "days",
            Direction = KpiDirection.LowerIsBetter,
            Target = 45m,
            AmberThreshold = 55m,
            RedThreshold = 70m,
            RecommendedAction = "",
            DiagnosticChecks = ""
        };
        Assert.Equal("GREEN", _sut.ComputeStatus(def, 40m));
        Assert.Equal("YELLOW", _sut.ComputeStatus(def, 55m));
        Assert.Equal("RED", _sut.ComputeStatus(def, 80m));
    }

    [Fact]
    public void AP_PastDue31p_LowerIsBetter_MatchesDesignThresholds()
    {
        var def = new KpiDefinition
        {
            Code = "AP_PastDue31p%",
            Name = "A/P & Purchasing Efficiency",
            Unit = "pct",
            Direction = KpiDirection.LowerIsBetter,
            Target = 0.10m,
            AmberThreshold = 0.12m,
            RedThreshold = 0.18m,
            RecommendedAction = "",
            DiagnosticChecks = ""
        };
        Assert.Equal("GREEN", _sut.ComputeStatus(def, 0.08m));
        Assert.Equal("YELLOW", _sut.ComputeStatus(def, 0.11m));
        Assert.Equal("RED", _sut.ComputeStatus(def, 0.20m));
    }

    [Fact]
    public void PerfectOrderRate_HigherIsBetter_MatchesDesignThresholds()
    {
        var def = new KpiDefinition
        {
            Code = "PerfectOrderRate",
            Name = "Service / Fulfillment (Perfect Order)",
            Unit = "pct",
            Direction = KpiDirection.HigherIsBetter,
            Target = 0.93m,
            AmberThreshold = 0.91m,
            RedThreshold = 0.89m,
            RecommendedAction = "",
            DiagnosticChecks = ""
        };
        Assert.Equal("GREEN", _sut.ComputeStatus(def, 0.95m));
        Assert.Equal("YELLOW", _sut.ComputeStatus(def, 0.92m));
        Assert.Equal("RED", _sut.ComputeStatus(def, 0.85m));
    }
}
