using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure;

namespace DecisionOS.Distribution.Tests;

public class SimplifiedWorkbookReviewServiceTests
{
    [Fact]
    public void ApplyOperatorOverrides_PreservesAutoMappings_WhenFormSendsEmptyMappings()
    {
        var detection = new WorkbookDetectionResult
        {
            Sheets =
            [
                new DetectedSheet
                {
                    SheetName = "Weekly_Financials",
                    Kind = WorkbookSheetKind.WeeklyRollup,
                    Headers = ["Week_Ending", "Gross_Margin_%"],
                    ColumnMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Week_Ending"] = "Period_End_Date",
                        ["Gross_Margin_%"] = "Gross_Margin_Percent"
                    }
                }
            ]
        };

        var input = new WorkbookReviewInput
        {
            Sheets =
            [
                new SheetReviewInput
                {
                    SheetName = "Weekly_Financials",
                    Kind = WorkbookSheetKind.WeeklyRollup,
                    ColumnMappings = new Dictionary<string, string>()
                }
            ]
        };

        var svc = new SimplifiedWorkbookReviewService(null!, null!);
        var updated = svc.ApplyOperatorOverrides(detection, input);

        var sheet = updated.Sheets.Single();
        Assert.Equal("Period_End_Date", sheet.ColumnMappings["Week_Ending"]);
        Assert.Equal("Gross_Margin_Percent", sheet.ColumnMappings["Gross_Margin_%"]);
    }
}
