namespace DecisionOS.Distribution.Domain.Import;

public enum ImportValidationSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}

public sealed class ImportValidationResult
{
    private readonly List<ImportValidationIssue> _issues = new();
    public IReadOnlyList<ImportValidationIssue> Issues => _issues;
    public bool HasCritical => _issues.Any(i => i.Severity == ImportValidationSeverity.Critical);
    public bool HasWarnings => _issues.Any(i => i.Severity == ImportValidationSeverity.Warning);
    public bool HasInfo => _issues.Any(i => i.Severity == ImportValidationSeverity.Info);

    /// <summary>
    /// "Valid" here means "no critical blockers". Warnings/Info imply the import may run with limitations.
    /// </summary>
    public bool IsValid => !HasCritical;

    public void Add(
        string category,
        string message,
        int? rowNumber = null,
        string? field = null,
        ImportValidationSeverity severity = ImportValidationSeverity.Critical) =>
        _issues.Add(new ImportValidationIssue(category, message, rowNumber, field, severity));

    public string FormatMessages(int? maxLines = 200)
    {
        var lines = _issues.Select(i => i.ToString()).ToList();
        if (maxLines is null || lines.Count <= maxLines) return string.Join(Environment.NewLine, lines);
        return string.Join(Environment.NewLine, lines.Take(maxLines.Value))
               + Environment.NewLine
               + $"... ({lines.Count - maxLines.Value} more)";
    }
}

public sealed record ImportValidationIssue(
    string Category,
    string Message,
    int? RowNumber,
    string? Field,
    ImportValidationSeverity Severity)
{
    public override string ToString()
    {
        var loc = RowNumber is { } r ? $"Row {r}" : "File";
        var fld = !string.IsNullOrEmpty(Field) ? $"{Field}: " : "";
        return $"[{Severity}] [{Category}] {loc} — {fld}{Message}";
    }
}
