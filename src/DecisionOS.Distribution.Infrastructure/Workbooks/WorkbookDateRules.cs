namespace DecisionOS.Distribution.Infrastructure.Workbooks;

/// <summary>Guards period/week values from mis-parsed IDs, invoice tokens, and Excel noise.</summary>
public static class WorkbookDateRules
{
    public const int MinPeriodYear = 2015;
    public const int MaxPeriodYear = 2040;

    public static bool IsPlausiblePeriod(DateOnly date)
        => date.Year is >= MinPeriodYear and <= MaxPeriodYear;

    public static DateOnly? TryParsePeriodDate(string? raw)
    {
        var d = WorkbookParseHelper.ParseDate(raw);
        return d is not null && IsPlausiblePeriod(d.Value) ? d : null;
    }

    public static IEnumerable<DateOnly> FilterPlausible(IEnumerable<DateOnly> dates)
        => dates.Where(IsPlausiblePeriod);

    public static bool IsNonPeriodDataColumn(string header)
    {
        var norm = WorkbookParseHelper.NormalizeHeader(header);
        if (norm.Length < 2) return true;

        string[] blocked =
        [
            "customer", "custid", "custname", "accountid", "accountname", "buyer", "endcustomer",
            "vendor", "supplier", "sku", "itemno", "itemnumber", "productid", "product",
            "invoice", "invno", "billid", "billno", "ponumber", "poid", "orderid", "ordernumber",
            "transactionid", "weeknumber", "weeknum", "fiscalyear", "fiscalweek", "fiscalperiod",
            "glaccount", "accountnumber", "postingid", "documentnumber", "referenceno", "refno",
            "quantity", "qty", "amount", "balance", "openbalance", "revenue", "sales", "cogs",
            "margin", "percent", "pct", "rate", "dayspast", "aging"
        ];

        return blocked.Any(b => norm.Contains(b, StringComparison.Ordinal));
    }
}
