namespace DecisionOS.Distribution.Domain.Uploads;

public static class UploadBatchStatuses
{
    public const string Draft = "Draft";
    public const string MappingInProgress = "MappingInProgress";
    public const string Validated = "Validated";
    public const string Imported = "Imported";
}

public sealed class UploadBatch
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public DateOnly PeriodEnd { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Status { get; set; } = UploadBatchStatuses.Draft;

    /// <summary>ReadyToRun / ReadyWithLimitations / NotReadyYet.</summary>
    public string? ReadinessStatus { get; set; }
    public string? ValidationSummary { get; set; }

    public int? ImportRunId { get; set; }
    public ImportRun? ImportRun { get; set; }

    public List<UploadedFile> Files { get; set; } = new();
}

