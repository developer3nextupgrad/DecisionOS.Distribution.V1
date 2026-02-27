using DecisionOS.Distribution.Domain;

namespace DecisionOS.Distribution.Tests;

public class AlertServiceTests
{
    private readonly AlertService _sut = new();

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateOnly PeriodEnd = new(2026, 2, 27);

    private static KpiDefinition MakeDefinition(int id, string name, decimal target) => new()
    {
        Id = id,
        Code = name.ToUpperInvariant(),
        Name = name,
        Unit = "%",
        Direction = KpiDirection.HigherIsBetter,
        Target = target,
        AmberThreshold = target * 0.95m,
        RedThreshold = target * 0.90m,
        RecommendedAction = $"Fix {name}",
        DiagnosticChecks = $"Check {name} logs"
    };

    private static KpiSnapshot MakeSnapshot(int kpiDefinitionId, decimal value, string status) => new()
    {
        TenantId = TenantId,
        PeriodEnd = PeriodEnd,
        KpiDefinitionId = kpiDefinitionId,
        Value = value,
        Status = status
    };

    [Fact]
    public void SelectTopAlert_AllGreen_ReturnsNull()
    {
        var definitions = new List<KpiDefinition> { MakeDefinition(1, "Revenue", 100m) };
        var snapshots = new List<KpiSnapshot> { MakeSnapshot(1, 105m, "GREEN") };

        var result = _sut.SelectTopAlert(TenantId, PeriodEnd, snapshots, definitions);

        Assert.Null(result);
    }

    [Fact]
    public void SelectTopAlert_SingleRed_ReturnsThatKpi()
    {
        var definitions = new List<KpiDefinition> { MakeDefinition(1, "Revenue", 100m) };
        var snapshots = new List<KpiSnapshot> { MakeSnapshot(1, 80m, "RED") };

        var result = _sut.SelectTopAlert(TenantId, PeriodEnd, snapshots, definitions);

        Assert.NotNull(result);
        Assert.Equal(1, result.KpiDefinitionId);
        Assert.Equal("RED", result.Severity);
    }

    [Fact]
    public void SelectTopAlert_MultipleNonGreen_PicksHighestSeverity()
    {
        var definitions = new List<KpiDefinition>
        {
            MakeDefinition(1, "Revenue", 100m),
            MakeDefinition(2, "Margin", 50m)
        };
        var snapshots = new List<KpiSnapshot>
        {
            MakeSnapshot(1, 95m, "YELLOW"),
            MakeSnapshot(2, 30m, "RED")
        };

        var result = _sut.SelectTopAlert(TenantId, PeriodEnd, snapshots, definitions);

        Assert.NotNull(result);
        Assert.Equal(2, result.KpiDefinitionId);
        Assert.Equal("RED", result.Severity);
    }

    [Fact]
    public void SelectTopAlert_TieBreak_PicksLargestDeviation()
    {
        var definitions = new List<KpiDefinition>
        {
            MakeDefinition(1, "Revenue", 100m),
            MakeDefinition(2, "Margin", 100m)
        };
        var snapshots = new List<KpiSnapshot>
        {
            MakeSnapshot(1, 90m, "RED"),   // |90-100|/100 = 0.10
            MakeSnapshot(2, 60m, "RED")    // |60-100|/100 = 0.40  ← larger deviation
        };

        var result = _sut.SelectTopAlert(TenantId, PeriodEnd, snapshots, definitions);

        Assert.NotNull(result);
        Assert.Equal(2, result.KpiDefinitionId);
    }

    [Fact]
    public void SelectTopAlert_SetsSeverityAndReasonSummary()
    {
        var definitions = new List<KpiDefinition> { MakeDefinition(1, "Revenue", 100m) };
        var snapshots = new List<KpiSnapshot> { MakeSnapshot(1, 80m, "RED") };

        var result = _sut.SelectTopAlert(TenantId, PeriodEnd, snapshots, definitions);

        Assert.NotNull(result);
        Assert.Equal("RED", result.Severity);
        Assert.Equal("Revenue is RED at 80 (target: 100)", result.ReasonSummary);
        Assert.Equal(TenantId, result.TenantId);
        Assert.Equal(PeriodEnd, result.PeriodEnd);
    }
}
