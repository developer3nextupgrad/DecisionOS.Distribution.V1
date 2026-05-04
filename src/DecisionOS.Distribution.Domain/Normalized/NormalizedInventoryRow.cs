namespace DecisionOS.Distribution.Domain.Normalized;

public sealed class NormalizedInventoryRow
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
    public string? SkuId { get; set; }
    public string? LocationId { get; set; }
    public decimal? QuantityOnHand { get; set; }
    public decimal? InventoryValue { get; set; }
    public decimal? AverageCost { get; set; }
    public DateOnly? LastSaleDate { get; set; }
}

