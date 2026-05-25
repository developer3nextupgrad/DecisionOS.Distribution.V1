using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure;
using DecisionOS.Distribution.Infrastructure.Workbooks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Operations.Uploads.Simplified;

[Authorize(Policy = "OpsPolicy")]
public class VerifyModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    private readonly ISimplifiedWorkbookImportService _simplified;
    private readonly IWebHostEnvironment _env;

    public VerifyModel(DecisionOsDbContext db, ISimplifiedWorkbookImportService simplified, IWebHostEnvironment env)
    {
        _db = db;
        _simplified = simplified;
        _env = env;
    }

    [BindProperty(SupportsGet = true)] public long Id { get; set; }
    [BindProperty] public DateOnly? AnchorPeriodEnd { get; set; }

    public UploadBatch? Batch { get; private set; }
    public WorkbookDetectionResult? Detection { get; private set; }
    public IReadOnlyList<UploadBatchIssue> Issues { get; private set; } = Array.Empty<UploadBatchIssue>();
    public string? ActionError { get; private set; }

    public string DashboardUrl =>
        Batch is null ? "/" : $"/Dashboard?ClientId={Batch.Tenant.ClientId}&PeriodEnd={Batch.PeriodEnd:yyyy-MM-dd}";

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        if (Batch is null) return NotFound();
        if (Detection is null)
            return RedirectToPage("Detect", new { id = Id });
        AnchorPeriodEnd = Batch.AnchorPeriodEnd ?? Detection.SuggestedAnchorPeriodEnd;
        return Page();
    }

    public async Task<IActionResult> OnPostReanalyzeAsync()
    {
        await LoadAsync();
        if (Batch is null) return NotFound();

        try
        {
            await _simplified.ReanalyzeStoredWorkbookAsync(Id, _env.ContentRootPath, AnchorPeriodEnd, Batch.Cadence);
            return RedirectToPage(new { id = Id });
        }
        catch (Exception ex)
        {
            ActionError = ex.Message;
            await LoadAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostValidateAsync()
    {
        await LoadAsync();
        if (Batch is null) return NotFound();
        await _simplified.ValidateSimplifiedAsync(Id);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostRunImportAsync()
    {
        await LoadAsync();
        if (Batch is null) return NotFound();

        try
        {
            await _simplified.RunSimplifiedImportAsync(Id, _env.ContentRootPath);
            return RedirectToPage("/Operations/ImportRuns/Index");
        }
        catch (Exception ex)
        {
            ActionError = ex.Message;
            await LoadAsync();
            return Page();
        }
    }

    private async Task LoadAsync()
    {
        Batch = await _db.UploadBatches.Include(b => b.Tenant).FirstOrDefaultAsync(b => b.Id == Id);
        if (Batch is null || Batch.ImportMode != UploadImportMode.Simplified) return;

        Detection = WorkbookAnalyzer.Deserialize(Batch.DetectionSummaryJson);
        Issues = await _db.UploadBatchIssues.AsNoTracking()
            .Where(i => i.UploadBatchId == Id)
            .OrderByDescending(i => i.Severity)
            .ToListAsync();
    }
}
