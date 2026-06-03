using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure.Workbooks;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Infrastructure;

public sealed class SimplifiedWorkbookReviewService : ISimplifiedWorkbookReviewService
{
    private readonly DecisionOsDbContext _db;
    private readonly ISimplifiedWorkbookImportService _import;

    public SimplifiedWorkbookReviewService(
        DecisionOsDbContext db,
        ISimplifiedWorkbookImportService import)
    {
        _db = db;
        _import = import;
    }

    public IReadOnlyList<KpiCoverageLine> BuildKpiCoveragePreview(
        WorkbookDetectionResult detection,
        IReadOnlySet<string> existingKpiCodes) =>
        KpiCoveragePreviewBuilder.Build(detection, existingKpiCodes);

    public WorkbookDetectionResult ApplyOperatorOverrides(
        WorkbookDetectionResult detection,
        WorkbookReviewInput input)
    {
        var byName = input.Sheets.ToDictionary(s => s.SheetName, StringComparer.OrdinalIgnoreCase);
        var excluded = input.ExcludedPeriodEnds?.ToHashSet() ?? [];

        var sheets = detection.Sheets.Select(s =>
        {
            if (!byName.TryGetValue(s.SheetName, out var ov))
                return s;

            var mappings = new Dictionary<string, string>(s.ColumnMappings, StringComparer.OrdinalIgnoreCase);
            foreach (var m in ov.ColumnMappings)
            {
                if (string.IsNullOrWhiteSpace(m.Key)) continue;
                if (string.IsNullOrWhiteSpace(m.Value) ||
                    string.Equals(m.Value, "Ignore", StringComparison.OrdinalIgnoreCase))
                {
                    mappings.Remove(m.Key);
                    continue;
                }
                mappings[m.Key] = m.Value;
            }

            if (mappings.Count == 0 && s.Headers.Count > 0)
            {
                foreach (var m in ColumnSynonymMatcher.InferMappings(s.Headers, ov.Kind))
                    mappings[m.Key] = m.Value;
            }
            else if (ov.Kind != s.Kind && s.Headers.Count > 0)
            {
                foreach (var m in ColumnSynonymMatcher.InferMappings(s.Headers, ov.Kind))
                {
                    if (!mappings.ContainsKey(m.Key))
                        mappings[m.Key] = m.Value;
                }
            }

            var rt = WorkbookReviewFieldCatalog.ReportTypeForKind(ov.Kind);
            return new DetectedSheet
            {
                SheetName = s.SheetName,
                SheetIndex = s.SheetIndex,
                Kind = ov.Kind,
                ReportType = rt,
                Confidence = s.Confidence,
                DataRowCount = s.DataRowCount,
                HeaderRowNumber = s.HeaderRowNumber,
                Headers = s.Headers,
                ColumnMappings = mappings
            };
        }).ToList();

        var filtered = detection.FilteredPeriodEnds
            .Where(p => !excluded.Contains(p))
            .ToList();

        return new WorkbookDetectionResult
        {
            WorkbookFingerprint = detection.WorkbookFingerprint,
            Sheets = sheets,
            RawPeriodEnds = detection.RawPeriodEnds,
            FilteredPeriodEnds = filtered,
            Warnings = detection.Warnings,
            SuggestedAnchorPeriodEnd = detection.SuggestedAnchorPeriodEnd,
            EffectiveAnchorPeriodEnd = detection.EffectiveAnchorPeriodEnd,
            AnchorAutoAdjusted = detection.AnchorAutoAdjusted,
            ExcludedPeriodEnds = excluded.ToList()
        };
    }

    public async Task SaveReviewAsync(long batchId, WorkbookReviewInput input, CancellationToken ct = default)
    {
        var batch = await _db.UploadBatches.FirstOrDefaultAsync(b => b.Id == batchId, ct)
            ?? throw new InvalidOperationException("Batch not found.");

        var detection = WorkbookAnalyzer.Deserialize(batch.DetectionSummaryJson)
            ?? throw new InvalidOperationException("Missing detection summary.");

        var updated = ApplyOperatorOverrides(detection, input);
        batch.DetectionSummaryJson = WorkbookAnalyzer.Serialize(updated);
        batch.ReadinessStatus = updated.FilteredPeriodEnds.Count > 0 ? "ReadyWithLimitations" : "NotReadyYet";
        if (updated.FilteredPeriodEnds.Count > 0)
            batch.PeriodEnd = updated.FilteredPeriodEnds.Max();

        await WorkbookKpiDefinitionEnsurer.EnsureAsync(_db, updated, ct);
        await _db.SaveChangesAsync(ct);
        await _import.ValidateSimplifiedAsync(batchId, ct);
    }

    public async Task<IReadOnlyList<KpiCoverageLine>> GetKpiCoverageForBatchAsync(
        long batchId,
        CancellationToken ct = default)
    {
        var detection = await LoadDetectionAsync(batchId, ct);
        var codes = await _db.KpiDefinitions.AsNoTracking()
            .Select(k => k.Code)
            .ToListAsync(ct);
        return BuildKpiCoveragePreview(detection, codes.ToHashSet(StringComparer.OrdinalIgnoreCase));
    }

    public Task EnsureWorkbookKpiDefinitionsAsync(
        WorkbookDetectionResult detection,
        CancellationToken ct = default) =>
        WorkbookKpiDefinitionEnsurer.EnsureAsync(_db, detection, ct);

    private async Task<WorkbookDetectionResult> LoadDetectionAsync(long batchId, CancellationToken ct)
    {
        var batch = await _db.UploadBatches.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == batchId, ct)
            ?? throw new InvalidOperationException("Batch not found.");
        return WorkbookAnalyzer.Deserialize(batch.DetectionSummaryJson)
            ?? throw new InvalidOperationException("Missing detection summary.");
    }

}
