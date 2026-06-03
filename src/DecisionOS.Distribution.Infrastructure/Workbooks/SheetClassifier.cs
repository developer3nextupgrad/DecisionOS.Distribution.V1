using DecisionOS.Distribution.Domain.Uploads;

namespace DecisionOS.Distribution.Infrastructure.Workbooks;

public static class SheetClassifier
{
    public static (WorkbookSheetKind Kind, ReportType? ReportType, double Confidence) Classify(
        string sheetName,
        IReadOnlyList<string> headers)
    {
        var norms = headers.Select(WorkbookParseHelper.NormalizeHeader).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var name = NormalizeSheetName(sheetName);
        var hasSkuColumn = norms.Contains("sku") || norms.Contains("skuid") || norms.Contains("itemno");

        double Score(params string[] tokens) =>
            tokens.Count(t => norms.Contains(t) || name.Contains(t, StringComparison.OrdinalIgnoreCase));

        var candidates = new List<(WorkbookSheetKind Kind, ReportType? Rt, double Score)>();

        if (name.Contains("readme") || name.Contains("source_note") || name.Contains("validation_summary") ||
            name.Contains("import_map") || name.Contains("changelog"))
            return (WorkbookSheetKind.Skip, null, 1.0);

        if (name.Contains("ar_aging") || name.Contains("ap_aging"))
            return (WorkbookSheetKind.WeeklyRollup, ReportType.FinancialStatement, 0.98);

        if (hasSkuColumn && (norms.Contains("netsales") || norms.Contains("quantitysold") ||
                             norms.Contains("weeknumber") || norms.Contains("weekenddate") ||
                             norms.Contains("cogs")) ||
            name.Contains("sales_by") || name.Contains("salesby") || (name.Contains("sales") && !name.Contains("history")))
            candidates.Add((WorkbookSheetKind.Sales, ReportType.Sales, 0.92));

        if (!hasSkuColumn && (
                name.Contains("weekly_financial") || name.Contains("weeklyfinancial") ||
                name.Contains("weekly_rollup") || name.Contains("weeklyrollup") ||
                name.Contains("ar_aging") || name.Contains("aragings") ||
                name.Contains("ap_aging") || name.Contains("apagings") ||
                (name.Contains("weekly") && norms.Contains("weekenddate")) ||
                (name.Contains("financial") && (norms.Contains("netsales") || norms.Contains("grossmargin")))))
            candidates.Add((WorkbookSheetKind.WeeklyRollup, ReportType.FinancialStatement, 0.95));

        if (!hasSkuColumn && norms.Contains("weekenddate") &&
            (norms.Contains("artotal") || norms.Contains("arover90") || norms.Contains("arover60") ||
             norms.Contains("apover60") || norms.Contains("apover90") || norms.Contains("aptotal")))
            candidates.Add((WorkbookSheetKind.WeeklyRollup, ReportType.FinancialStatement, 0.88));

        if (!hasSkuColumn && Score("weekenddate", "netsales", "grossmargin", "cogs") >= 2)
            candidates.Add((WorkbookSheetKind.WeeklyRollup, ReportType.FinancialStatement, 0.9));

        if (Score("sku", "onhandunits", "inventoryvalue", "quantityonhand") >= 2 ||
            name.Contains("inventory") || name.Contains("stock_on_hand"))
            candidates.Add((WorkbookSheetKind.Inventory, ReportType.Inventory, 0.85));

        if (Score("invoiceid", "customerid", "openamount", "openbalance") >= 2 ||
            name.Contains("receivable") || name.StartsWith("ar") || name.Contains("_ar"))
            candidates.Add((WorkbookSheetKind.AccountsReceivable, ReportType.AccountsReceivable, 0.85));

        if (Score("billid", "vendorid", "openamount", "openbalance") >= 2 ||
            name.Contains("payable") || name.StartsWith("ap") || name.Contains("_ap"))
            candidates.Add((WorkbookSheetKind.AccountsPayable, ReportType.AccountsPayable, 0.85));

        if ((Score("customerid", "customername") >= 2 || name.Contains("customer_master") || name.Contains("customermaster")) &&
            !norms.Contains("openamount") && !norms.Contains("openbalance"))
            candidates.Add((WorkbookSheetKind.Customer, ReportType.Customer, 0.8));

        if (Score("vendorid", "vendorname", "ontime") >= 2 || name.Contains("vendor_master"))
            candidates.Add((WorkbookSheetKind.Vendor, ReportType.Vendor, 0.8));

        if (Score("sku", "description", "unitcost") >= 2 && (name.Contains("master") || name.Contains("product")))
            candidates.Add((WorkbookSheetKind.Product, ReportType.Product, 0.8));

        if ((Score("poid", "poamount") >= 1 && name.Contains("purchase")) ||
            (norms.Contains("poid") && norms.Contains("poamount") && !norms.Contains("billid")))
            candidates.Add((WorkbookSheetKind.Purchasing, ReportType.Purchasing, 0.85));

        if (Score("holdoverid", "completion", "owner") >= 2 || name.Contains("holdover"))
            candidates.Add((WorkbookSheetKind.Holdover, null, 0.9));

        if (Score("issueid", "severity", "issuetype") >= 2 || name.Contains("operational"))
            candidates.Add((WorkbookSheetKind.OperationalIssues, null, 0.85));

        if (Score("field", "value") >= 2 && name.Contains("company"))
            candidates.Add((WorkbookSheetKind.CompanyProfile, null, 0.9));

        if (name.Contains("kpi_target") || name.Contains("expected_kpi"))
            candidates.Add((WorkbookSheetKind.ExpectedKpiValidation, null, 0.85));

        if (name.Contains("decision_board") || name.Contains("expected_decision"))
            candidates.Add((WorkbookSheetKind.ExpectedDecisionBoard, null, 0.85));

        if (candidates.Count == 0)
            return (WorkbookSheetKind.Unknown, null, 0.3);

        var best = candidates.OrderByDescending(c => c.Score).First();
        return (best.Kind, best.Rt, Math.Min(1.0, best.Score / 3.0 + 0.4));
    }

    private static string NormalizeSheetName(string sheetName)
    {
        var lower = sheetName.Trim().ToLowerInvariant();
        var sb = new System.Text.StringBuilder();
        foreach (var ch in lower)
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            else if (ch is ' ' or '-' or '.' or '_') sb.Append('_');
        }
        return sb.ToString();
    }
}
