using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure.Workbooks;

namespace DecisionOS.Distribution.Tests;

public class FinancialsUploadTemplateTests
{
    private static string TemplatePath =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "DecisionOS.Distribution.Web", "wwwroot", "downloads",
            "DecisionOS_Financials_Upload_Template.xlsx"));

    [Fact]
    public void Template_Exists_ClassifiesFinancialsAndAr_MonthlyPeriods()
    {
        Assert.True(File.Exists(TemplatePath), TemplatePath);

        var analyzer = new WorkbookAnalyzer();
        var result = analyzer.AnalyzeFile(TemplatePath, UploadCadence.Monthly, new DateOnly(2026, 5, 31));

        Assert.Contains(result.Sheets, s => s.SheetName == "README_Import_Map" && s.Kind == WorkbookSheetKind.Skip);
        Assert.Contains(result.Sheets, s => s.SheetName == "Weekly_Financials" && s.Kind == WorkbookSheetKind.WeeklyRollup);
        Assert.Contains(result.Sheets, s => s.SheetName == "P_and_L" && s.Kind == WorkbookSheetKind.WeeklyRollup);
        Assert.Contains(result.Sheets, s => s.SheetName == "Weekly_Balance_Sheet" && s.Kind == WorkbookSheetKind.WeeklyRollup);
        Assert.Contains(result.Sheets, s => s.SheetName == "Weekly_Cash_Flow" && s.Kind == WorkbookSheetKind.WeeklyRollup);
        Assert.Contains(result.Sheets, s => s.SheetName == "Accounts_Receivable" && s.Kind == WorkbookSheetKind.AccountsReceivable);

        Assert.Equal(3, result.FilteredPeriodEnds.Count);
        Assert.Contains(new DateOnly(2026, 5, 31), result.FilteredPeriodEnds);
        Assert.Contains(new DateOnly(2026, 6, 30), result.FilteredPeriodEnds);
        Assert.Contains(new DateOnly(2026, 7, 31), result.FilteredPeriodEnds);

        var rollup = result.Sheets.First(s => s.SheetName == "Weekly_Financials");
        Assert.Contains("Period_End_Date", rollup.ColumnMappings.Values, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Net_Sales", rollup.ColumnMappings.Values, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("COGS", rollup.ColumnMappings.Values, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Net_Income", rollup.ColumnMappings.Values, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Cash_Balance", rollup.ColumnMappings.Values, StringComparer.OrdinalIgnoreCase);

        var ar = result.Sheets.First(s => s.Kind == WorkbookSheetKind.AccountsReceivable);
        Assert.Contains("Open_Balance", ar.ColumnMappings.Values, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Aging_Bucket", ar.ColumnMappings.Values, StringComparer.OrdinalIgnoreCase);
    }
}
