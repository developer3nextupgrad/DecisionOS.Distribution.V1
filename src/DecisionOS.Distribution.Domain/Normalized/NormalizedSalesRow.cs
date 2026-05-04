namespace DecisionOS.Distribution.Domain.Normalized;

public sealed class NormalizedSalesRow
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

    public DateOnly? TransactionDate { get; set; }
    public string? TransactionId { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? SkuId { get; set; }
    public string? ProductDescription { get; set; }
    public string? LocationId { get; set; }

    public decimal? QuantitySold { get; set; }
    public decimal? NetSales { get; set; }
    public decimal? Cogs { get; set; }
    public decimal? GrossProfit { get; set; }
}

