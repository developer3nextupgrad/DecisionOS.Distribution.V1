using DecisionOS.Distribution.Domain.Uploads;

namespace DecisionOS.Distribution.Infrastructure.Workbooks;

public static class KpiCoveragePreviewBuilder
{
    private static readonly string[] StandardKpiCodes =
    [
        "GrossMargin%",
        "AR_PastDue31p%",
        "AP_PastDue31p%",
        "DOH",
        "CCC",
        "NetProfit%",
        "PerfectOrderRate"
    ];

    private static readonly Dictionary<string, string> DisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["GrossMargin%"] = "Gross Margin %",
        ["AR_PastDue31p%"] = "AR Past Due 31+ %",
        ["AP_PastDue31p%"] = "AP Past Due 31+ %",
        ["DOH"] = "Days on Hand",
        ["CCC"] = "Cash Conversion Cycle",
        ["NetProfit%"] = "Net Profit %",
        ["PerfectOrderRate"] = "Perfect Order Rate"
    };

    public static IReadOnlyList<KpiCoverageLine> Build(
        WorkbookDetectionResult detection,
        IReadOnlySet<string>? existingKpiCodes = null)
    {
        var sheets = detection.Sheets
            .Where(s => s.Kind is not WorkbookSheetKind.Skip and not WorkbookSheetKind.Unknown)
            .ToList();

        var rollup = sheets.FirstOrDefault(s => s.Kind == WorkbookSheetKind.WeeklyRollup);
        var sales = sheets.FirstOrDefault(s => s.Kind == WorkbookSheetKind.Sales);
        var ar = sheets.FirstOrDefault(s => s.Kind == WorkbookSheetKind.AccountsReceivable);
        var ap = sheets.FirstOrDefault(s => s.Kind == WorkbookSheetKind.AccountsPayable);
        var inv = sheets.FirstOrDefault(s => s.Kind == WorkbookSheetKind.Inventory);
        var vendor = sheets.FirstOrDefault(s => s.Kind == WorkbookSheetKind.Vendor);

        var lines = new List<KpiCoverageLine>();

        lines.Add(BuildGrossMargin(rollup, sales, existingKpiCodes));
        lines.Add(BuildArPastDue(rollup, ar, existingKpiCodes));
        lines.Add(BuildApPastDue(rollup, ap, existingKpiCodes));
        lines.Add(BuildDoh(rollup, sales, inv, existingKpiCodes));
        lines.Add(BuildCcc(lines));
        lines.Add(BuildNetProfit(rollup, existingKpiCodes));
        lines.Add(BuildPerfectOrder(rollup, vendor, existingKpiCodes));

        foreach (var extra in DiscoverWorkbookKpiCodes(sheets).Except(StandardKpiCodes, StringComparer.OrdinalIgnoreCase))
        {
            if (lines.Any(l => string.Equals(l.KpiCode, extra, StringComparison.OrdinalIgnoreCase)))
                continue;

            lines.Add(new KpiCoverageLine
            {
                KpiCode = extra,
                DisplayName = extra,
                Status = existingKpiCodes is not null && existingKpiCodes.Contains(extra)
                    ? KpiCoverageStatus.ReadyFromRollup
                    : KpiCoverageStatus.NotInSystem,
                SourceSummary = "Found in workbook column or KPI sheet",
                SuggestedFix = existingKpiCodes is null || !existingKpiCodes.Contains(extra)
                    ? "Will be auto-created as a KPI definition on import if mapped in rollup."
                    : null,
                ExistsInDatabase = existingKpiCodes?.Contains(extra) ?? false
            });
        }

        return lines;
    }

    private static KpiCoverageLine BuildGrossMargin(
        DetectedSheet? rollup,
        DetectedSheet? sales,
        IReadOnlySet<string>? existing)
    {
        if (rollup is not null && HasMapped(rollup, "Gross_Margin_Percent"))
            return Line("GrossMargin%", KpiCoverageStatus.ReadyFromRollup,
                $"Rollup sheet '{rollup.SheetName}' → Gross_Margin_Percent", existing);

        if (rollup is not null && HasMapped(rollup, "Net_Sales") && HasMapped(rollup, "COGS"))
            return Line("GrossMargin%", KpiCoverageStatus.ReadyFromRollup,
                $"Rollup '{rollup.SheetName}' (Net_Sales + COGS)", existing);

        if (sales is not null && HasMapped(sales, "Net_Sales") && HasMapped(sales, "COGS"))
            return Line("GrossMargin%", KpiCoverageStatus.ReadyFromDetail,
                $"Sales sheet '{sales.SheetName}'", existing);

        return Line("GrossMargin%", KpiCoverageStatus.MissingExpectGray,
            "No rollup margin or sales+COGS mapping",
            existing,
            "Map Gross_Margin_% on weekly rollup or add sales detail with Net_Sales and COGS.");
    }

    private static KpiCoverageLine BuildArPastDue(
        DetectedSheet? rollup,
        DetectedSheet? ar,
        IReadOnlySet<string>? existing)
    {
        if (rollup is not null && HasMapped(rollup, "AR_Over_60_Pct"))
            return Line("AR_PastDue31p%", KpiCoverageStatus.ReadyFromRollup,
                $"Rollup '{rollup.SheetName}' → AR_Over_60_Pct (31+ approximation)", existing);

        if (ar is not null && HasMapped(ar, "Open_Balance"))
            return Line("AR_PastDue31p%", KpiCoverageStatus.ReadyFromDetail,
                $"AR sheet '{ar.SheetName}'", existing);

        return Line("AR_PastDue31p%", KpiCoverageStatus.MissingExpectGray,
            "No AR rollup % or AR open-balance detail",
            existing,
            "Map AR_Over_60/90 on weekly rollup or AR aging sheet, or add AR detail.");
    }

    private static KpiCoverageLine BuildApPastDue(
        DetectedSheet? rollup,
        DetectedSheet? ap,
        IReadOnlySet<string>? existing)
    {
        if (rollup is not null && HasMapped(rollup, "AP_Past_Due_Pct"))
            return Line("AP_PastDue31p%", KpiCoverageStatus.ReadyFromRollup,
                $"Rollup '{rollup.SheetName}' → AP_Past_Due_Pct", existing);

        if (ap is not null && HasMapped(ap, "Open_Balance"))
            return Line("AP_PastDue31p%", KpiCoverageStatus.ReadyFromDetail,
                $"AP sheet '{ap.SheetName}'", existing);

        return Line("AP_PastDue31p%", KpiCoverageStatus.MissingExpectGray,
            "No AP rollup % or AP open-balance detail",
            existing,
            "Map AP past-due % on rollup or add AP snapshot sheet.");
    }

    private static KpiCoverageLine BuildDoh(
        DetectedSheet? rollup,
        DetectedSheet? sales,
        DetectedSheet? inv,
        IReadOnlySet<string>? existing)
    {
        if (inv is not null && HasMapped(inv, "Inventory_Value") &&
            sales is not null && HasMapped(sales, "COGS"))
            return Line("DOH", KpiCoverageStatus.ReadyFromDetail,
                $"Inventory '{inv.SheetName}' + sales COGS", existing);

        if (rollup is not null && HasMapped(rollup, "Inventory_Value") &&
            HasMapped(rollup, "COGS"))
            return Line("DOH", KpiCoverageStatus.ReadyFromRollup,
                $"Rollup '{rollup.SheetName}' (inventory + COGS)", existing);

        if (rollup is not null && HasMapped(rollup, "Inventory_Value") &&
            sales is not null && HasMapped(sales, "COGS"))
            return Line("DOH", KpiCoverageStatus.ReadyFromRollup,
                $"Rollup inventory + sales COGS", existing);

        if (inv is not null && HasMapped(inv, "Inventory_Value"))
            return Line("DOH", KpiCoverageStatus.DependsOnOther,
                $"Inventory '{inv.SheetName}' — needs sales COGS for same week",
                existing,
                "Add sales COGS or weekly rollup with COGS.");

        return Line("DOH", KpiCoverageStatus.MissingExpectGray,
            "No inventory value + COGS combination",
            existing,
            "Add inventory snapshot or map Inventory_Value on weekly rollup with COGS.");
    }

    private static KpiCoverageLine BuildCcc(IReadOnlyList<KpiCoverageLine> prior)
    {
        var gm = prior.First(l => l.KpiCode == "GrossMargin%");
        var ar = prior.First(l => l.KpiCode == "AR_PastDue31p%");
        var ap = prior.First(l => l.KpiCode == "AP_PastDue31p%");
        var doh = prior.First(l => l.KpiCode == "DOH");

        var needsSales = gm.Status is KpiCoverageStatus.ReadyFromDetail or KpiCoverageStatus.ReadyFromRollup;
        var needsAr = ar.Status is KpiCoverageStatus.ReadyFromDetail or KpiCoverageStatus.ReadyFromRollup;
        var needsAp = ap.Status is KpiCoverageStatus.ReadyFromDetail or KpiCoverageStatus.ReadyFromRollup;
        var needsDoh = doh.Status is KpiCoverageStatus.ReadyFromDetail or KpiCoverageStatus.ReadyFromRollup;

        if (needsSales && needsAr && needsAp && needsDoh)
            return new KpiCoverageLine
            {
                KpiCode = "CCC",
                DisplayName = DisplayNames["CCC"],
                Status = KpiCoverageStatus.ReadyFromDetail,
                SourceSummary = "Computed from sales, AR/AP balances, and inventory (rollup or detail sheets)",
                ExistsInDatabase = true
            };

        return new KpiCoverageLine
        {
            KpiCode = "CCC",
            DisplayName = DisplayNames["CCC"],
            Status = KpiCoverageStatus.DependsOnOther,
            SourceSummary = "Requires sales/COGS, AR balance, AP balance, and inventory for the week",
            SuggestedFix = "Map AR_Ending and AP_Ending on weekly rollup, or provide AR/AP detail with open balances.",
            ExistsInDatabase = true
        };
    }

    private static KpiCoverageLine BuildNetProfit(DetectedSheet? rollup, IReadOnlySet<string>? existing)
    {
        if (rollup is not null && (HasMapped(rollup, "Net_Profit_Percent") || HasMapped(rollup, "Net_Income") || HasMapped(rollup, "Operating_Profit")))
            return Line("NetProfit%", KpiCoverageStatus.ReadyFromRollup,
                $"Rollup '{rollup.SheetName}'", existing);

        return Line("NetProfit%", KpiCoverageStatus.MissingExpectGray,
            "No net profit / net income / operating profit on rollup",
            existing,
            "Cannot derive from Gross Profit or sales alone — add Net_Profit_%, Net_Income ($), or Operating_Profit ($) on weekly financials.");
    }

    private static KpiCoverageLine BuildPerfectOrder(
        DetectedSheet? rollup,
        DetectedSheet? vendor,
        IReadOnlySet<string>? existing)
    {
        if (rollup is not null && HasMapped(rollup, "Fill_Rate_Pct"))
            return Line("PerfectOrderRate", KpiCoverageStatus.ReadyFromRollup,
                $"Rollup '{rollup.SheetName}' → Fill_Rate_Pct", existing);

        if (vendor is not null && vendor.ColumnMappings.Values.Any(v =>
                v.Contains("Fill", StringComparison.OrdinalIgnoreCase) ||
                v.Contains("Perfect", StringComparison.OrdinalIgnoreCase)))
            return Line("PerfectOrderRate", KpiCoverageStatus.ReadyFromRollup,
                $"Vendor sheet '{vendor.SheetName}' (map Fill_Rate_Pct)", existing);

        return Line("PerfectOrderRate", KpiCoverageStatus.MissingExpectGray,
            "No fill rate / perfect order mapping",
            existing,
            "Map Fill_Rate on vendor performance or rollup sheet.");
    }

    private static IEnumerable<string> DiscoverWorkbookKpiCodes(IReadOnlyList<DetectedSheet> sheets)
    {
        foreach (var sheet in sheets)
        {
            foreach (var mapping in sheet.ColumnMappings.Values)
            {
                if (mapping.Contains("Margin", StringComparison.OrdinalIgnoreCase) &&
                    mapping.Contains("Percent", StringComparison.OrdinalIgnoreCase))
                    yield return "GrossMargin%";
            }

            foreach (var header in sheet.Headers)
            {
                var norm = WorkbookParseHelper.NormalizeHeader(header);
                if (norm.Contains("dposkpi", StringComparison.Ordinal) ||
                    norm.Equals("kpi", StringComparison.OrdinalIgnoreCase))
                {
                    // Values come from rows; codes discovered at import from ExpectedKpi sheet
                }
            }
        }
    }

    private static bool HasMapped(DetectedSheet sheet, string systemField) =>
        sheet.ColumnMappings.Values.Any(v =>
            string.Equals(v, systemField, StringComparison.OrdinalIgnoreCase));

    private static KpiCoverageLine Line(
        string code,
        KpiCoverageStatus status,
        string source,
        IReadOnlySet<string>? existing,
        string? fix = null) => new()
    {
        KpiCode = code,
        DisplayName = DisplayNames.TryGetValue(code, out var n) ? n : code,
        Status = existing is not null && !existing.Contains(code) && status != KpiCoverageStatus.MissingExpectGray
            ? KpiCoverageStatus.NotInSystem
            : status,
        SourceSummary = source,
        SuggestedFix = fix,
        ExistsInDatabase = existing?.Contains(code) ?? true
    };
}
