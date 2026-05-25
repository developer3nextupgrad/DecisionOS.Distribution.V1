using DecisionOS.Distribution.Domain.Uploads;

namespace DecisionOS.Distribution.Infrastructure.Workbooks;

public static class ColumnSynonymMatcher
{
    private static readonly Dictionary<string, string[]> Synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Transaction_Date"] = ["transactiondate", "weekenddate", "week_end_date", "invoicedate", "invoice_date", "podate", "po_date"],
        ["Quantity_Sold"] = ["quantitysold", "unitssold", "units_sold", "orderedquantity"],
        ["Net_Sales"] = ["netsales", "net_sales"],
        ["COGS"] = ["cogs", "costofgoodssold"],
        ["Gross_Sales"] = ["grosssales", "gross_sales"],
        ["Discount_Amount"] = ["discountamount", "discount_amount"],
        ["Gross_Profit"] = ["grossprofit", "gross_profit"],
        ["Gross_Margin_Percent"] = ["grossmargin", "gross_margin", "grossmarginpercent", "gross_margin_percent", "gross_margin_"],
        ["Customer_ID"] = ["customerid", "customer_id"],
        ["Customer_Name"] = ["customername", "customer_name"],
        ["SKU_ID"] = ["skuid", "sku_id", "sku"],
        ["Product_Description"] = ["description", "productdescription", "product_description"],
        ["Snapshot_Date"] = ["snapshotdate", "snapshot_date", "weekenddate", "week_end_date"],
        ["Quantity_On_Hand"] = ["quantityonhand", "onhandunits", "on_hand_units", "quantity_on_hand"],
        ["Inventory_Value"] = ["inventoryvalue", "inventory_value"],
        ["Invoice_ID"] = ["invoiceid", "invoice_id"],
        ["Invoice_Date"] = ["invoicedate", "invoice_date"],
        ["Due_Date"] = ["duedate", "due_date"],
        ["Invoice_Amount"] = ["invoiceamount", "originalamount", "original_amount"],
        ["Open_Balance"] = ["openbalance", "openamount", "open_amount", "open_balance"],
        ["Aging_Bucket"] = ["agingbucket", "aging_bucket"],
        ["Days_Past_Due"] = ["dayspastdue", "agedays", "age_days"],
        ["Bill_ID"] = ["billid", "bill_id"],
        ["Bill_Date"] = ["billdate", "bill_date"],
        ["Bill_Amount"] = ["billamount", "originalamount"],
        ["Bill_Amount_Alt"] = ["billamount"],
        ["Vendor_ID"] = ["vendorid", "vendor_id"],
        ["Vendor_Name"] = ["vendorname", "vendor_name"],
        ["PO_ID"] = ["poid", "po_id"],
        ["PO_Date"] = ["podate", "po_date"],
        ["PO_Amount"] = ["poamount", "po_amount"],
        ["Expected_Receipt_Date"] = ["expecteddate", "expected_date"],
        ["PO_Status"] = ["status", "po_status"],
        ["AR_Snapshot_Date"] = ["snapshotdate", "weekenddate"],
        ["AP_Snapshot_Date"] = ["snapshotdate", "weekenddate"],
        ["AR_Over_60_Pct"] = ["arover60", "ar_over_60", "ar_over_60_"],
        ["AP_Past_Due_Pct"] = ["appastdue", "ap_past_due", "ap_past_due_"],
        ["Fill_Rate_Pct"] = ["fillrate", "fill_rate", "fill_rate_"],
        ["Cash_Balance"] = ["cashending", "cash_ending", "cashbalance"],
        ["Revenue"] = ["netsales", "net_sales"],
        ["Period_End_Date"] = ["weekenddate", "week_end_date"],
    };

    public static IReadOnlyDictionary<string, string> InferMappings(
        IReadOnlyList<string> headers,
        WorkbookSheetKind kind)
    {
        var systemFields = SystemFieldsForKind(kind);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var normalizedHeaders = headers
            .Select(h => (Original: h, Norm: WorkbookParseHelper.NormalizeHeader(h)))
            .ToList();

        foreach (var sys in systemFields)
        {
            if (!Synonyms.TryGetValue(sys, out var syns))
                syns = [WorkbookParseHelper.NormalizeHeader(sys)];

            string? best = null;
            foreach (var (orig, norm) in normalizedHeaders)
            {
                if (syns.Any(s => norm.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                                  s.Contains(norm, StringComparison.OrdinalIgnoreCase)))
                {
                    best = orig;
                    break;
                }
            }

            if (best is not null && !result.ContainsKey(best))
                result[best] = sys;
        }

        // Special workbook columns not in SystemFields
        foreach (var (orig, norm) in normalizedHeaders)
        {
            if (result.ContainsKey(orig)) continue;
            if (norm.Contains("arover60") || norm.Contains("arover60"))
                result[orig] = "AR_Over_60_Pct";
            if (norm.Contains("appastdue"))
                result[orig] = "AP_Past_Due_Pct";
            if (norm.Contains("fillrate"))
                result[orig] = "Fill_Rate_Pct";
            if (norm.Contains("cashending") || norm.Contains("cashending"))
                result[orig] = "Cash_Balance";
            if (norm.Contains("weekenddate"))
                result[orig] = "Period_End_Date";
        }

        return result;
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
            "Period_End_Date", "Net_Sales", "COGS", "Gross_Margin_Percent",
            "AR_Over_60_Pct", "AP_Past_Due_Pct", "Fill_Rate_Pct", "Cash_Balance", "Inventory_Value"
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
}
