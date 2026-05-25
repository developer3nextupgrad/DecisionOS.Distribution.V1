namespace DecisionOS.Distribution.Domain.Uploads;

public sealed class WorkbookDetectionResult
{
    public string WorkbookFingerprint { get; set; } = "";
    public IReadOnlyList<DetectedSheet> Sheets { get; set; } = Array.Empty<DetectedSheet>();
    public IReadOnlyList<DateOnly> RawPeriodEnds { get; set; } = Array.Empty<DateOnly>();
    public IReadOnlyList<DateOnly> FilteredPeriodEnds { get; set; } = Array.Empty<DateOnly>();
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
    /// <summary>Earliest period date found anywhere in the workbook.</summary>
    public DateOnly? SuggestedAnchorPeriodEnd { get; set; }
    /// <summary>Anchor used after auto-adjustment (may match batch anchor or earliest period).</summary>
    public DateOnly? EffectiveAnchorPeriodEnd { get; set; }
    public bool AnchorAutoAdjusted { get; set; }
}

public sealed class DetectedSheet
{
    public string SheetName { get; set; } = "";
    public int SheetIndex { get; set; }
    public WorkbookSheetKind Kind { get; set; }
    public ReportType? ReportType { get; set; }
    public double Confidence { get; set; }
    public int DataRowCount { get; set; }
    /// <summary>1-based header row detected in the tab.</summary>
    public int HeaderRowNumber { get; set; } = 1;
    public IReadOnlyList<string> Headers { get; set; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> ColumnMappings { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class WeeklyScoringRequest
{
    public Guid TenantId { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public long UploadBatchId { get; set; }
    public string DataConfidence { get; set; } = "High";
    /// <summary>Optional direct KPI values (e.g. from weekly rollup sheet) keyed by KpiDefinition.Code.</summary>
    public IReadOnlyDictionary<string, decimal?>? DirectKpiValues { get; set; }
}

public sealed class WeeklyScoringResult
{
    public int SnapshotsWritten { get; set; }
    public int DriverRowsProcessed { get; set; }
}
