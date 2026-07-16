namespace DecisionOS.Distribution.Domain.Uploads;

public interface IExcelMapperService
{
    Task<ExcelMapperSessionInfo> StartSessionAsync(byte[] workbookBytes, string fileName, CancellationToken ct = default);

    Task<WorkbookDetectionResult> GetDetectionAsync(Guid sessionId, CancellationToken ct = default);

    Task<ExcelMapperReviewInput> GetReviewAsync(Guid sessionId, CancellationToken ct = default);

    Task SaveReviewAsync(Guid sessionId, ExcelMapperReviewInput input, CancellationToken ct = default);

    ExcelMapperReadinessResult EvaluateReadiness(WorkbookDetectionResult detection, ExcelMapperReviewInput review);

    Task<byte[]> GenerateMappedWorkbookAsync(Guid sessionId, CancellationToken ct = default);

    string GetSuggestedDownloadName(Guid sessionId);
}
