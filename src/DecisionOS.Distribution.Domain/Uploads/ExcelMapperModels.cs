namespace DecisionOS.Distribution.Domain.Uploads;

public sealed class ExcelMapperSessionInfo
{
    public Guid SessionId { get; set; }
    public string SourceFileName { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
}

public sealed class ExcelMapperSheetReview
{
    public string SheetName { get; set; } = "";
    public WorkbookSheetKind Kind { get; set; }
    public Dictionary<string, string> ColumnMappings { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ExcelMapperReviewInput
{
    public List<ExcelMapperSheetReview> Sheets { get; set; } = [];
}
