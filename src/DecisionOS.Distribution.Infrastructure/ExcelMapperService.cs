using System.Text.Json;
using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure.Workbooks;

namespace DecisionOS.Distribution.Infrastructure;

public sealed class ExcelMapperService : IExcelMapperService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IWorkbookAnalyzer _analyzer;
    private readonly string _sessionRoot;

    public ExcelMapperService(IWorkbookAnalyzer analyzer)
    {
        _analyzer = analyzer;
        _sessionRoot = Path.Combine(Path.GetTempPath(), "DecisionOS", "excel-mapper");
        Directory.CreateDirectory(_sessionRoot);
    }

    public async Task<ExcelMapperSessionInfo> StartSessionAsync(
        byte[] workbookBytes,
        string fileName,
        CancellationToken ct = default)
    {
        var sessionId = Guid.NewGuid();
        var dir = SessionDir(sessionId);
        Directory.CreateDirectory(dir);

        await File.WriteAllBytesAsync(Path.Combine(dir, "source.xlsx"), workbookBytes, ct);

        var detection = _analyzer.Analyze(workbookBytes, UploadCadence.Weekly, null);
        await WriteDetectionAsync(sessionId, detection, ct);

        var review = BuildReviewFromDetection(detection);
        await WriteReviewAsync(sessionId, review, ct);

        var meta = new ExcelMapperSessionInfo
        {
            SessionId = sessionId,
            SourceFileName = fileName,
            CreatedUtc = DateTime.UtcNow
        };
        await File.WriteAllTextAsync(Path.Combine(dir, "meta.json"), JsonSerializer.Serialize(meta, JsonOptions), ct);

        return meta;
    }

    public async Task<WorkbookDetectionResult> GetDetectionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var path = Path.Combine(SessionDir(sessionId), "detection.json");
        if (!File.Exists(path))
            throw new InvalidOperationException("Excel Mapper session not found. Please upload the workbook again.");

        var json = await File.ReadAllTextAsync(path, ct);
        return WorkbookAnalyzer.Deserialize(json)
               ?? throw new InvalidOperationException("Detection data is invalid. Please upload the workbook again.");
    }

    public async Task<ExcelMapperReviewInput> GetReviewAsync(Guid sessionId, CancellationToken ct = default)
    {
        EnsureSessionExists(sessionId);
        var path = Path.Combine(SessionDir(sessionId), "review.json");
        if (!File.Exists(path))
        {
            var detection = await GetDetectionAsync(sessionId, ct);
            return BuildReviewFromDetection(detection);
        }

        var json = await File.ReadAllTextAsync(path, ct);
        return JsonSerializer.Deserialize<ExcelMapperReviewInput>(json, JsonOptions)
               ?? throw new InvalidOperationException("Review data is invalid. Please upload the workbook again.");
    }

    public async Task SaveReviewAsync(Guid sessionId, ExcelMapperReviewInput input, CancellationToken ct = default)
    {
        EnsureSessionExists(sessionId);
        var detection = await GetDetectionAsync(sessionId, ct);
        var existing = await GetReviewAsync(sessionId, ct);

        var merged = MergeReview(existing, input, detection);
        await WriteReviewAsync(sessionId, merged, ct);

        var updatedDetection = ApplyReviewToDetection(detection, merged, operatorConfirmed: true);
        await WriteDetectionAsync(sessionId, updatedDetection, ct);
    }

    public ExcelMapperReadinessResult EvaluateReadiness(
        WorkbookDetectionResult detection,
        ExcelMapperReviewInput review) =>
        ExcelMapperReadinessEvaluator.Evaluate(detection, review);

    public async Task<byte[]> GenerateMappedWorkbookAsync(Guid sessionId, CancellationToken ct = default)
    {
        var dir = SessionDir(sessionId);
        EnsureSessionExists(sessionId);

        var review = await GetReviewAsync(sessionId, ct);
        var detection = await GetDetectionAsync(sessionId, ct);
        var readiness = EvaluateReadiness(detection, review);
        if (!readiness.CanGenerate)
            throw new InvalidOperationException(string.Join(" ", readiness.BlockingIssues));

        var sourceBytes = await File.ReadAllBytesAsync(Path.Combine(dir, "source.xlsx"), ct);
        var parsed = WorkbookParseHelper.Parse(sourceBytes);
        return MappedWorkbookExporter.Export(parsed, review.Sheets);
    }

    public string GetSuggestedDownloadName(Guid sessionId)
    {
        var metaPath = Path.Combine(SessionDir(sessionId), "meta.json");
        if (!File.Exists(metaPath))
            return "DecisionOS_Mapped_Workbook.xlsx";

        var meta = JsonSerializer.Deserialize<ExcelMapperSessionInfo>(File.ReadAllText(metaPath), JsonOptions);
        if (meta is null || string.IsNullOrWhiteSpace(meta.SourceFileName))
            return "DecisionOS_Mapped_Workbook.xlsx";

        var baseName = Path.GetFileNameWithoutExtension(meta.SourceFileName);
        return $"{baseName}_DecisionOS_Mapped.xlsx";
    }

    public static ExcelMapperReviewInput MergeReview(
        ExcelMapperReviewInput existing,
        ExcelMapperReviewInput incoming,
        WorkbookDetectionResult detection)
    {
        var byName = existing.Sheets.ToDictionary(s => s.SheetName, StringComparer.OrdinalIgnoreCase);
        var detectionByName = detection.Sheets.ToDictionary(s => s.SheetName, StringComparer.OrdinalIgnoreCase);

        foreach (var sheet in incoming.Sheets)
        {
            if (string.IsNullOrWhiteSpace(sheet.SheetName)) continue;

            if (!byName.TryGetValue(sheet.SheetName, out var current))
            {
                current = new ExcelMapperSheetReview
                {
                    SheetName = sheet.SheetName,
                    Kind = sheet.Kind,
                    ColumnMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                };
                byName[sheet.SheetName] = current;
            }

            var kindChanged = current.Kind != sheet.Kind;
            current.Kind = sheet.Kind;

            if (sheet.ColumnMappingsProvided)
            {
                current.ColumnMappings = new Dictionary<string, string>(
                    sheet.ColumnMappings,
                    StringComparer.OrdinalIgnoreCase);
            }
            else if (kindChanged && detectionByName.TryGetValue(sheet.SheetName, out var detected))
            {
                if (WorkbookReviewFieldCatalog.IsReferenceOnly(sheet.Kind))
                {
                    current.ColumnMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    current.ColumnMappings = ColumnSynonymMatcher.InferMappings(detected.Headers, sheet.Kind)
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        // Preserve sheet order from detection when possible
        var ordered = new List<ExcelMapperSheetReview>();
        foreach (var ds in detection.Sheets)
        {
            if (byName.TryGetValue(ds.SheetName, out var rev))
                ordered.Add(rev);
        }

        foreach (var rev in byName.Values)
        {
            if (ordered.All(o => !string.Equals(o.SheetName, rev.SheetName, StringComparison.OrdinalIgnoreCase)))
                ordered.Add(rev);
        }

        return new ExcelMapperReviewInput { Sheets = ordered };
    }

    public static WorkbookDetectionResult ApplyReviewToDetection(
        WorkbookDetectionResult detection,
        ExcelMapperReviewInput review,
        bool operatorConfirmed)
    {
        var byName = review.Sheets.ToDictionary(s => s.SheetName, StringComparer.OrdinalIgnoreCase);

        var sheets = detection.Sheets.Select(s =>
        {
            if (!byName.TryGetValue(s.SheetName, out var ov))
                return s;

            var mappings = new Dictionary<string, string>(ov.ColumnMappings, StringComparer.OrdinalIgnoreCase);
            // Drop Ignore entries from persisted detection maps
            foreach (var key in mappings.Keys.Where(k =>
                         mappings[k].Equals("Ignore", StringComparison.OrdinalIgnoreCase)).ToList())
                mappings.Remove(key);

            return new DetectedSheet
            {
                SheetName = s.SheetName,
                SheetIndex = s.SheetIndex,
                Kind = ov.Kind,
                ReportType = WorkbookReviewFieldCatalog.ReportTypeForKind(ov.Kind),
                Confidence = operatorConfirmed ? 1.0 : s.Confidence,
                DataRowCount = s.DataRowCount,
                HeaderRowNumber = s.HeaderRowNumber,
                Headers = s.Headers,
                ColumnMappings = mappings
            };
        }).ToList();

        return new WorkbookDetectionResult
        {
            WorkbookFingerprint = detection.WorkbookFingerprint,
            Sheets = sheets,
            RawPeriodEnds = detection.RawPeriodEnds,
            FilteredPeriodEnds = detection.FilteredPeriodEnds,
            Warnings = RebuildWarnings(sheets, detection.Warnings),
            SuggestedAnchorPeriodEnd = detection.SuggestedAnchorPeriodEnd,
            EffectiveAnchorPeriodEnd = detection.EffectiveAnchorPeriodEnd,
            AnchorAutoAdjusted = detection.AnchorAutoAdjusted,
            ExcludedPeriodEnds = detection.ExcludedPeriodEnds
        };
    }

    private static IReadOnlyList<string> RebuildWarnings(
        IReadOnlyList<DetectedSheet> sheets,
        IReadOnlyList<string> prior)
    {
        var warnings = prior
            .Where(w => !w.Contains("could not be classified", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var s in sheets.Where(x => x.Kind == WorkbookSheetKind.Unknown))
            warnings.Add($"Sheet '{s.SheetName}' could not be classified confidently.");

        if (!sheets.Any(s => s.Kind == WorkbookSheetKind.WeeklyRollup) &&
            !warnings.Any(w => w.Contains("No weekly rollup", StringComparison.OrdinalIgnoreCase)))
            warnings.Add("No weekly rollup sheet detected; KPIs will be computed from transactional detail only.");

        if (!sheets.Any(s => s.Kind == WorkbookSheetKind.Sales) &&
            !warnings.Any(w => w.Contains("No sales detail", StringComparison.OrdinalIgnoreCase)))
            warnings.Add("No sales detail sheet detected.");

        // Remove stale "No weekly/sales" if now present
        if (sheets.Any(s => s.Kind == WorkbookSheetKind.WeeklyRollup))
            warnings.RemoveAll(w => w.Contains("No weekly rollup", StringComparison.OrdinalIgnoreCase));
        if (sheets.Any(s => s.Kind == WorkbookSheetKind.Sales))
            warnings.RemoveAll(w => w.Contains("No sales detail", StringComparison.OrdinalIgnoreCase));

        return warnings;
    }

    private static ExcelMapperReviewInput BuildReviewFromDetection(WorkbookDetectionResult detection)
    {
        return new ExcelMapperReviewInput
        {
            Sheets = detection.Sheets.Select(s => new ExcelMapperSheetReview
            {
                SheetName = s.SheetName,
                Kind = s.Kind,
                ColumnMappings = s.ColumnMappings.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value,
                    StringComparer.OrdinalIgnoreCase)
            }).ToList()
        };
    }

    private async Task WriteReviewAsync(Guid sessionId, ExcelMapperReviewInput review, CancellationToken ct)
    {
        // Do not persist the transient ColumnMappingsProvided flag
        var toStore = new ExcelMapperReviewInput
        {
            Sheets = review.Sheets.Select(s => new ExcelMapperSheetReview
            {
                SheetName = s.SheetName,
                Kind = s.Kind,
                ColumnMappings = s.ColumnMappings
            }).ToList()
        };
        await File.WriteAllTextAsync(
            Path.Combine(SessionDir(sessionId), "review.json"),
            JsonSerializer.Serialize(toStore, JsonOptions),
            ct);
    }

    private async Task WriteDetectionAsync(Guid sessionId, WorkbookDetectionResult detection, CancellationToken ct) =>
        await File.WriteAllTextAsync(
            Path.Combine(SessionDir(sessionId), "detection.json"),
            WorkbookAnalyzer.Serialize(detection),
            ct);

    private void EnsureSessionExists(Guid sessionId)
    {
        if (!Directory.Exists(SessionDir(sessionId)))
            throw new InvalidOperationException("Excel Mapper session not found. Please upload the workbook again.");
    }

    private string SessionDir(Guid sessionId) => Path.Combine(_sessionRoot, sessionId.ToString("N"));
}
