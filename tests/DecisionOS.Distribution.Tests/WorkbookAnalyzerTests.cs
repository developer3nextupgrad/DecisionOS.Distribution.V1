using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure.Workbooks;

namespace DecisionOS.Distribution.Tests;

public class WorkbookAnalyzerTests
{
    private static string FixturePath =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Steves_Bowling_Supply_DPOS_Distribution_Test_Data.xlsx");

    private static string? ResolveFixture()
    {
        if (File.Exists(FixturePath)) return FixturePath;
        var alt = @"c:\Users\emran\Downloads\Steves_Bowling_Supply_DPOS_Distribution_Test_Data.xlsx";
        return File.Exists(alt) ? alt : null;
    }

    [Fact]
    public void Analyze_SteveWorkbook_DetectsWeeklyRollupAndSales()
    {
        var path = ResolveFixture();
        if (path is null) return;

        var analyzer = new WorkbookAnalyzer();
        var anchor = new DateOnly(2025, 11, 22);
        var result = analyzer.AnalyzeFile(path, UploadCadence.Weekly, anchor);

        Assert.Contains(result.Sheets, s => s.Kind == WorkbookSheetKind.WeeklyRollup);
        Assert.Contains(result.Sheets, s => s.Kind == WorkbookSheetKind.Sales);
        Assert.InRange(result.FilteredPeriodEnds.Count, 20, 30);
        Assert.All(result.FilteredPeriodEnds, d => Assert.True(d >= anchor));
        Assert.All(result.FilteredPeriodEnds, d => Assert.InRange(d.Year, WorkbookDateRules.MinPeriodYear, WorkbookDateRules.MaxPeriodYear));
        Assert.Contains(result.Warnings, w => w.Contains("week-ending columns only", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_SteveWorkbook_ClassifiesHoldoverAndArAp()
    {
        var path = ResolveFixture();
        if (path is null) return;

        var analyzer = new WorkbookAnalyzer();
        var result = analyzer.AnalyzeFile(path, UploadCadence.Weekly, null);

        Assert.Contains(result.Sheets, s => s.Kind == WorkbookSheetKind.Holdover);
        Assert.Contains(result.Sheets, s => s.Kind == WorkbookSheetKind.AccountsReceivable);
        Assert.Contains(result.Sheets, s => s.Kind == WorkbookSheetKind.AccountsPayable);
    }

    [Fact]
    public void PeriodExtractor_MonthlyBucketsPeriods()
    {
        var raw = new[]
        {
            new DateOnly(2025, 11, 22),
            new DateOnly(2025, 11, 29),
            new DateOnly(2025, 12, 6)
        };
        var monthly = PeriodExtractor.ApplyCadenceAndAnchor(raw, UploadCadence.Monthly, null);
        Assert.Equal(2, monthly.Count);
    }

    [Fact]
    public void Analyze_ClientMayQbDecisionOsFiles_ClassifiesRollupAndPeriod()
    {
        var dir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "_client_docs", "Data Ingestion Normal vs Reformatted"));
        var cases = new[]
        {
            ("SCBS_May_P&L_Decision_OS.xlsx", "Net_Sales", "COGS"),
            ("SCBSMAYBS_Decision_OS_Format.xlsx", "Cash_Balance", "AR_Balance"),
            ("SCBSMAYCFS_Decision_OS_Format.xlsx", "Net_Income", "Cash_Balance")
        };

        var analyzer = new WorkbookAnalyzer();
        foreach (var (file, fieldA, fieldB) in cases)
        {
            var path = Path.Combine(dir, file);
            if (!File.Exists(path)) return;

            var result = analyzer.AnalyzeFile(path, UploadCadence.Monthly, null);
            var rollup = Assert.Single(result.Sheets, s => s.Kind == WorkbookSheetKind.WeeklyRollup);
            Assert.Equal("Period_End_Date", rollup.ColumnMappings["Date"]);
            Assert.Contains(fieldA, rollup.ColumnMappings.Values, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(fieldB, rollup.ColumnMappings.Values, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(new DateOnly(2026, 5, 31), result.FilteredPeriodEnds);
        }
    }
}
