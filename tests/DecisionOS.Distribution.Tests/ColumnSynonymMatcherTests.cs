using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure.Workbooks;

namespace DecisionOS.Distribution.Tests;

public class ColumnSynonymMatcherTests
{
    [Fact]
    public void InferMappings_RollupNetProfitPercent_MapsToNetProfitPercent()
    {
        var headers = new[] { "Week_End_Date", "Net_Sales", "Net_Profit_%", "COGS" };
        var map = ColumnSynonymMatcher.InferMappings(headers, WorkbookSheetKind.WeeklyRollup);

        Assert.Equal("Period_End_Date", map["Week_End_Date"]);
        Assert.Equal("Net_Profit_Percent", map["Net_Profit_%"]);
    }

    [Fact]
    public void InferMappings_RollupNetIncomeDollars_MapsToNetIncome()
    {
        var headers = new[] { "Week_End_Date", "Net_Sales", "Net_Income", "COGS" };
        var map = ColumnSynonymMatcher.InferMappings(headers, WorkbookSheetKind.WeeklyRollup);

        Assert.Equal("Net_Income", map["Net_Income"]);
    }

    [Fact]
    public void InferMappings_OperatingProfit_MapsToOperatingProfit()
    {
        var headers = new[] { "Week_End_Date", "Net_Sales", "Operating_Profit", "COGS" };
        var map = ColumnSynonymMatcher.InferMappings(headers, WorkbookSheetKind.WeeklyRollup);

        Assert.Equal("Operating_Profit", map["Operating_Profit"]);
    }

    [Fact]
    public void InferMappings_InventoryValueEnd_MapsToInventoryValue()
    {
        var headers = new[] { "Week_End_Date", "Inventory_Value_End", "Net_Sales" };
        var map = ColumnSynonymMatcher.InferMappings(headers, WorkbookSheetKind.WeeklyRollup);

        Assert.Equal("Inventory_Value", map["Inventory_Value_End"]);
    }

    [Theory]
    [InlineData("AR_Total", "AR_Over_60_Pct", true)]
    [InlineData("AR_Over_90", "AR_Over_60_Pct", true)]
    [InlineData("AR_Over_60_%", "AR_Over_60_Pct", false)]
    public void GetMappingWarning_FlagsDollarToPercentMismatch(string header, string field, bool expectWarning)
    {
        var warning = SystemFieldDisplayCatalog.GetMappingWarning(header, field);
        if (expectWarning)
            Assert.NotNull(warning);
        else
            Assert.Null(warning);
    }
}
