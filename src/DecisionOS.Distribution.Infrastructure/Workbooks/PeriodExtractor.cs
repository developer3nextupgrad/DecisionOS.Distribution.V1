using DecisionOS.Distribution.Domain.Uploads;

namespace DecisionOS.Distribution.Infrastructure.Workbooks;

public static class PeriodExtractor
{
    private static readonly string[] DateHeaderTokens =
    [
        "weekenddate", "week_end_date", "weekending", "weekendingdate", "periodend", "periodenddate",
        "period_end", "snapshotdate", "snapshot_date", "transactiondate", "transaction_date",
        "invoicedate", "invoice_date", "billdate", "bill_date", "podate", "po_date",
        "arsnapshotdate", "apsnapshotdate", "reportdate", "asofdate", "as_of_date", "fiscalweekend"
    ];

    private static readonly string[] PeriodSystemFields =
    [
        "Period_End_Date", "Transaction_Date", "AR_Snapshot_Date", "AP_Snapshot_Date", "Snapshot_Date",
        "Invoice_Date", "Bill_Date", "PO_Date"
    ];

    public static IReadOnlyList<DateOnly> ExtractRawPeriods(ParsedWorkbook workbook)
        => ExtractRawPeriods(workbook, sheets: null);

    public static IReadOnlyList<DateOnly> ExtractRawPeriods(
        ParsedWorkbook workbook,
        IReadOnlyList<DetectedSheet>? sheets)
    {
        var dates = new HashSet<DateOnly>();

        foreach (var sheet in workbook.Sheets)
        {
            CollectDatesFromDateLikeColumns(sheet, dates);
        }

        if (sheets is not null)
        {
            var byName = workbook.Sheets.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);
            foreach (var det in sheets.Where(s => s.Kind is not WorkbookSheetKind.Skip and not WorkbookSheetKind.Unknown))
            {
                if (!byName.TryGetValue(det.SheetName, out var parsed)) continue;
                CollectDatesFromMappings(parsed, det.ColumnMappings, dates);
            }
        }

        return dates.OrderBy(d => d).ToList();
    }

    public static (DateOnly? EffectiveAnchor, IReadOnlyList<DateOnly> Filtered, DateOnly? SuggestedMin, bool AutoAdjusted)
        ResolvePeriods(IReadOnlyList<DateOnly> raw, UploadCadence cadence, DateOnly? anchorPeriodEnd)
    {
        var suggested = raw.Count > 0 ? raw.Min() : (DateOnly?)null;
        var anchor = anchorPeriodEnd;
        var filtered = ApplyCadenceAndAnchor(raw, cadence, anchor);
        var autoAdjusted = false;

        if (filtered.Count == 0 && raw.Count > 0)
        {
            anchor = suggested;
            filtered = ApplyCadenceAndAnchor(raw, cadence, anchor);
            autoAdjusted = anchorPeriodEnd is null || anchor != anchorPeriodEnd;
        }

        return (anchor, filtered, suggested, autoAdjusted);
    }

    public static IReadOnlyList<DateOnly> ApplyCadenceAndAnchor(
        IReadOnlyList<DateOnly> raw,
        UploadCadence cadence,
        DateOnly? anchor)
    {
        if (raw.Count == 0) return raw;

        var filtered = anchor is null
            ? raw.ToList()
            : raw.Where(d => d >= anchor.Value).ToList();

        return cadence switch
        {
            UploadCadence.Weekly => filtered,
            UploadCadence.Monthly => filtered
                .GroupBy(d => new { d.Year, d.Month })
                .Select(g => new DateOnly(g.Key.Year, g.Key.Month, DateTime.DaysInMonth(g.Key.Year, g.Key.Month)))
                .OrderBy(d => d)
                .ToList(),
            UploadCadence.Yearly => filtered.Count == 0
                ? filtered
                : [filtered.Max()],
            _ => filtered
        };
    }

    private static void CollectDatesFromDateLikeColumns(ParsedSheet sheet, HashSet<DateOnly> dates)
    {
        if (sheet.Rows.Count == 0) return;

        foreach (var header in sheet.Headers)
        {
            var norm = WorkbookParseHelper.NormalizeHeader(header);
            if (!DateHeaderTokens.Any(t => norm.Contains(t, StringComparison.OrdinalIgnoreCase)))
                continue;

            foreach (var row in sheet.Rows)
            {
                if (row.TryGetValue(header, out var raw))
                {
                    var d = WorkbookParseHelper.ParseDate(raw);
                    if (d is not null) dates.Add(d.Value);
                }
            }
        }

        foreach (var header in sheet.Headers)
        {
            var norm = WorkbookParseHelper.NormalizeHeader(header);
            if (DateHeaderTokens.Any(t => norm.Contains(t, StringComparison.OrdinalIgnoreCase)))
                continue;

            var parsed = 0;
            var total = 0;
            foreach (var row in sheet.Rows)
            {
                if (!row.TryGetValue(header, out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                total++;
                if (WorkbookParseHelper.ParseDate(raw) is not null) parsed++;
            }

            if (total >= 5 && parsed >= Math.Max(3, total / 5))
            {
                foreach (var row in sheet.Rows)
                {
                    if (row.TryGetValue(header, out var raw))
                    {
                        var d = WorkbookParseHelper.ParseDate(raw);
                        if (d is not null) dates.Add(d.Value);
                    }
                }
            }
        }
    }

    private static void CollectDatesFromMappings(
        ParsedSheet sheet,
        IReadOnlyDictionary<string, string> colMap,
        HashSet<DateOnly> dates)
    {
        foreach (var field in PeriodSystemFields)
        {
            var source = colMap.FirstOrDefault(kvp =>
                string.Equals(kvp.Value, field, StringComparison.OrdinalIgnoreCase)).Key;
            if (source is null) continue;

            foreach (var row in sheet.Rows)
            {
                if (row.TryGetValue(source, out var raw))
                {
                    var d = WorkbookParseHelper.ParseDate(raw);
                    if (d is not null) dates.Add(d.Value);
                }
            }
        }
    }
}
