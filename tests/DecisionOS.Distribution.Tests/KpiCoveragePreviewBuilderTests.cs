using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure.Workbooks;

namespace DecisionOS.Distribution.Tests;

public class KpiCoveragePreviewBuilderTests
{
    private static readonly HashSet<string> AllSeven = new(StringComparer.OrdinalIgnoreCase)
    {
        "GrossMargin%", "AR_PastDue31p%", "AP_PastDue31p%", "DOH", "CCC", "NetProfit%", "PerfectOrderRate"
    };

    [Fact]
    public void Build_RollupOnly_ReportsGrossMarginReady_AndApMissing()
    {
        var detection = new WorkbookDetectionResult
        {
            Sheets =
            [
                new DetectedSheet
                {
                    SheetName = "Weekly_Financials",
                    Kind = WorkbookSheetKind.WeeklyRollup,
                    ColumnMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Week_Ending"] = "Period_End_Date",
                        ["Gross_Margin_%"] = "Gross_Margin_Percent",
                        ["Sales"] = "Net_Sales",
                        ["COGS"] = "COGS"
                    }
                }
            ]
        };

        var lines = KpiCoveragePreviewBuilder.Build(detection, AllSeven);
        var gm = lines.First(l => l.KpiCode == "GrossMargin%");
        var ap = lines.First(l => l.KpiCode == "AP_PastDue31p%");

        Assert.Equal(KpiCoverageStatus.ReadyFromRollup, gm.Status);
        Assert.Equal(KpiCoverageStatus.MissingExpectGray, ap.Status);
    }

    [Fact]
    public void Build_ArAgingRollupMapping_ReportsArReady()
    {
        var detection = new WorkbookDetectionResult
        {
            Sheets =
            [
                new DetectedSheet
                {
                    SheetName = "AR_Aging_Weekly",
                    Kind = WorkbookSheetKind.WeeklyRollup,
                    ColumnMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Week_Ending"] = "Period_End_Date",
                        ["AR_Over_90"] = "AR_Over_60_Pct"
                    }
                }
            ]
        };

        var lines = KpiCoveragePreviewBuilder.Build(detection, AllSeven);
        Assert.Equal(KpiCoverageStatus.ReadyFromRollup,
            lines.First(l => l.KpiCode == "AR_PastDue31p%").Status);
    }
}
