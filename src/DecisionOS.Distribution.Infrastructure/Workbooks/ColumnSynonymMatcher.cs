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
        ["Inventory_Value"] = ["inventoryvalue", "inventory_value", "invvalue", "totalinventoryvalue"],
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
        ["Revenue"] = ["netsales", "net_sales", "revenue"],
        ["Period_End_Date"] = ["weekenddate", "week_end_date", "periodenddate", "period_end", "weekending"],
        ["Net_Income"] = ["netincome", "net_income", "netprofit"],
        ["Net_Profit_Percent"] = ["netprofit", "net_profit", "netprofitpercent", "netmargin"],
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

        ApplyRollupExtras(normalizedHeaders, result);
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
        List<(string Original, string Norm)> normalizedHeaders,
        Dictionary<string, string> result)
    {
        foreach (var (orig, norm) in normalizedHeaders)
        {
            if (result.ContainsKey(orig)) continue;
            if (norm.Contains("arover60", StringComparison.Ordinal) || norm.Contains("arpastdue31", StringComparison.Ordinal))
                result[orig] = "AR_Over_60_Pct";
            else if (norm.Contains("appastdue", StringComparison.Ordinal))
                result[orig] = "AP_Past_Due_Pct";
            else if (norm.Contains("fillrate", StringComparison.Ordinal) || norm.Contains("perfectorder", StringComparison.Ordinal))
                result[orig] = "Fill_Rate_Pct";
            else if (norm.Contains("cashending", StringComparison.Ordinal) || norm.Contains("cashbalance", StringComparison.Ordinal))
                result[orig] = "Cash_Balance";
            else if (norm.Contains("weekend", StringComparison.Ordinal) || norm.Contains("periodend", StringComparison.Ordinal))
                result[orig] = "Period_End_Date";
        }
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
            "Net_Income", "Net_Profit_Percent", "Revenue"
        },
        _ => SystemFields.Generic
    };

    public static string? GetMapped(IReadOnlyDictionary<string, string?> row, IReadOnlyDictionary<string, string> colMap, string systemField)
    {
        var source = colMap.FirstOrDefault(kvp =>
            string.Equals(kvp.Value, systemField, StringComparison.OrdinalIgnoreCase)).Key;
        if (source is null) return null;
        return row.TryGetValue(source, out var v) ? v : null;
    }

    public static DateOnly? ResolveRowPeriod(
        IReadOnlyDictionary<string, string?> row,
        IReadOnlyDictionary<string, string> colMap)
    {
        foreach (var field in new[] { "Transaction_Date", "Period_End_Date", "AR_Snapshot_Date", "AP_Snapshot_Date", "Snapshot_Date" })
        {
            var d = WorkbookParseHelper.ParseDate(GetMapped(row, colMap, field));
            if (d is not null) return d;
        }
        return null;
    }
}
