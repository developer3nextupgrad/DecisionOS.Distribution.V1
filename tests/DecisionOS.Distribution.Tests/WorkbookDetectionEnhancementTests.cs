using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure.Workbooks;

namespace DecisionOS.Distribution.Tests;

public class WorkbookDetectionEnhancementTests
{
    [Fact]
    public void ResolvePeriods_AutoAdjusts_WhenAnchorExcludesAllRaw()
    {
        var raw = new[]
        {
            new DateOnly(2025, 11, 22),
            new DateOnly(2025, 11, 29),
            new DateOnly(2025, 12, 6)
        };
        var lateAnchor = new DateOnly(2026, 6, 20);

        var (_, filtered, suggested, auto) = PeriodExtractor.ResolvePeriods(raw, UploadCadence.Weekly, lateAnchor);

        Assert.True(auto);
        Assert.Equal(new DateOnly(2025, 11, 22), suggested);
        Assert.Equal(3, filtered.Count);
        Assert.All(filtered, d => Assert.True(d >= suggested));
    }

    [Fact]
    public void ColumnSynonymMatcher_MapsCommonSalesHeaders()
    {
        var headers = new[] { "Week_End_Date", "SKU", "Qty Sold", "Net Sales", "COGS" };
        var map = ColumnSynonymMatcher.InferMappings(headers, WorkbookSheetKind.Sales);

        Assert.Equal("Transaction_Date", map["Week_End_Date"]);
        Assert.Equal("SKU_ID", map["SKU"]);
        Assert.Equal("Quantity_Sold", map["Qty Sold"]);
        Assert.Equal("Net_Sales", map["Net Sales"]);
        Assert.Equal("COGS", map["COGS"]);
    }

    [Fact]
    public void Analyze_SteveWorkbook_WithLateAnchor_StillFindsPeriods()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Steves_Bowling_Supply_DPOS_Distribution_Test_Data.xlsx");
        if (!File.Exists(path))
        {
            var alt = @"c:\Users\emran\Downloads\Steves_Bowling_Supply_DPOS_Distribution_Test_Data.xlsx";
            if (!File.Exists(alt)) return;
            path = alt;
        }

        var analyzer = new WorkbookAnalyzer();
        var lateAnchor = new DateOnly(2026, 6, 20);
        var result = analyzer.AnalyzeFile(path, UploadCadence.Weekly, lateAnchor);

        Assert.NotEmpty(result.RawPeriodEnds);
        Assert.NotEmpty(result.FilteredPeriodEnds);

        if (result.RawPeriodEnds.All(d => d < lateAnchor))
        {
            Assert.True(result.AnchorAutoAdjusted);
            Assert.Equal(result.RawPeriodEnds.Min(), result.EffectiveAnchorPeriodEnd);
        }
    }
}
