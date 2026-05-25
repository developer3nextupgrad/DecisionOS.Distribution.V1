namespace DecisionOS.Distribution.Domain.Uploads;

public interface ISimplifiedWorkbookImportService
{
    Task<WorkbookDetectionResult> DetectAndPersistAsync(
        long batchId,
        byte[] workbookBytes,
        string originalFileName,
        string contentRootPath,
        CancellationToken ct = default);

    Task ValidateSimplifiedAsync(long batchId, CancellationToken ct = default);

    Task RunSimplifiedImportAsync(long batchId, string contentRootPath, CancellationToken ct = default);
}
