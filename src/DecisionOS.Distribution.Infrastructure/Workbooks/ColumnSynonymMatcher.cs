using DecisionOS.Distribution.Domain.Uploads;

namespace DecisionOS.Distribution.Infrastructure.Workbooks;

public static class ColumnSynonymMatcher
{
    private static readonly Dictionary<string, string[]> Synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Transaction_Date"] = ["transactiondate", "weekenddate", "week_end_date", "weekending", "periodenddate", "invoicedate", "invoice_date", "podate", "po_date", "fiscalweekend"],
        ["Quantity_Sold"] = ["quantitysold", "qtysold", "qty_sold", "unitssold", "units_sold", "orderedquantity", "qty", "quantity"],
        ["Net_Sales"] = ["netsales", "net_sales", "netsale", "revenue", "netsalesamount"],
        ["COGS"] = ["cogs", "costofgoodssold", "cost_of_goods_sold", "costofsales"],
        ["Gross_Sales"] = ["grosssales", "gross_sales"],
        ["Discount_Amount"] = ["discountamount", "discount_amount", "discounts"],
        ["Gross_Profit"] = ["grossprofit", "gross_profit"],
        ["Gross_Margin_Percent"] = ["grossmargin", "gross_margin", "grossmarginpercent", "gross_margin_percent", "grossmarginpct", "gross_margin_", "gmpercent", "gm"],
        ["Customer_ID"] = ["customerid", "customer_id", "custid", "cust_id", "accountid"],
        ["Customer_Name"] = ["customername", "customer_name", "custname", "accountname"],
        ["SKU_ID"] = ["skuid", "sku_id", "sku", "itemno", "itemnumber", "productid", "product_id", "item"],
        ["Product_Description"] = ["description", "productdescription", "product_description", "itemdescription"],
        ["Snapshot_Date"] = ["snapshotdate", "snapshot_date", "weekenddate", "week_end_date", "asofdate", "inventorydate"],
        ["Quantity_On_Hand"] = ["quantityonhand", "onhandunits", "on_hand_units", "quantity_on_hand", "qoh", "qtyonhand"],
        ["Inventory_Value"] = ["inventoryvalue", "inventory_value", "invvalue", "totalinventoryvalue", "inventoryvalueend", "inventory_value_end"],
        ["Invoice_ID"] = ["invoiceid", "invoice_id", "invno", "invoiceno"],
        ["Invoice_Date"] = ["invoicedate", "invoice_date"],
        ["Due_Date"] = ["duedate", "due_date"],
        ["Invoice_Amount"] = ["invoiceamount", "originalamount", "original_amount", "invoiceamt"],
        ["Open_Balance"] = ["openbalance", "openamount", "open_amount", "open_balance", "balance", "amountdue"],
        ["Aging_Bucket"] = ["agingbucket", "aging_bucket", "aging", "agebucket"],
        ["Days_Past_Due"] = ["dayspastdue", "agedays", "age_days", "dayspast"],
        ["Bill_ID"] = ["billid", "bill_id"],
        ["Bill_Date"] = ["billdate", "bill_date"],
        ["Bill_Amount"] = ["billamount", "bill_amount"],
        ["Vendor_ID"] = ["vendorid", "vendor_id", "supplierid"],
        ["Vendor_Name"] = ["vendorname", "vendor_name", "suppliername"],
        ["PO_ID"] = ["poid", "po_id", "ponumber"],
        ["PO_Date"] = ["podate", "po_date"],
        ["PO_Amount"] = ["poamount", "po_amount"],
        ["Expected_Receipt_Date"] = ["expecteddate", "expected_date", "expectedreceipt"],
        ["PO_Status"] = ["status", "po_status"],
        ["AR_Snapshot_Date"] = ["snapshotdate", "weekenddate", "week_end_date", "arsnapshotdate"],
        ["AP_Snapshot_Date"] = ["snapshotdate", "weekenddate", "week_end_date", "apsnapshotdate"],
        ["AR_Over_60_Pct"] = ["arover60", "ar_over_60", "ar_over_60_", "arpastdue", "arpastduepercent"],
        ["AP_Past_Due_Pct"] = ["appastdue", "ap_past_due", "ap_past_due_", "appastduepercent"],
        ["Fill_Rate_Pct"] = ["fillrate", "fill_rate", "fill_rate_", "perfectorder", "perfectorderrate"],
        ["Cash_Balance"] = ["cashending", "cash_ending", "cashbalance", "endingcash"],
        ["AR_Balance"] = ["arending", "ar_ending", "artotal", "ar_total", "arbalance"],
        ["AP_Balance"] = ["apending", "ap_ending", "aptotal", "ap_total", "apbalance"],
        ["Revenue"] = ["netsales", "net_sales", "revenue"],
        ["Period_End_Date"] = ["weekenddate", "week_end_date", "periodenddate", "period_end", "weekending"],
        ["Net_Income"] = ["netincome", "net_income", "netincomeamount", "netprofitdollars"],
        ["Net_Profit_Percent"] = ["netprofitpercent", "net_profit_percent", "netprofitpct", "netmargin", "netmarginpercent", "net_profit_"],
        ["Operating_Profit"] = ["operatingprofit", "operating_profit", "operatingincome", "operating_income", "ebit"],
        ["Operating_Expenses"] = ["operatingexpenses", "opex", "operating_expenses"],
    };

    public static HashSet<string> BuildKnownHeaderNormSet()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var syns in Synonyms.Values)
        {
            foreach (var s in syns) set.Add(s);
        }

        foreach (var list in new[]
                 {
                     SystemFields.Sales, SystemFields.Inventory, SystemFields.AccountsReceivable,
                     SystemFields.AccountsPayable, SystemFields.FinancialStatement, SystemFields.Customer,
                     SystemFields.Vendor, SystemFields.Product
                 })
        {
            foreach (var f in list)
                set.Add(WorkbookParseHelper.NormalizeHeader(f));
        }

        return set;
    }

    public static IReadOnlyDictionary<string, string> InferMappings(
        IReadOnlyList<string> headers,
        WorkbookSheetKind kind)
    {
        var systemFields = SystemFieldsForKind(kind);
        var normalizedHeaders = headers
            .Select(h => (Original: h, Norm: WorkbookParseHelper.NormalizeHeader(h)))
            .Where(x => x.Norm.Length >= 2)
            .ToList();

        var scores = new List<(string Header, string SystemField, int Score)>();
        foreach (var sys in systemFields)
        {
            var syns = GetSynonyms(sys);
            foreach (var (orig, norm) in normalizedHeaders)
            {
                var score = ScoreMatch(norm, syns);
                if (score > 0)
                    scores.Add((orig, sys, score));
            }
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in scores.GroupBy(s => s.Header, StringComparer.OrdinalIgnoreCase).OrderByDescending(g => g.Max(x => x.Score)))
        {
            var best = group.OrderByDescending(x => x.Score).First();
            if (!result.ContainsKey(best.Header))
                result[best.Header] = best.SystemField;
        }

        ApplyRollupExtras(kind, normalizedHeaders, result);
        ApplyProfitabilityDisambiguation(normalizedHeaders, result);
        if (kind == WorkbookSheetKind.WeeklyRollup)
        {
            PreferCanonicalDuplicates(result);
            StripCashFlowChangeBalances(normalizedHeaders, result);
        }
        return result;
    }

    private static int ScoreMatch(string headerNorm, string[] syns)
    {
        var best = 0;
        foreach (var syn in syns)
        {
            if (headerNorm.Equals(syn, StringComparison.OrdinalIgnoreCase))
                return 100;
            if (headerNorm.Contains(syn, StringComparison.Ordinal) || syn.Contains(headerNorm, StringComparison.Ordinal))
                best = Math.Max(best, 70);
            else if (headerNorm.StartsWith(syn, StringComparison.Ordinal) || syn.StartsWith(headerNorm, StringComparison.Ordinal))
                best = Math.Max(best, 50);
        }
        return best;
    }

    private static string[] GetSynonyms(string sys) =>
        Synonyms.TryGetValue(sys, out var syns)
            ? syns
            : [WorkbookParseHelper.NormalizeHeader(sys)];

    private static void ApplyRollupExtras(
        WorkbookSheetKind kind,
        List<(string Original, string Norm)> normalizedHeaders,
        Dictionary<string, string> result)
    {
        foreach (var (orig, norm) in normalizedHeaders)
        {
            if (result.ContainsKey(orig)) continue;
            if (norm.Contains("arover60", StringComparison.Ordinal) || norm.Contains("arover90", StringComparison.Ordinal) ||
                norm.Contains("arpastdue31", StringComparison.Ordinal) || norm.Contains("arpastdue", StringComparison.Ordinal))
                result[orig] = "AR_Over_60_Pct";
            else if (norm.Contains("apover60", StringComparison.Ordinal) || norm.Contains("apover90", StringComparison.Ordinal) ||
                     norm.Contains("appastdue", StringComparison.Ordinal))
                result[orig] = "AP_Past_Due_Pct";
            else if (norm.Contains("fillrate", StringComparison.Ordinal) || norm.Contains("perfectorder", StringComparison.Ordinal))
                result[orig] = "Fill_Rate_Pct";
            else if (norm.Contains("cashending", StringComparison.Ordinal) || norm.Contains("cashbalance", StringComparison.Ordinal))
                result[orig] = "Cash_Balance";
            else if (norm.Contains("weekend", StringComparison.Ordinal) || norm.Contains("periodend", StringComparison.Ordinal))
                result[orig] = "Period_End_Date";
            else if (kind == WorkbookSheetKind.WeeklyRollup && IsQbDecisionOsRollupHeader(norm, out var qbField))
                result[orig] = qbField;
            else if (norm.Contains("inventoryvalue", StringComparison.Ordinal))
                result[orig] = "Inventory_Value";
            else if (norm is "arending" or "artotal" or "arbalance" ||
                     (norm.StartsWith("ar", StringComparison.Ordinal) && norm.EndsWith("ending", StringComparison.Ordinal)))
                result[orig] = "AR_Balance";
            else if (norm is "apending" or "aptotal" or "apbalance" ||
                     (norm.StartsWith("ap", StringComparison.Ordinal) && norm.EndsWith("ending", StringComparison.Ordinal)))
                result[orig] = "AP_Balance";
            else if (norm.Contains("netprofit", StringComparison.Ordinal) || norm.Contains("netincome", StringComparison.Ordinal) ||
                     norm.Contains("operatingprofit", StringComparison.Ordinal) || norm.Contains("operatingincome", StringComparison.Ordinal))
                result[orig] = InferProfitabilityField(norm);
        }
    }

    /// <summary>
    /// Exact QuickBooks Decision-OS titles. Kept out of the global synonym dictionary so
    /// header detection cannot match short fragments like "end", "income", or "payable" in data cells.
    /// </summary>
    private static bool IsQbDecisionOsRollupHeader(string norm, out string field)
    {
        switch (norm)
        {
            case "date":
                field = "Period_End_Date";
                return true;
            case "totalincome":
                field = "Net_Sales";
                return true;
            case "totalcogs":
                field = "COGS";
                return true;
            case "cashatendofperiod":
            case "totalcheckingsavings":
                field = "Cash_Balance";
                return true;
            case "inventoryks":
            case "1100inventoryks":
                field = "Inventory_Value";
                return true;
            case "accountspayable":
            case "totalaccountspayable":
                field = "AP_Balance";
                return true;
        }

        if (IsQbArKsHeader(norm))
        {
            field = "AR_Balance";
            return true;
        }

        if (norm.EndsWith("inventoryks", StringComparison.Ordinal) &&
            (char.IsDigit(norm[0]) || norm.Contains("inventory", StringComparison.Ordinal)))
        {
            field = "Inventory_Value";
            return true;
        }

        if (norm.Contains("accountspayable", StringComparison.Ordinal))
        {
            field = "AP_Balance";
            return true;
        }

        field = "";
        return false;
    }

    /// <summary>QuickBooks Decision-OS "1000 AR-KS" (not a global "arks" synonym — that would match "remarks").</summary>
    private static bool IsQbArKsHeader(string norm) =>
        norm is "arks" or "1000arks" ||
        (norm.EndsWith("arks", StringComparison.Ordinal) &&
         (char.IsDigit(norm[0]) || norm.StartsWith("ar", StringComparison.Ordinal)));

    private static readonly HashSet<string> CanonicalDedupeFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "Net_Sales", "COGS", "Cash_Balance", "AR_Balance", "AP_Balance", "Inventory_Value", "Gross_Profit"
    };

    /// <summary>
    /// When several columns map to one rollup field (line-item COGS vs Total COGS), keep the Total* / canonical header.
    /// Date fields are excluded — a bare "Date" must not replace Week_End_Date.
    /// </summary>
    private static void PreferCanonicalDuplicates(Dictionary<string, string> result)
    {
        var duplicateFields = result
            .Where(kv => CanonicalDedupeFields.Contains(kv.Value))
            .GroupBy(kv => kv.Value, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in duplicateFields)
        {
            var ranked = group
                .Select(kv => (kv.Key, Score: CanonicalHeaderPreference(WorkbookParseHelper.NormalizeHeader(kv.Key))))
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Key.Length)
                .ToList();
            foreach (var extra in ranked.Skip(1))
                result.Remove(extra.Key);
        }
    }

    private static int CanonicalHeaderPreference(string norm)
    {
        var score = 0;
        if (norm.StartsWith("total", StringComparison.Ordinal)) score += 50;
        if (norm.Contains("total", StringComparison.Ordinal)) score += 20;
        if (norm is "totalcogs" or "totalincome" or "totalaccountspayable" or "totalcheckingsavings"
            or "cashatendofperiod" or "grossprofit" or "netincome")
            score += 40;
        if (norm.Length > 0 && char.IsDigit(norm[0])) score -= 40;
        return score;
    }

    /// <summary>
    /// Cash-flow "1000 AR-KS" / inventory / AP columns are period changes, not ending balances.
    /// Leave those unmapped so a later BS tab cannot be overwritten with flow amounts.
    /// </summary>
    private static void StripCashFlowChangeBalances(
        List<(string Original, string Norm)> normalizedHeaders,
        Dictionary<string, string> result)
    {
        var norms = normalizedHeaders.Select(x => x.Norm).ToHashSet(StringComparer.Ordinal);
        var looksLikeCashFlow =
            norms.Contains("cashatendofperiod") ||
            norms.Contains("cashatbeginningofperiod") ||
            norms.Contains("netcashincreaseforperiod");
        if (!looksLikeCashFlow) return;
        if (norms.Contains("totalassets") || norms.Contains("totalcheckingsavings") ||
            norms.Contains("totalaccountspayable"))
            return;

        foreach (var header in result
                     .Where(kv => kv.Value is "AR_Balance" or "AP_Balance" or "Inventory_Value")
                     .Select(kv => kv.Key)
                     .ToList())
            result.Remove(header);
    }

    private static void ApplyProfitabilityDisambiguation(
        List<(string Original, string Norm)> normalizedHeaders,
        Dictionary<string, string> result)
    {
        foreach (var (orig, norm) in normalizedHeaders)
        {
            if (!result.TryGetValue(orig, out var mapped)) continue;
            if (!mapped.Equals("Net_Income", StringComparison.OrdinalIgnoreCase) &&
                !mapped.Equals("Net_Profit_Percent", StringComparison.OrdinalIgnoreCase) &&
                !mapped.Equals("Operating_Profit", StringComparison.OrdinalIgnoreCase))
                continue;

            result[orig] = InferProfitabilityField(norm);
        }
    }

    private static string InferProfitabilityField(string norm)
    {
        var looksPercent = norm.Contains("percent", StringComparison.Ordinal) ||
                           norm.Contains("pct", StringComparison.Ordinal) ||
                           norm.Contains("margin", StringComparison.Ordinal);

        if (norm.Contains("operating", StringComparison.Ordinal))
            return looksPercent ? "Net_Profit_Percent" : "Operating_Profit";

        if (norm.Contains("netincome", StringComparison.Ordinal))
            return looksPercent ? "Net_Profit_Percent" : "Net_Income";

        if (norm.Contains("netprofit", StringComparison.Ordinal))
        {
            if (norm.Contains("dollar", StringComparison.Ordinal) || norm.Contains("amount", StringComparison.Ordinal))
                return "Net_Income";
            return "Net_Profit_Percent";
        }

        return "Net_Profit_Percent";
    }

    private static IReadOnlyList<string> SystemFieldsForKind(WorkbookSheetKind kind) => kind switch
    {
        WorkbookSheetKind.Sales => SystemFields.Sales,
        WorkbookSheetKind.Inventory => SystemFields.Inventory,
        WorkbookSheetKind.AccountsReceivable => SystemFields.AccountsReceivable,
        WorkbookSheetKind.AccountsPayable => SystemFields.AccountsPayable,
        WorkbookSheetKind.Customer => SystemFields.Customer,
        WorkbookSheetKind.Vendor => SystemFields.Vendor,
        WorkbookSheetKind.Product => SystemFields.Product,
        WorkbookSheetKind.Purchasing => SystemFields.Purchasing,
        WorkbookSheetKind.WeeklyRollup => new[]
        {
            "Period_End_Date", "Net_Sales", "COGS", "Gross_Margin_Percent", "Gross_Profit",
            "AR_Over_60_Pct", "AP_Past_Due_Pct", "Fill_Rate_Pct", "Cash_Balance", "Inventory_Value",
            "AR_Balance", "AP_Balance",
            "Net_Income", "Net_Profit_Percent", "Operating_Profit", "Revenue"
        },
        WorkbookSheetKind.Holdover => new[] { "Customer_ID", "Customer_Name" },
        _ => SystemFields.Generic
    };

    public static string? GetMapped(IReadOnlyDictionary<string, string?> row, IReadOnlyDictionary<string, string> colMap, string systemField)
    {
        var source = colMap.FirstOrDefault(kvp =>
            string.Equals(kvp.Value, systemField, StringComparison.OrdinalIgnoreCase)).Key;
        if (source is null) return null;
        return row.TryGetValue(source, out var v) ? v : null;
    }

    private static readonly string[] PeriodSystemFields =
    [
        "Transaction_Date", "Period_End_Date", "AR_Snapshot_Date", "AP_Snapshot_Date", "Snapshot_Date"
    ];

    public static bool MapsAnyPeriodField(IReadOnlyDictionary<string, string> colMap)
        => colMap.Values.Any(v => PeriodSystemFields.Contains(v, StringComparer.OrdinalIgnoreCase));

    public static DateOnly? ResolveRowPeriod(
        IReadOnlyDictionary<string, string?> row,
        IReadOnlyDictionary<string, string> colMap)
    {
        foreach (var field in PeriodSystemFields)
        {
            var d = WorkbookDateRules.TryParsePeriodDate(GetMapped(row, colMap, field));
            if (d is not null) return d;
        }
        return null;
    }
}
