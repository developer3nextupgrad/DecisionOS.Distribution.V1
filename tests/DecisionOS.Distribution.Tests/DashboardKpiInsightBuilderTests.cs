using DecisionOS.Distribution.Domain;
using Xunit;

namespace DecisionOS.Distribution.Tests;

public class DashboardKpiInsightBuilderTests
{
    [Fact]
    public void Build_GrayKpi_IncludesMissingDataChecklist()
    {
        var def = new KpiDefinition
        {
            Id = 1,
            Code = "DOH",
            Name = "Inventory Health",
            Unit = "days",
            Direction = KpiDirection.LowerIsBetter,
            Target = 45m,
            AmberThreshold = 55m,
            RedThreshold = 70m,
            RecommendedAction = "Balance inventory.",
            DiagnosticChecks = "Check DOH by category."
        };

        var snapshot = new KpiSnapshot
        {
            Id = 10,
            KpiDefinitionId = 1,
            KpiDefinition = def,
            Value = 0m,
            Status = "GRAY"
        };

        var insights = DashboardKpiInsightBuilder.Build(
            [snapshot],
            [],
            null,
            null,
            _ => "—",
            _ => "45 days",
            _ => "—");

        var insight = insights[10];
        Assert.NotEmpty(insight.MissingDataItems);
        Assert.Contains("Inventory", insight.MissingDataItems[0], StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(insight.MissingDataSummary);
    }

    [Fact]
    public void Build_RedHigherIsBetter_GapSummaryBelowTarget()
    {
        var def = new KpiDefinition
        {
            Id = 2,
            Code = "GrossMargin%",
            Name = "Gross Margin",
            Unit = "pct",
            Direction = KpiDirection.HigherIsBetter,
            Target = 0.28m,
            RedThreshold = 0.25m,
            RecommendedAction = "Protect margin.",
            DiagnosticChecks = "Review margin by SKU."
        };

        var snapshot = new KpiSnapshot
        {
            Id = 20,
            KpiDefinitionId = 2,
            KpiDefinition = def,
            Value = 0.24m,
            Status = "RED"
        };

        var insights = DashboardKpiInsightBuilder.Build(
            [snapshot],
            [],
            null,
            null,
            s => (s.Value * 100m).ToString("F1") + "%",
            d => (d.Target * 100m).ToString("F1") + "%",
            _ => "—");

        Assert.Contains("below target", insights[20].GapSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Critical", insights[20].StatusHeadline, StringComparison.OrdinalIgnoreCase);
    }
}
