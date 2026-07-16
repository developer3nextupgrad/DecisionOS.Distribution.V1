using DecisionOS.Distribution.Domain.Uploads;

namespace DecisionOS.Distribution.Infrastructure.Workbooks;

public static class ExcelMapperReadinessEvaluator
{
    private static readonly WorkbookSheetKind[] PreferredKinds =
    [
        WorkbookSheetKind.WeeklyRollup,
        WorkbookSheetKind.Sales
    ];

    private static readonly Dictionary<WorkbookSheetKind, string[]> RequiredFields =
        new()
        {
            [WorkbookSheetKind.WeeklyRollup] = ["Period_End_Date", "Net_Sales"],
            [WorkbookSheetKind.Sales] = ["Transaction_Date", "Net_Sales"],
            [WorkbookSheetKind.AccountsReceivable] = ["Customer_ID", "Open_Balance"],
            [WorkbookSheetKind.AccountsPayable] = ["Vendor_ID", "Open_Balance"],
            [WorkbookSheetKind.Inventory] = ["SKU_ID", "Quantity_On_Hand"]
        };

    public static ExcelMapperReadinessResult Evaluate(
        WorkbookDetectionResult detection,
        ExcelMapperReviewInput review)
    {
        var result = new ExcelMapperReadinessResult();
        var reviewByName = review.Sheets.ToDictionary(s => s.SheetName, StringComparer.OrdinalIgnoreCase);
        var exportable = review.Sheets.Where(s => ExcelMapperOutputCatalog.IsExportable(s.Kind)).ToList();

        if (exportable.Count == 0)
        {
            result.BlockingIssues.Add(
                "No tabs are set to export. Set at least one tab to Weekly financials or Sales, then Save.");
            return result;
        }

        var hasPreferred = exportable.Any(s => PreferredKinds.Contains(s.Kind));
        if (!hasPreferred)
        {
            result.BlockingIssues.Add(
                "Add a Weekly financials or Sales tab role before download. Other tabs alone are not enough for Simplified upload.");
        }

        var unknown = review.Sheets.Where(s => s.Kind == WorkbookSheetKind.Unknown).ToList();
        foreach (var u in unknown)
        {
            result.Warnings.Add(
                $"Tab “{u.SheetName}” is still Unknown — choose a Role or set Skip, then Save. " +
                ExcelMapperWarningGuide.LowConfidenceFix());
        }

        foreach (var sheet in exportable)
        {
            if (!RequiredFields.TryGetValue(sheet.Kind, out var required))
                continue;

            var detectionSheet = detection.Sheets.FirstOrDefault(s =>
                string.Equals(s.SheetName, sheet.SheetName, StringComparison.OrdinalIgnoreCase));
            var headers = detectionSheet?.Headers ?? Array.Empty<string>();
            var mapped = sheet.ColumnMappings.Values
                .Where(v => !string.IsNullOrWhiteSpace(v) &&
                            !v.Equals("Ignore", StringComparison.OrdinalIgnoreCase))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Direct header names that already match template / system fields also count.
            foreach (var h in headers)
            {
                var sys = ExcelMapperOutputCatalog.SystemFieldForOutputHeader(sheet.Kind, h);
                if (sys is not null)
                    mapped.Add(sys);
                mapped.Add(h);
            }

            var missing = required.Where(r => !mapped.Contains(r)).ToList();
            if (missing.Count == 0)
                continue;

            var labels = string.Join(", ", missing.Select(SystemFieldDisplayCatalog.GetLabel));
            var msg =
                $"Tab “{sheet.SheetName}” ({WorkbookSheetKindDisplay.ShortLabel(sheet.Kind)}) is missing: {labels}.";
            var fix =
                $"Click Edit mappings for “{sheet.SheetName}”, map those columns, then Save.";

            if (PreferredKinds.Contains(sheet.Kind))
                result.BlockingIssues.Add($"{msg} {fix}");
            else
                result.Warnings.Add($"{msg} {fix}");
        }

        if (!detection.FilteredPeriodEnds.Any() && !detection.RawPeriodEnds.Any())
        {
            result.Warnings.Add(
                "No week-ending dates detected yet. After mapping date columns, Save — or confirm dates when you upload in Simplified.");
        }

        return result;
    }
}
