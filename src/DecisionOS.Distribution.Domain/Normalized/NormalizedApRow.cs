namespace DecisionOS.Distribution.Domain.Normalized;

public sealed class NormalizedApRow
{
    public long Id { get; set; }

    public Guid TenantId { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public long UploadBatchId { get; set; }
    public long UploadedFileId { get; set; }

    public int SourceRowNumber { get; set; }
    public RowStatus Status { get; set; }
    public string? IssueSummary { get; set; }
    public string RawJson { get; set; } = null!;

    public DateOnly? SnapshotDate { get; set; }
    public string? VendorId { get; set; }
    public string? VendorName { get; set; }
    public string? BillId { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? AgingBucket { get; set; }
    public int? DaysPastDue { get; set; }
    public decimal? OpenBalance { get; set; }
}

