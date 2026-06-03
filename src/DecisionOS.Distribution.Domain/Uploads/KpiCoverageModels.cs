namespace DecisionOS.Distribution.Domain.Uploads;

public enum KpiCoverageStatus
{
    /// <summary>Direct value from weekly rollup (or aging rollup) mapping.</summary>
    ReadyFromRollup = 0,
    /// <summary>Computed from normalized detail sheets (sales, AR, AP, inventory).</summary>
    ReadyFromDetail = 1,
    /// <summary>Composite KPI; depends on other pillars having data.</summary>
    DependsOnOther = 2,
    /// <summary>No data path; dashboard tile will be GRAY.</summary>
    MissingExpectGray = 3,
    /// <summary>KPI code present in workbook but not seeded in KpiDefinitions.</summary>
    NotInSystem = 4
}

public sealed class KpiCoverageLine
{
    public string KpiCode { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public KpiCoverageStatus Status { get; set; }
    public string SourceSummary { get; set; } = "";
    public string? SuggestedFix { get; set; }
    public bool ExistsInDatabase { get; set; } = true;
}

public sealed class WorkbookReviewInput
{
    public List<SheetReviewInput> Sheets { get; set; } = [];
    public List<DateOnly>? ExcludedPeriodEnds { get; set; }
    public bool AcknowledgeGrayKpis { get; set; }
}

public sealed class SheetReviewInput
{
    public string SheetName { get; set; } = "";
    public WorkbookSheetKind Kind { get; set; }
    public Dictionary<string, string> ColumnMappings { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}
