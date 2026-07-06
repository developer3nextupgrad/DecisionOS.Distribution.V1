namespace DecisionOS.Distribution.Domain.Catalog;

public interface ICatalogImportService
{
    Task<CatalogImportResult> ImportFromWorkbookAsync(Stream xlsx, CancellationToken ct = default);
}
