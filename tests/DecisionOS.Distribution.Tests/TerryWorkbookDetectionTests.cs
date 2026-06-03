using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure.Workbooks;

namespace DecisionOS.Distribution.Tests;

public class TerryWorkbookDetectionTests
{
    private static string ResolveFixturePath()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Terrys_Plumbing_AddOn_Test_Pack_v1.0.xlsx");
        if (File.Exists(path)) return path;
        var alt = @"c:\Users\emran\Downloads\Re_ bulk excel Import\DPOS_Distribution_Terrys_Plumbing_AddOn_Test_Pack_v1.0.xlsx";
        return File.Exists(alt) ? alt : path;
    }

    [Fact]
    public void Analyze_TerryAddOn_FindsWeeklyFinancialsAndPeriods()
    {
        var path = ResolveFixturePath();
        if (!File.Exists(path)) return;

        var analyzer = new WorkbookAnalyzer();
        var result = analyzer.AnalyzeFile(path, UploadCadence.Weekly, null);

        Assert.Contains(result.Sheets, s =>
            s.Kind == WorkbookSheetKind.WeeklyRollup &&
            s.SheetName.Contains("Financial", StringComparison.OrdinalIgnoreCase));

        Assert.NotEmpty(result.FilteredPeriodEnds);
        Assert.True(result.FilteredPeriodEnds.Count >= 20,
            $"Expected ~26 weeks, got {result.FilteredPeriodEnds.Count}");
    }

    [Fact]
    public void Analyze_TerryAddOn_ArAgingClassifiedAsWeeklyRollup()
    {
        var path = ResolveFixturePath();
        if (!File.Exists(path)) return;

        var analyzer = new WorkbookAnalyzer();
        var result = analyzer.AnalyzeFile(path, UploadCadence.Weekly, null);

        var arAging = result.Sheets.FirstOrDefault(s =>
            s.SheetName.Contains("AR", StringComparison.OrdinalIgnoreCase) &&
            s.SheetName.Contains("Aging", StringComparison.OrdinalIgnoreCase))
            ?? result.Sheets.FirstOrDefault(s =>
                s.Kind == WorkbookSheetKind.WeeklyRollup &&
                s.ColumnMappings.Values.Any(v =>
                    string.Equals(v, "AR_Over_60_Pct", StringComparison.OrdinalIgnoreCase)));

        Assert.NotNull(arAging);
        Assert.True(arAging!.Kind == WorkbookSheetKind.WeeklyRollup,
            $"Expected WeeklyRollup for '{arAging.SheetName}' but got {arAging.Kind}. All: {string.Join("; ", result.Sheets.Select(s => $"{s.SheetName}={s.Kind}"))}");
    }

    [Fact]
    public void KpiCoverage_TerryWorkbook_ReportsGrossMarginReady()
    {
        var path = ResolveFixturePath();
        if (!File.Exists(path)) return;

        var detection = new WorkbookAnalyzer().AnalyzeFile(path, UploadCadence.Weekly, null);
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "GrossMargin%", "AR_PastDue31p%", "AP_PastDue31p%", "DOH", "CCC", "NetProfit%", "PerfectOrderRate"
        };

        var lines = KpiCoveragePreviewBuilder.Build(detection, codes);
        var gm = lines.First(l => l.KpiCode == "GrossMargin%");

        Assert.NotEqual(KpiCoverageStatus.MissingExpectGray, gm.Status);
    }
}
