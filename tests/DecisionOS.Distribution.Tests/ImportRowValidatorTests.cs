using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Import;

namespace DecisionOS.Distribution.Tests;

public class ImportRowValidatorTests
{
    private static KpiDefinition Def(decimal? min = null, decimal? max = null) => new()
    {
        Code = "K",
        Name = "K",
        Unit = "pct",
        Direction = KpiDirection.HigherIsBetter,
        Target = 0.1m,
        AmberThreshold = 0.08m,
        RedThreshold = 0.05m,
        MinValue = min,
        MaxValue = max,
        RecommendedAction = "a",
        DiagnosticChecks = "d"
    };

    [Fact]
    public void Kpi_OutsideMinMax_AddsIssue()
    {
        var r = new ImportValidationResult();
        var d = Def(min: 0.05m, max: 0.5m);
        ImportRowValidator.ValidateKpiValue(d, 0.01m, r, rowNumber: 3);
        var issue = Assert.Single(r.Issues, i => i.Field == "value");
        Assert.Equal(ImportValidationSeverity.Warning, issue.Severity);
        Assert.True(r.IsValid); // warnings imply "ready with limitations", not a blocker
    }

    [Fact]
    public void Driver_InvalidStatus_AddsIssue()
    {
        var r = new ImportValidationResult();
        ImportRowValidator.ValidateDriverRow(
            "AR",
            "X",
            driverCode: null,
            current: 1,
            rank: 1,
            status: "BLUE",
            fixProgress: null,
            Array.Empty<DriverDefinition>().ToLookup(d => d.PillarCode),
            r,
            rowNumber: 1);
        Assert.False(r.IsValid);
    }

    [Fact]
    public void Driver_CatalogRequiresDriverCode()
    {
        var defs = new List<DriverDefinition>
        {
            new()
            {
                PillarCode = "AR",
                DriverCode = "a",
                DisplayName = "A",
                IsActive = true
            }
        }.ToLookup(d => d.PillarCode);

        var r = new ImportValidationResult();
        ImportRowValidator.ValidateDriverRow(
            "AR",
            "Name",
            driverCode: null,
            current: 1,
            rank: 1,
            status: "RED",
            fixProgress: 10,
            defs,
            r,
            rowNumber: 2);
        Assert.False(r.IsValid);
    }
}
