namespace DecisionOS.Distribution.Domain.Uploads;

public interface IWorkbookAnalyzer
{
    WorkbookDetectionResult Analyze(
        byte[] workbookBytes,
        UploadCadence cadence,
        DateOnly? anchorPeriodEnd);

    WorkbookDetectionResult AnalyzeFile(
        string filePath,
        UploadCadence cadence,
        DateOnly? anchorPeriodEnd);
}
