using DecisionOS.Distribution.Infrastructure.Workbooks;

namespace DecisionOS.Distribution.Tests;

public class WorkbookRollupKpiExtractorTests
{
    [Fact]
    public void TryComputeArPastDuePercent_FromTerryBuckets_ReturnsAbout30Pct()
    {
        var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["AR_Total"] = "5760000",
            ["AR_Current"] = "4032000",
            ["AR_1_30"] = "1152000",
            ["AR_61_90"] = "288000",
            ["AR_Over_90"] = "288000"
        };

        var pct = WorkbookRollupKpiExtractor.TryComputeArPastDuePercent(row);
        Assert.NotNull(pct);
        Assert.InRange(pct!.Value, 0.29m, 0.31m);
    }

    [Fact]
    public void NormalizeRatio_RejectsDollarAmounts()
    {
        Assert.Null(WorkbookRollupKpiExtractor.NormalizeRatio(5760000m));
        Assert.Equal(0.3m, WorkbookRollupKpiExtractor.NormalizeRatio(0.3m));
        Assert.Equal(0.3m, WorkbookRollupKpiExtractor.NormalizeRatio(30m));
    }

    [Fact]
    public void TryComputeNetProfitPercent_FromOperatingProfitAndSales()
    {
        var result = WorkbookRollupKpiExtractor.TryComputeNetProfitPercent(1_000_000m, 60_000m);
        Assert.Equal(0.06m, result);
    }

    [Fact]
    public void TryComputeCcc_FromRollupBalances()
    {
        var ccc = WorkbookRollupKpiExtractor.TryComputeCcc(
            netSales: 700_000m,
            weeklyCogs: 500_000m,
            inventoryValue: 1_000_000m,
            arBalance: 800_000m,
            apBalance: 200_000m);
        Assert.NotNull(ccc);
        Assert.True(ccc > 0);
    }
}
