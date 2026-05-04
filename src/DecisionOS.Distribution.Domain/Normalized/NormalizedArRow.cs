namespace DecisionOS.Distribution.Domain.Normalized;

public sealed class NormalizedArRow
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
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? InvoiceId { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? AgingBucket { get; set; }
    public int? DaysPastDue { get; set; }
    public decimal? OpenBalance { get; set; }
}

