using System.Text.Json;
using DecisionOS.Distribution.Domain.Uploads;

namespace DecisionOS.Distribution.Infrastructure.Workbooks;

public sealed class WorkbookAnalyzer : IWorkbookAnalyzer
{
    public WorkbookDetectionResult Analyze(byte[] workbookBytes, UploadCadence cadence, DateOnly? anchorPeriodEnd)
    {
        var wb = WorkbookParseHelper.Parse(workbookBytes);
        return BuildResult(wb, cadence, anchorPeriodEnd, UploadedFile.ComputeSha256Hex(workbookBytes));
    }

    public WorkbookDetectionResult AnalyzeFile(string filePath, UploadCadence cadence, DateOnly? anchorPeriodEnd)
    {
        var bytes = File.ReadAllBytes(filePath);
        return Analyze(bytes, cadence, anchorPeriodEnd);
    }

    internal static WorkbookDetectionResult BuildResult(
        ParsedWorkbook wb,
        UploadCadence cadence,
        DateOnly? anchorPeriodEnd,
        string fingerprint)
    {
        var warnings = new List<string>();
        var sheets = new List<DetectedSheet>();

        foreach (var ps in wb.Sheets)
        {
            var (kind, rt, conf) = SheetClassifier.Classify(ps.Name, ps.Headers);
            if (kind == WorkbookSheetKind.Unknown)
                warnings.Add($"Sheet '{ps.Name}' could not be classified confidently.");

            var mappings = kind is WorkbookSheetKind.Skip or WorkbookSheetKind.Unknown
                ? new Dictionary<string, string>()
                : ColumnSynonymMatcher.InferMappings(ps.Headers, kind);

            sheets.Add(new DetectedSheet
            {
                SheetName = ps.Name,
                SheetIndex = ps.Index,
                Kind = kind,
                ReportType = rt,
                Confidence = conf,
                DataRowCount = ps.Rows.Count,
                Headers = ps.Headers,
                ColumnMappings = mappings,
                HeaderRowNumber = ps.HeaderRowNumber
            });
        }

        if (!sheets.Any(s => s.Kind == WorkbookSheetKind.WeeklyRollup))
            warnings.Add("No weekly rollup sheet detected; KPIs will be computed from transactional detail only.");

        if (!sheets.Any(s => s.Kind == WorkbookSheetKind.Sales))
            warnings.Add("No sales detail sheet detected.");

        var rawPeriods = PeriodExtractor.ExtractRawPeriods(wb, sheets);
        var (effectiveAnchor, filtered, suggested, autoAdjusted) =
            PeriodExtractor.ResolvePeriods(rawPeriods, cadence, anchorPeriodEnd);

        if (autoAdjusted && effectiveAnchor is not null)
            warnings.Add($"Anchor auto-adjusted to earliest detected period {effectiveAnchor:yyyy-MM-dd} (configured anchor excluded all periods).");

        if (filtered.Count == 0 && rawPeriods.Count > 0 && !autoAdjusted)
            warnings.Add("Anchor date excluded all periods; set anchor to earliest week in workbook or leave blank on create.");

        if (filtered.Count == 0 && rawPeriods.Count == 0)
            warnings.Add("No week-ending dates detected; check date column headers (e.g. Week_End_Date, Period_End).");

        if (sheets.Any(s => s.Kind == WorkbookSheetKind.AccountsReceivable))
            warnings.Add("AR_Over_60_% in rollup maps to AR_PastDue31p% KPI (approximation).");

        return new WorkbookDetectionResult
        {
            WorkbookFingerprint = fingerprint,
            Sheets = sheets,
            RawPeriodEnds = rawPeriods,
            FilteredPeriodEnds = filtered,
            SuggestedAnchorPeriodEnd = suggested,
            EffectiveAnchorPeriodEnd = effectiveAnchor,
            AnchorAutoAdjusted = autoAdjusted,
            Warnings = warnings
        };
    }

    public static string Serialize(WorkbookDetectionResult result)
        => JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });

    public static WorkbookDetectionResult? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        return JsonSerializer.Deserialize<WorkbookDetectionResult>(json);
    }
}
