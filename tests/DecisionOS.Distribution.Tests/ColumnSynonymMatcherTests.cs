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

    [Fact]
    public void InferMappings_SteveRollupHeaders_MapsBalancesAndMargins()
    {
        var headers = new[]
        {
            "Week_End_Date", "Net_Sales", "COGS", "Gross_Margin_%", "Inventory_Value_End",
            "Fill_Rate_%", "AR_Ending", "AR_Over_60_%", "AP_Ending", "AP_Past_Due_%"
        };
        var map = ColumnSynonymMatcher.InferMappings(headers, WorkbookSheetKind.WeeklyRollup);

        Assert.Equal("AR_Balance", map["AR_Ending"]);
        Assert.Equal("AP_Balance", map["AP_Ending"]);
        Assert.Equal("Inventory_Value", map["Inventory_Value_End"]);
        Assert.Equal("Fill_Rate_Pct", map["Fill_Rate_%"]);
        Assert.Equal("AR_Over_60_Pct", map["AR_Over_60_%"]);
    }

    [Fact]
    public void InferMappings_QbDecisionOsPlHeaders_MapsDateIncomeCogsGrossProfit()
    {
        var headers = new[]
        {
            "Date", "4100 RETAIL INCOME", "Total 4000 SALES INCOME", "Total Income",
            "5000 COGS - SCBS", "5100 COGS - FREIGHT", "Total COGS", "Gross Profit",
            "DONATIONS", "Total ADVERTISING & PROMOTION"
        };
        var map = ColumnSynonymMatcher.InferMappings(headers, WorkbookSheetKind.WeeklyRollup);

        Assert.Equal("Period_End_Date", map["Date"]);
        Assert.Equal("Net_Sales", map["Total Income"]);
        Assert.Equal("COGS", map["Total COGS"]);
        Assert.Equal("Gross_Profit", map["Gross Profit"]);
        Assert.False(map.ContainsKey("5000 COGS - SCBS"));
        Assert.False(map.ContainsKey("5100 COGS - FREIGHT"));
        Assert.False(map.ContainsKey("Total 4000 SALES INCOME"));
    }

    [Fact]
    public void InferMappings_QbDecisionOsBsHeaders_MapsCashArInventoryAp()
    {
        var headers = new[]
        {
            "Date", "Everbank Business Checking", "Total Checking/Savings", "Cash on Hand",
            "1000 AR-KS", "1100 Inventory - KS", "Total Other Current Assets", "TOTAL ASSETS",
            "1900 Accounts Payable", "Total Accounts Payable", "Accrued Payroll"
        };
        var map = ColumnSynonymMatcher.InferMappings(headers, WorkbookSheetKind.WeeklyRollup);

        Assert.Equal("Period_End_Date", map["Date"]);
        Assert.Equal("Cash_Balance", map["Total Checking/Savings"]);
        Assert.Equal("AR_Balance", map["1000 AR-KS"]);
        Assert.Equal("Inventory_Value", map["1100 Inventory - KS"]);
        Assert.Equal("AP_Balance", map["Total Accounts Payable"]);
        Assert.False(map.ContainsKey("1900 Accounts Payable"));
        Assert.False(map.ContainsKey("Cash on Hand"));
        Assert.False(map.ContainsKey("Everbank Business Checking"));
    }

    [Fact]
    public void InferMappings_QbDecisionOsCfHeaders_MapsDateNetIncomeCash_NotBalanceChanges()
    {
        var headers = new[]
        {
            "Date", "Net Income", "1000 AR-KS", "1100 Inventory - KS", "1900 Accounts Payable",
            "Net cash provided by Operating Activities", "Net cash increase for period",
            "Cash at beginning of period", "Cash at end of period"
        };
        var map = ColumnSynonymMatcher.InferMappings(headers, WorkbookSheetKind.WeeklyRollup);

        Assert.Equal("Period_End_Date", map["Date"]);
        Assert.Equal("Net_Income", map["Net Income"]);
        Assert.Equal("Cash_Balance", map["Cash at end of period"]);
        Assert.False(map.ContainsKey("1000 AR-KS"));
        Assert.False(map.ContainsKey("1100 Inventory - KS"));
        Assert.False(map.ContainsKey("1900 Accounts Payable"));
    }

    [Fact]
    public void InferMappings_SalesSheet_DoesNotMapBareDateToPeriodEnd()
    {
        var headers = new[] { "Date", "SKU", "Net_Sales", "Quantity_Sold", "Customer_ID" };
        var map = ColumnSynonymMatcher.InferMappings(headers, WorkbookSheetKind.Sales);

        Assert.Equal("Net_Sales", map["Net_Sales"]);
        Assert.Equal("SKU_ID", map["SKU"]);
        if (map.TryGetValue("Date", out var dateField))
            Assert.NotEqual("Period_End_Date", dateField);
    }

    [Fact]
    public void InferMappings_RemarksHeader_DoesNotMapToArBalance()
    {
        var headers = new[] { "Week_End_Date", "Net_Sales", "Remarks" };
        var map = ColumnSynonymMatcher.InferMappings(headers, WorkbookSheetKind.WeeklyRollup);

        Assert.False(map.ContainsKey("Remarks"));
        Assert.Equal("Period_End_Date", map["Week_End_Date"]);
    }

    [Fact]
    public void InferMappings_WeekEndDate_IsNotReplacedByBareDate()
    {
        var headers = new[] { "Date", "Week_End_Date", "Net_Sales", "COGS" };
        var map = ColumnSynonymMatcher.InferMappings(headers, WorkbookSheetKind.WeeklyRollup);

        Assert.Equal("Period_End_Date", map["Week_End_Date"]);
        Assert.Contains("Period_End_Date", map.Values, StringComparer.OrdinalIgnoreCase);
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
