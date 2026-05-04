namespace DecisionOS.Distribution.Domain.Uploads;

public sealed class UploadedFileColumnMap
{
    public long Id { get; set; }

    public long UploadedFileId { get; set; }
    public UploadedFile UploadedFile { get; set; } = null!;

    public string SourceColumn { get; set; } = null!;
    public string? SystemField { get; set; }
    public bool Ignore { get; set; }
}

