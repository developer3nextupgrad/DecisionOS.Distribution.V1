using System.Globalization;
using DecisionOS.Distribution.Domain.Uploads;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace DecisionOS.Distribution.Infrastructure.Workbooks;

public static class MappedWorkbookExporter
{
    private static readonly (WorkbookSheetKind Kind, int Order)[] ExportOrder =
    [
        (WorkbookSheetKind.WeeklyRollup, 1),
        (WorkbookSheetKind.Sales, 2),
        (WorkbookSheetKind.AccountsReceivable, 3),
        (WorkbookSheetKind.AccountsPayable, 4),
        (WorkbookSheetKind.Inventory, 5),
        (WorkbookSheetKind.Customer, 6),
        (WorkbookSheetKind.Vendor, 7),
        (WorkbookSheetKind.Product, 8),
        (WorkbookSheetKind.Purchasing, 9),
        (WorkbookSheetKind.Holdover, 10),
    ];

    public static byte[] Export(
        ParsedWorkbook source,
        IReadOnlyList<ExcelMapperSheetReview> reviewSheets)
    {
        using var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = doc.AddWorkbookPart();
            workbookPart.Workbook = new Workbook(new Sheets());

            AddReadmeSheet(workbookPart, reviewSheets);

            var reviewByName = reviewSheets.ToDictionary(s => s.SheetName, StringComparer.OrdinalIgnoreCase);
            var rowsByOutputSheet = new Dictionary<string, List<string[]>>(StringComparer.OrdinalIgnoreCase);
            var headersByOutputSheet = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
            var kindByOutputSheet = new Dictionary<string, WorkbookSheetKind>(StringComparer.OrdinalIgnoreCase);

            foreach (var ps in source.Sheets)
            {
                if (!reviewByName.TryGetValue(ps.Name, out var review)) continue;
                if (!ExcelMapperOutputCatalog.IsExportable(review.Kind)) continue;

                var outputName = ExcelMapperOutputCatalog.SheetNameForKind(review.Kind)!;
                var headers = ExcelMapperOutputCatalog.OutputHeadersForKind(review.Kind);
                headersByOutputSheet[outputName] = headers;
                kindByOutputSheet[outputName] = review.Kind;

                if (!rowsByOutputSheet.TryGetValue(outputName, out var bucket))
                {
                    bucket = [];
                    rowsByOutputSheet[outputName] = bucket;
                }

                var rowIndex = 0;
                foreach (var row in ps.Rows)
                {
                    rowIndex++;
                    var values = BuildOutputRow(review.Kind, headers, row, review.ColumnMappings, rowIndex);
                    if (values.Any(v => !string.IsNullOrWhiteSpace(v)))
                        bucket.Add(values);
                }
            }

            uint sheetId = 2;
            foreach (var (kind, _) in ExportOrder)
            {
                var outputName = ExcelMapperOutputCatalog.SheetNameForKind(kind);
                if (outputName is null || !headersByOutputSheet.ContainsKey(outputName)) continue;

                var headers = headersByOutputSheet[outputName];
                var rows = rowsByOutputSheet.GetValueOrDefault(outputName) ?? [];
                AddDataSheet(workbookPart, outputName, sheetId++, headers, rows);
            }

            workbookPart.Workbook.Save();
        }

        return ms.ToArray();
    }

    private static string[] BuildOutputRow(
        WorkbookSheetKind kind,
        IReadOnlyList<string> headers,
        IReadOnlyDictionary<string, string?> sourceRow,
        IReadOnlyDictionary<string, string> columnMappings,
        int rowIndex)
    {
        var values = new string[headers.Count];
        for (var i = 0; i < headers.Count; i++)
        {
            var header = headers[i];
            if (header.Equals("Week_Number", StringComparison.OrdinalIgnoreCase))
            {
                values[i] = rowIndex.ToString(CultureInfo.InvariantCulture);
                continue;
            }

            var systemField = ExcelMapperOutputCatalog.SystemFieldForOutputHeader(kind, header);
            if (systemField is not null)
            {
                values[i] = ColumnSynonymMatcher.GetMapped(sourceRow, columnMappings, systemField) ?? "";
                if (!string.IsNullOrWhiteSpace(values[i]))
                    continue;
            }

            values[i] = TryDirectHeaderMatch(sourceRow, header) ?? "";
        }

        return values;
    }

    private static string? TryDirectHeaderMatch(IReadOnlyDictionary<string, string?> sourceRow, string outputHeader)
    {
        if (sourceRow.TryGetValue(outputHeader, out var exact) && !string.IsNullOrWhiteSpace(exact))
            return exact;

        var norm = WorkbookParseHelper.NormalizeHeader(outputHeader);
        foreach (var kvp in sourceRow)
        {
            if (WorkbookParseHelper.NormalizeHeader(kvp.Key).Equals(norm, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        return null;
    }

    private static void AddReadmeSheet(WorkbookPart workbookPart, IReadOnlyList<ExcelMapperSheetReview> reviewSheets)
    {
        var headers = new[] { "Topic", "Guidance" };
        var rows = new List<string[]>
        {
            new[] { "Purpose", "Generated by Excel Mapper — upload via Operations → Uploads → Simplified." },
            new[] { "Sheet roles", string.Join(", ", reviewSheets.Where(s => ExcelMapperOutputCatalog.IsExportable(s.Kind)).Select(s => $"{s.SheetName}→{ExcelMapperOutputCatalog.SheetNameForKind(s.Kind)}")) },
            new[] { "Skipped tabs", string.Join(", ", reviewSheets.Where(s => !ExcelMapperOutputCatalog.IsExportable(s.Kind)).Select(s => s.SheetName)) },
            new[] { "Next step", "Operations → Uploads → Create batch (Simplified) → upload this file." },
        };
        AddDataSheet(workbookPart, "README_Import_Map", 1, headers, rows);
    }

    private static void AddDataSheet(
        WorkbookPart workbookPart,
        string sheetName,
        uint sheetId,
        IReadOnlyList<string> headers,
        IReadOnlyList<string[]> rows)
    {
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        var sheetData = new SheetData();
        var worksheet = new Worksheet(sheetData);

        var headerRow = new Row { RowIndex = 1 };
        for (var c = 0; c < headers.Count; c++)
            headerRow.Append(CreateCell(headers[c], c + 1));
        sheetData.Append(headerRow);

        for (var r = 0; r < rows.Count; r++)
        {
            var dataRow = new Row { RowIndex = (uint)(r + 2) };
            var rowValues = rows[r];
            for (var c = 0; c < headers.Count; c++)
            {
                var val = c < rowValues.Length ? rowValues[c] : "";
                dataRow.Append(CreateCell(val, c + 1));
            }
            sheetData.Append(dataRow);
        }

        worksheetPart.Worksheet = worksheet;
        worksheetPart.Worksheet.Save();

        var sheets = workbookPart.Workbook.GetFirstChild<Sheets>()!;
        sheets.Append(new Sheet
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = sheetId,
            Name = SanitizeSheetName(sheetName)
        });
    }

    private static Cell CreateCell(string value, int columnIndex)
    {
        var colRef = GetColumnName(columnIndex);
        return new Cell
        {
            CellReference = colRef,
            DataType = CellValues.String,
            CellValue = new CellValue(value ?? "")
        };
    }

    private static string GetColumnName(int columnIndex)
    {
        var dividend = columnIndex;
        var columnName = "";
        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar('A' + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }
        return columnName;
    }

    private static string SanitizeSheetName(string name)
    {
        var invalid = new[] { '\\', '/', '*', '?', ':', '[', ']' };
        var cleaned = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }
}
