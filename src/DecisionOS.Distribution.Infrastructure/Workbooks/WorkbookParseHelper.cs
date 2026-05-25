using System.Data;
using System.Globalization;
using System.Text;
using ExcelDataReader;

namespace DecisionOS.Distribution.Infrastructure.Workbooks;

public sealed class ParsedWorkbook
{
    public IReadOnlyList<ParsedSheet> Sheets { get; init; } = Array.Empty<ParsedSheet>();
}

public sealed class ParsedSheet
{
    public string Name { get; init; } = "";
    public int Index { get; init; }
    /// <summary>1-based Excel row number of the detected header row.</summary>
    public int HeaderRowNumber { get; init; } = 1;
    public IReadOnlyList<string> Headers { get; init; } = Array.Empty<string>();
    public IReadOnlyList<IReadOnlyDictionary<string, string?>> Rows { get; init; } =
        Array.Empty<IReadOnlyDictionary<string, string?>>();
}

public static class WorkbookParseHelper
{
    static WorkbookParseHelper()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static ParsedWorkbook Parse(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
        });

        var sheets = new List<ParsedSheet>();
        for (var i = 0; i < dataSet.Tables.Count; i++)
        {
            var table = dataSet.Tables[i];
            if (table.Rows.Count == 0)
            {
                sheets.Add(new ParsedSheet { Name = table.TableName, Index = i });
                continue;
            }

            var headerRowIndex = WorkbookHeaderDetector.DetectHeaderRowIndex(table);
            var headerRow = table.Rows[headerRowIndex];
            var headers = new List<string>();
            for (var c = 0; c < table.Columns.Count; c++)
            {
                var h = FormatCell(headerRow[c]);
                headers.Add(string.IsNullOrWhiteSpace(h) ? $"Column{c + 1}" : h.Trim());
            }

            var rows = new List<IReadOnlyDictionary<string, string?>>();
            for (var r = headerRowIndex + 1; r < table.Rows.Count; r++)
            {
                var dr = table.Rows[r];
                if (IsEmptyRow(dr)) continue;

                var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                for (var c = 0; c < headers.Count; c++)
                    dict[headers[c]] = FormatCell(dr[c]);
                rows.Add(dict);
            }

            sheets.Add(new ParsedSheet
            {
                Name = table.TableName,
                Index = i,
                Headers = headers,
                Rows = rows,
                HeaderRowNumber = headerRowIndex + 1
            });
        }

        return new ParsedWorkbook { Sheets = sheets };
    }

    public static ParsedWorkbook ParseFile(string path)
        => Parse(File.ReadAllBytes(path));

    private static bool IsEmptyRow(DataRow row)
    {
        foreach (var item in row.ItemArray)
        {
            if (!string.IsNullOrWhiteSpace(FormatCell(item)))
                return false;
        }
        return true;
    }

    public static string FormatCell(object? cell)
    {
        if (cell is null || cell == DBNull.Value) return "";
        if (cell is DateTime dt) return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (cell is double d && d > 20000 && d < 60000)
        {
            try
            {
                return DateTime.FromOADate(d).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            catch
            {
                // fall through
            }
        }
        return cell.ToString()?.Trim() ?? "";
    }

    public static DateOnly? ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (DateOnly.TryParse(raw.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        if (double.TryParse(raw.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var oa) &&
            oa > 20000 && oa < 60000)
        {
            try { return DateOnly.FromDateTime(DateTime.FromOADate(oa)); }
            catch { return null; }
        }
        return null;
    }

    public static decimal? ParseDecimal(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim().Replace("%", "");
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    public static int? ParseInt(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return int.TryParse(raw.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    public static string NormalizeHeader(string header)
    {
        var sb = new StringBuilder();
        foreach (var ch in header.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
        }
        return sb.ToString();
    }
}
