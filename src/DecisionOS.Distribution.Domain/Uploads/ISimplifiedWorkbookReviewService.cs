namespace DecisionOS.Distribution.Domain.Uploads;

public interface ISimplifiedWorkbookReviewService
{
    IReadOnlyList<KpiCoverageLine> BuildKpiCoveragePreview(
        WorkbookDetectionResult detection,
        IReadOnlySet<string> existingKpiCodes);

    WorkbookDetectionResult ApplyOperatorOverrides(
        WorkbookDetectionResult detection,
        WorkbookReviewInput input);

    Task SaveReviewAsync(long batchId, WorkbookReviewInput input, CancellationToken ct = default);

    Task<IReadOnlyList<KpiCoverageLine>> GetKpiCoverageForBatchAsync(long batchId, CancellationToken ct = default);

    /// <summary>Creates global KPI definitions for codes found in workbook mappings but not yet in the database.</summary>
    Task EnsureWorkbookKpiDefinitionsAsync(WorkbookDetectionResult detection, CancellationToken ct = default);
}
