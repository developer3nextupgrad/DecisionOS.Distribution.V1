namespace DecisionOS.Distribution.Domain.Uploads;

public interface IExcelMapperService
{
    Task<ExcelMapperSessionInfo> StartSessionAsync(byte[] workbookBytes, string fileName, CancellationToken ct = default);

    Task<WorkbookDetectionResult> GetDetectionAsync(Guid sessionId, CancellationToken ct = default);

    Task SaveReviewAsync(Guid sessionId, ExcelMapperReviewInput input, CancellationToken ct = default);

    Task<byte[]> GenerateMappedWorkbookAsync(Guid sessionId, CancellationToken ct = default);

    string GetSuggestedDownloadName(Guid sessionId);
}
