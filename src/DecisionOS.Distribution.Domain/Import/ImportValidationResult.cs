namespace DecisionOS.Distribution.Domain.Import;

public sealed class ImportValidationResult
{
    private readonly List<ImportValidationIssue> _issues = new();
    public IReadOnlyList<ImportValidationIssue> Issues => _issues;
    public bool IsValid => _issues.Count == 0;
    public void Add(string category, string message, int? rowNumber = null, string? field = null) =>
        _issues.Add(new ImportValidationIssue(category, message, rowNumber, field));

    public string FormatMessages() => string.Join(Environment.NewLine, _issues.Select(i => i.ToString()));
}

public sealed record ImportValidationIssue(string Category, string Message, int? RowNumber, string? Field)
{
    public override string ToString()
    {
        var loc = RowNumber is { } r ? $"Row {r}" : "File";
        var fld = !string.IsNullOrEmpty(Field) ? $"{Field}: " : "";
        return $"[{Category}] {loc} — {fld}{Message}";
    }
}
