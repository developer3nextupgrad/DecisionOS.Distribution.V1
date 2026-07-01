namespace DecisionOS.Distribution.Infrastructure.Workbooks;

/// <summary>Derives KPI values from weekly rollup / aging sheets without treating dollar buckets as percentages.</summary>
public static class WorkbookRollupKpiExtractor
{
    public static decimal? NormalizeRatio(decimal value)
    {
        var d = value;
        if (d > 1m && d <= 100m) d /= 100m;
        return d is >= 0m and <= 1m ? d : null;
    }

    public static bool SheetHasVendorRows(IReadOnlyList<string> headers) =>
        headers.Any(h =>
        {
            var n = WorkbookParseHelper.NormalizeHeader(h);
            return n.Contains("vendorid", StringComparison.Ordinal) ||
                   n.Contains("vendorname", StringComparison.Ordinal);
        });

    public static decimal? TryComputeArPastDuePercent(IReadOnlyDictionary<string, string?> row)
    {
        var fromPct = TryRatioFromColumn(row, "arover90", "arover60", "arpastdue", "30pct", "30plus");
        if (fromPct is not null) return fromPct;

        var total = SumColumns(row, "artotal");
        if (total is null or <= 0) return null;

        var past = SumColumns(row, "ar130", "ar1_30", "ar3060", "ar6190", "ar61_90", "arover90");
        if (past is > 0) return past / total;

        var current = SumColumns(row, "arcurrent");
        if (current is > 0) return (total - current) / total;

        return null;
    }

    public static decimal? AggregateApPastDuePercent(
        IEnumerable<IReadOnlyDictionary<string, string?>> rowsForPeriod)
    {
        decimal total = 0, past = 0;
        foreach (var row in rowsForPeriod)
        {
            var rowTotal = SumColumns(row, "aptotal", "ap_total");
            var rowPast = SumColumns(row, "apover60", "apover90", "ap3160", "ap31_60", "appastdue");
            if (rowTotal is > 0)
            {
                total += rowTotal.Value;
                past += rowPast ?? 0;
            }
        }

        if (total > 0 && past > 0) return past / total;
        return null;
    }

    public static decimal? TryComputeDoh(decimal? inventoryValue, decimal? weeklyCogs)
    {
        if (inventoryValue is null or <= 0 || weeklyCogs is null or <= 0) return null;
        var perDay = weeklyCogs.Value / 7m;
        if (perDay <= 0) return null;
        return inventoryValue / perDay;
    }

    public static decimal? TryComputeNetProfitPercent(decimal? netSales, decimal? operatingProfit)
    {
        if (netSales is null or <= 0 || operatingProfit is null) return null;
        return operatingProfit / netSales;
    }

    /// <summary>DSO + DIO − DPO from weekly rollup balances (same formula as WeeklyScoringService.CCC).</summary>
    public static decimal? TryComputeCcc(
        decimal? netSales,
        decimal? weeklyCogs,
        decimal? inventoryValue,
        decimal? arBalance,
        decimal? apBalance)
    {
        if (netSales is null or <= 0 || weeklyCogs is null or <= 0) return null;
        if (arBalance is null || apBalance is null) return null;

        var dio = TryComputeDoh(inventoryValue, weeklyCogs);
        if (dio is null) return null;

        var dso = arBalance.Value / (netSales.Value / 7m);
        var dpo = apBalance.Value / (weeklyCogs.Value / 7m);
        return dso + dio.Value - dpo;
    }

    public static decimal? TryParseRollupBalance(
        IReadOnlyDictionary<string, string?> row,
        IReadOnlyDictionary<string, string> colMap,
        string systemField,
        params string[] headerTokens)
    {
        var mapped = ColumnSynonymMatcher.GetMapped(row, colMap, systemField);
        var fromMap = WorkbookParseHelper.ParseDecimal(mapped);
        if (fromMap is not null) return fromMap;

        foreach (var kvp in row)
        {
            var norm = WorkbookParseHelper.NormalizeHeader(kvp.Key);
            if (!headerTokens.Any(t => norm.Contains(t, StringComparison.Ordinal))) continue;
            var d = WorkbookParseHelper.ParseDecimal(kvp.Value);
            if (d is not null) return d;
        }

        return null;
    }

    private static decimal? TryRatioFromColumn(IReadOnlyDictionary<string, string?> row, params string[] headerTokens)
    {
        foreach (var kvp in row)
        {
            var norm = WorkbookParseHelper.NormalizeHeader(kvp.Key);
            if (!headerTokens.Any(t => norm.Contains(t, StringComparison.Ordinal))) continue;
            var d = WorkbookParseHelper.ParseDecimal(kvp.Value);
            if (d is null) continue;
            var ratio = NormalizeRatio(d.Value);
            if (ratio is not null) return ratio;
        }
        return null;
    }

    private static decimal? SumColumns(IReadOnlyDictionary<string, string?> row, params string[] headerTokens)
    {
        decimal sum = 0;
        var any = false;
        foreach (var kvp in row)
        {
            var norm = WorkbookParseHelper.NormalizeHeader(kvp.Key);
            if (!headerTokens.Any(t => norm.Contains(t, StringComparison.Ordinal))) continue;
            var d = WorkbookParseHelper.ParseDecimal(kvp.Value);
            if (d is null) continue;
            sum += d.Value;
            any = true;
        }
        return any ? sum : null;
    }
}
