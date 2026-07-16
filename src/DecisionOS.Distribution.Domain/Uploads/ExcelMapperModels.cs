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

    /// <summary>
    /// True when the operator posted column mapping fields for this sheet.
    /// When false, existing mappings are preserved on save (other sheets not wiped).
    /// </summary>
    public bool ColumnMappingsProvided { get; set; }
}

public sealed class ExcelMapperReviewInput
{
    public List<ExcelMapperSheetReview> Sheets { get; set; } = [];
}

public sealed class ExcelMapperReadinessResult
{
    public List<string> BlockingIssues { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public bool CanGenerate => BlockingIssues.Count == 0;
}

public sealed class ExcelMapperUserMessage
{
    public string Message { get; set; } = "";
    public string? SuggestedFix { get; set; }
}
