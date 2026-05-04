using DecisionOS.Distribution.Domain.Import;

namespace DecisionOS.Distribution.Domain;

/// <summary>
/// Row-level or file-level exception log for an import run.
/// </summary>
public sealed class ImportRunIssue
{
    public long Id { get; set; }

    public int ImportRunId { get; set; }
    public ImportRun ImportRun { get; set; } = null!;

    public string Category { get; set; } = null!;
    public ImportValidationSeverity Severity { get; set; }
    public string Message { get; set; } = null!;

    public int? RowNumber { get; set; }
    public string? Field { get; set; }
}

