using DecisionOS.Distribution.Domain.Uploads;

namespace DecisionOS.Distribution.Infrastructure.Workbooks;

public static class PeriodExtractor
{
    private static readonly string[] DateHeaderTokens =
    [
        "weekenddate", "week_end_date", "periodenddate", "snapshotdate",
        "transactiondate", "invoicedate", "billdate"
    ];

    public static IReadOnlyList<DateOnly> ExtractRawPeriods(ParsedWorkbook workbook)
    {
        var dates = new HashSet<DateOnly>();
        foreach (var sheet in workbook.Sheets)
        {
            if (sheet.Rows.Count == 0) continue;
            var dateCols = sheet.Headers
                .Where(h => DateHeaderTokens.Any(t =>
                    WorkbookParseHelper.NormalizeHeader(h).Contains(t, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var row in sheet.Rows)
            {
                foreach (var col in dateCols)
                {
                    if (row.TryGetValue(col, out var raw))
                    {
                        var d = WorkbookParseHelper.ParseDate(raw);
                        if (d is not null) dates.Add(d.Value);
                    }
                }
            }
        }
        return dates.OrderBy(d => d).ToList();
    }

    public static IReadOnlyList<DateOnly> ApplyCadenceAndAnchor(
        IReadOnlyList<DateOnly> raw,
        UploadCadence cadence,
        DateOnly? anchor)
    {
        if (raw.Count == 0) return raw;

        var filtered = anchor is null
            ? raw
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
}
