using System.Data;

namespace DecisionOS.Distribution.Infrastructure.Workbooks;

/// <summary>Finds the header row when Excel tabs have title rows above column names.</summary>
internal static class WorkbookHeaderDetector
{
    private static HashSet<string>? _knownNorms;

    public static int DetectHeaderRowIndex(DataTable table, int maxScanRows = 30)
    {
        if (table.Rows.Count == 0) return 0;

        _knownNorms ??= ColumnSynonymMatcher.BuildKnownHeaderNormSet();
        var limit = Math.Min(maxScanRows, table.Rows.Count);
        var bestRow = 0;
        var bestScore = 0;

        for (var r = 0; r < limit; r++)
        {
            var row = table.Rows[r];
            var nonEmpty = 0;
            var score = 0;
            for (var c = 0; c < table.Columns.Count; c++)
            {
                var cell = WorkbookParseHelper.FormatCell(row[c]);
                if (!string.IsNullOrWhiteSpace(cell)) nonEmpty++;
                var norm = WorkbookParseHelper.NormalizeHeader(cell);
                if (norm.Length < 2) continue;
                if (_knownNorms.Contains(norm) || _knownNorms.Any(k => norm.Contains(k, StringComparison.Ordinal) || k.Contains(norm, StringComparison.Ordinal)))
                    score++;
            }

            if (nonEmpty < 2) continue;

            var adjusted = score + (r == 0 ? 1 : 0);
            if (adjusted > bestScore || (adjusted == bestScore && r < bestRow))
            {
                bestScore = adjusted;
                bestRow = r;
            }
        }

        return bestScore >= 3 ? bestRow : 0;
    }
}
