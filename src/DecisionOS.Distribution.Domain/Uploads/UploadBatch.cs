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

    public UploadImportMode ImportMode { get; set; } = UploadImportMode.Classic;
    public UploadCadence? Cadence { get; set; }
    /// <summary>First period to include when importing multi-period simplified workbooks.</summary>
    public DateOnly? AnchorPeriodEnd { get; set; }
    public string? WorkbookFingerprint { get; set; }
    public string? DetectionSummaryJson { get; set; }
    /// <summary>Relative path to stored .xlsx for simplified imports.</summary>
    public string? WorkbookStoredRelativePath { get; set; }

    /// <summary>ReadyToRun / ReadyWithLimitations / NotReadyYet.</summary>
    public string? ReadinessStatus { get; set; }
    public string? ValidationSummary { get; set; }

    public int? ImportRunId { get; set; }
    public ImportRun? ImportRun { get; set; }

    public List<UploadedFile> Files { get; set; } = new();
}

