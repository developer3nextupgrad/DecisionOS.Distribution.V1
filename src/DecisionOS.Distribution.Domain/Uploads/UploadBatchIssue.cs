namespace DecisionOS.Distribution.Domain.Uploads;

public enum UploadIssueSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}

public sealed class UploadBatchIssue
{
    public long Id { get; set; }

    public long UploadBatchId { get; set; }
    public UploadBatch UploadBatch { get; set; } = null!;

    public long? UploadedFileId { get; set; }
    public UploadedFile? UploadedFile { get; set; }

    public UploadIssueSeverity Severity { get; set; }
    public string Category { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? Field { get; set; }
}

