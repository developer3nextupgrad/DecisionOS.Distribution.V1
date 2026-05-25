using DecisionOS.Distribution.Domain.Uploads;

namespace DecisionOS.Distribution.Infrastructure.Workbooks;

public static class SheetClassifier
{
    public static (WorkbookSheetKind Kind, ReportType? ReportType, double Confidence) Classify(
        string sheetName,
        IReadOnlyList<string> headers)
    {
        var norms = headers.Select(WorkbookParseHelper.NormalizeHeader).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var name = sheetName.ToLowerInvariant();
        var hasSkuColumn = norms.Contains("sku");

        double Score(params string[] tokens) =>
            tokens.Count(t => norms.Contains(t) || name.Contains(t.Replace("_", ""), StringComparison.OrdinalIgnoreCase));

        var candidates = new List<(WorkbookSheetKind Kind, ReportType? Rt, double Score)>();

        if (hasSkuColumn && (norms.Contains("netsales") || norms.Contains("weeknumber") || norms.Contains("weekenddate")) ||
            name.Contains("sales_by") || name.Contains("salesby"))
            candidates.Add((WorkbookSheetKind.Sales, ReportType.Sales, 0.92));

        if (!hasSkuColumn && (
                name.Contains("weekly_financial") || name.Contains("weeklyfinancial") ||
                (name.Contains("weekly") && norms.Contains("weekenddate")) ||
                (name.Contains("financial") && norms.Contains("netsales") && norms.Contains("grossmargin"))))
            candidates.Add((WorkbookSheetKind.WeeklyRollup, ReportType.FinancialStatement, 0.95));

        if (!hasSkuColumn && Score("weekenddate", "netsales", "grossmargin") >= 2)
            candidates.Add((WorkbookSheetKind.WeeklyRollup, ReportType.FinancialStatement, 0.9));

        if (Score("sku", "onhandunits", "inventoryvalue") >= 2 || name.Contains("inventory"))
            candidates.Add((WorkbookSheetKind.Inventory, ReportType.Inventory, 0.85));

        if (Score("invoiceid", "customerid", "openamount") >= 2 ||
            name.Contains("receivable") || name.StartsWith("ar"))
            candidates.Add((WorkbookSheetKind.AccountsReceivable, ReportType.AccountsReceivable, 0.85));

        if (Score("billid", "vendorid", "openamount") >= 2 ||
            name.Contains("payable") || name.StartsWith("ap"))
            candidates.Add((WorkbookSheetKind.AccountsPayable, ReportType.AccountsPayable, 0.85));

        if (Score("customerid", "customername") >= 2 && !norms.Contains("openamount"))
            candidates.Add((WorkbookSheetKind.Customer, ReportType.Customer, 0.8));

        if (Score("vendorid", "vendorname", "ontime") >= 2)
            candidates.Add((WorkbookSheetKind.Vendor, ReportType.Vendor, 0.8));

        if (Score("sku", "description", "unitcost") >= 2 && name.Contains("master"))
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

        if (name.Contains("readme") || name.Contains("source_note") || name.Contains("validation_summary"))
            candidates.Add((WorkbookSheetKind.Skip, null, 1.0));

        if (name.Contains("kpi_target"))
            candidates.Add((WorkbookSheetKind.ExpectedKpiValidation, null, 0.85));

        if (name.Contains("decision_board"))
            candidates.Add((WorkbookSheetKind.ExpectedDecisionBoard, null, 0.85));

        if (candidates.Count == 0)
            return (WorkbookSheetKind.Unknown, null, 0.3);

        var best = candidates.OrderByDescending(c => c.Score).First();
        return (best.Kind, best.Rt, Math.Min(1.0, best.Score / 3.0 + 0.4));
    }
}
