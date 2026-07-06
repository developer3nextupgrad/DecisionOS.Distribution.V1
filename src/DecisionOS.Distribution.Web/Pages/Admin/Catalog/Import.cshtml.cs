using DecisionOS.Distribution.Domain.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DecisionOS.Distribution.Web.Pages.Admin.Catalog;

public class ImportModel : PageModel
{
    private readonly ICatalogImportService _catalogImport;
    public ImportModel(ICatalogImportService catalogImport) => _catalogImport = catalogImport;

    [BindProperty]
    public IFormFile? Upload { get; set; }

    public CatalogImportResult? Result { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (Upload is null || Upload.Length == 0)
        {
            ErrorMessage = "Select a catalog workbook (.xlsx).";
            return Page();
        }

        await using var stream = Upload.OpenReadStream();
        Result = await _catalogImport.ImportFromWorkbookAsync(stream, ct);
        if (!Result.Success && Result.KpisImported == 0)
            ErrorMessage = string.Join("; ", Result.Errors);
        return Page();
    }
}
