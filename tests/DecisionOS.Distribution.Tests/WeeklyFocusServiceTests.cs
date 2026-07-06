using DecisionOS.Distribution.Domain;

namespace DecisionOS.Distribution.Tests;

public class WeeklyFocusServiceTests
{
    private readonly WeeklyFocusService _sut = new();

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateOnly PeriodEnd = new(2026, 2, 27);

    private static KpiDefinition MakeDefinition(int id, string name) => new()
    {
        Id = id,
        Code = name.ToUpperInvariant(),
        Name = name,
        Unit = "%",
        Direction = KpiDirection.HigherIsBetter,
        Target = 100m,
        AmberThreshold = 95m,
        RedThreshold = 90m,
        RecommendedAction = $"Increase {name} pipeline",
        DiagnosticChecks = $"Review {name} dashboard"
    };

    [Fact]
    public void GenerateWeeklyFocus_NullAlert_ReturnsNull()
    {
        var definitions = new List<KpiDefinition> { MakeDefinition(1, "Revenue") };

        var result = _sut.GenerateWeeklyFocus(TenantId, PeriodEnd, null, definitions);

        Assert.Null(result);
    }

    [Fact]
    public void GenerateWeeklyFocus_ValidAlert_GeneratesCorrectFocus()
    {
        var definition = MakeDefinition(1, "Revenue");
        var definitions = new List<KpiDefinition> { definition };
        var alert = new Alert
        {
            TenantId = TenantId,
            PeriodEnd = PeriodEnd,
            KpiDefinitionId = 1,
            Severity = "RED",
            ReasonSummary = "Revenue is RED at 80 (target: 100)"
        };

        var result = _sut.GenerateWeeklyFocus(TenantId, PeriodEnd, alert, definitions);

        Assert.NotNull(result);
        Assert.Equal("Will we address Revenue this week?", result.DecisionQuestion);
        Assert.Equal("Increase Revenue pipeline", result.RecommendedAction);
        Assert.Equal("Revenue needs your attention (urgent). Review Revenue dashboard", result.WhyNow);
        Assert.Equal("Operations", result.Owner);
        Assert.Equal("Weekly", result.Cadence);
    }

    [Fact]
    public void GenerateWeeklyFocus_UsesCorrectKpiDefinition()
    {
        var definitions = new List<KpiDefinition>
        {
            MakeDefinition(1, "Revenue"),
            MakeDefinition(2, "Margin")
        };
        var alert = new Alert
        {
            TenantId = TenantId,
            PeriodEnd = PeriodEnd,
            KpiDefinitionId = 2,
            Severity = "YELLOW",
            ReasonSummary = "Margin is YELLOW at 93 (target: 100)"
        };

        var result = _sut.GenerateWeeklyFocus(TenantId, PeriodEnd, alert, definitions);

        Assert.NotNull(result);
        Assert.Equal(2, result.KpiDefinitionId);
        Assert.Equal("Will we address Margin this week?", result.DecisionQuestion);
        Assert.Equal("Increase Margin pipeline", result.RecommendedAction);
        Assert.Equal("Margin needs your attention (needs attention). Review Margin dashboard", result.WhyNow);
    }
}
