using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure;
using DecisionOS.Distribution.Infrastructure.Workbooks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Operations.Uploads.Simplified;

[Authorize(Policy = "OpsPolicy")]
public class DetectModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    private readonly ISimplifiedWorkbookImportService _simplified;
    private readonly IWebHostEnvironment _env;

    public DetectModel(DecisionOsDbContext db, ISimplifiedWorkbookImportService simplified, IWebHostEnvironment env)
    {
        _db = db;
        _simplified = simplified;
        _env = env;
    }

    [BindProperty(SupportsGet = true)] public long Id { get; set; }

    public UploadBatch? Batch { get; private set; }
    public WorkbookDetectionResult? Detection { get; private set; }
    public string? Error { get; private set; }

    [BindProperty] public IFormFile? WorkbookFile { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Batch = await LoadBatchAsync();
        if (Batch is null) return NotFound();
        Detection = WorkbookAnalyzer.Deserialize(Batch.DetectionSummaryJson);
        return Page();
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        Batch = await LoadBatchAsync();
        if (Batch is null) return NotFound();

        if (WorkbookFile is null || WorkbookFile.Length == 0)
        {
            Error = "Please select an Excel workbook (.xlsx).";
            return Page();
        }

        var ext = Path.GetExtension(WorkbookFile.FileName);
        if (!string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(ext, ".xls", StringComparison.OrdinalIgnoreCase))
        {
            Error = "Only Excel (.xlsx/.xls) workbooks are supported for simplified import.";
            return Page();
        }

        await using var ms = new MemoryStream();
        await WorkbookFile.CopyToAsync(ms);
        var bytes = ms.ToArray();

        try
        {
            Detection = await _simplified.DetectAndPersistAsync(
                Batch.Id, bytes, WorkbookFile.FileName, _env.ContentRootPath);
            return RedirectToPage("Verify", new { id = Batch.Id });
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            return Page();
        }
    }

    private async Task<UploadBatch?> LoadBatchAsync()
    {
        var batch = await _db.UploadBatches.Include(b => b.Tenant).FirstOrDefaultAsync(b => b.Id == Id);
        if (batch is null || batch.ImportMode != UploadImportMode.Simplified)
            return null;
        return batch;
    }
}
