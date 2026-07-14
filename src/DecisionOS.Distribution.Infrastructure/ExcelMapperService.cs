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
        await File.WriteAllTextAsync(
            Path.Combine(dir, "detection.json"),
            WorkbookAnalyzer.Serialize(detection),
            ct);

        var review = BuildReviewFromDetection(detection);
        await File.WriteAllTextAsync(
            Path.Combine(dir, "review.json"),
            JsonSerializer.Serialize(review, JsonOptions),
            ct);

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
            throw new InvalidOperationException("Excel Mapper session not found.");

        var json = await File.ReadAllTextAsync(path, ct);
        return WorkbookAnalyzer.Deserialize(json)
               ?? throw new InvalidOperationException("Detection data is invalid.");
    }

    public async Task SaveReviewAsync(Guid sessionId, ExcelMapperReviewInput input, CancellationToken ct = default)
    {
        EnsureSessionExists(sessionId);
        await File.WriteAllTextAsync(
            Path.Combine(SessionDir(sessionId), "review.json"),
            JsonSerializer.Serialize(input, JsonOptions),
            ct);
    }

    public async Task<byte[]> GenerateMappedWorkbookAsync(Guid sessionId, CancellationToken ct = default)
    {
        var dir = SessionDir(sessionId);
        EnsureSessionExists(sessionId);

        var sourcePath = Path.Combine(dir, "source.xlsx");
        var reviewJson = await File.ReadAllTextAsync(Path.Combine(dir, "review.json"), ct);
        var review = JsonSerializer.Deserialize<ExcelMapperReviewInput>(reviewJson, JsonOptions)
                     ?? throw new InvalidOperationException("Review data is invalid.");

        var sourceBytes = await File.ReadAllBytesAsync(sourcePath, ct);
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

    private void EnsureSessionExists(Guid sessionId)
    {
        if (!Directory.Exists(SessionDir(sessionId)))
            throw new InvalidOperationException("Excel Mapper session not found.");
    }

    private string SessionDir(Guid sessionId) => Path.Combine(_sessionRoot, sessionId.ToString("N"));
}
