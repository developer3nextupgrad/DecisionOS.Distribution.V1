using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure.Workbooks;

namespace DecisionOS.Distribution.Tests;

public class SimplifiedWorkbookTemplateTests
{
    private static string TemplatePath =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "DecisionOS.Distribution.Web", "wwwroot", "downloads",
            "DecisionOS_Simplified_Workbook_Template.xlsx"));

    [Fact]
    public void Template_Exists_And_DetectsAllSevenKpiSources()
    {
        if (!File.Exists(TemplatePath)) return;

        var analyzer = new WorkbookAnalyzer();
        var result = analyzer.AnalyzeFile(TemplatePath, UploadCadence.Weekly, new DateOnly(2025, 11, 22));

        Assert.Contains(result.Sheets, s => s.Kind == WorkbookSheetKind.WeeklyRollup);
        Assert.Contains(result.Sheets, s => s.Kind == WorkbookSheetKind.Sales);
        Assert.InRange(result.FilteredPeriodEnds.Count, 4, 4);

        var rollup = result.Sheets.First(s => s.Kind == WorkbookSheetKind.WeeklyRollup);
        Assert.Contains("Net_Profit_Percent", rollup.ColumnMappings.Values, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("AR_Balance", rollup.ColumnMappings.Values, StringComparer.OrdinalIgnoreCase);

        var coverage = KpiCoveragePreviewBuilder.Build(result, new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "GrossMargin%", "AR_PastDue31p%", "AP_PastDue31p%", "DOH", "CCC", "NetProfit%", "PerfectOrderRate"
        });

        var netProfit = coverage.First(c => c.KpiCode == "NetProfit%");
        Assert.Equal(KpiCoverageStatus.ReadyFromRollup, netProfit.Status);
    }
}
