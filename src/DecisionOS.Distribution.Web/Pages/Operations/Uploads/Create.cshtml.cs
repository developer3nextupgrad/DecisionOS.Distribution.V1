using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Operations.Uploads;

[Authorize(Policy = "OpsPolicy")]
public class CreateModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public CreateModel(DecisionOsDbContext db) => _db = db;

    [BindProperty] public string ClientId { get; set; } = "";
    [BindProperty] public DateOnly PeriodEnd { get; set; }
    [BindProperty] public UploadImportMode ImportMode { get; set; } = UploadImportMode.Classic;
    [BindProperty] public DateOnly AnchorPeriodEnd { get; set; }
    [BindProperty] public UploadCadence Cadence { get; set; } = UploadCadence.Weekly;

    public IReadOnlyList<(string ClientId, string Name)> Tenants { get; private set; } = Array.Empty<(string, string)>();

    public async Task OnGetAsync()
    {
        PeriodEnd = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        AnchorPeriodEnd = PeriodEnd;
        Tenants = await _db.Tenants.AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new ValueTuple<string, string>(t.ClientId, t.Name))
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
            ModelState.AddModelError(nameof(ClientId), "ClientId is required.");

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.ClientId == ClientId);
        if (tenant is null)
            ModelState.AddModelError(nameof(ClientId), "Unknown tenant.");

        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        if (ImportMode == UploadImportMode.Simplified)
        {
            var batch = new UploadBatch
            {
                TenantId = tenant!.Id,
                PeriodEnd = AnchorPeriodEnd,
                AnchorPeriodEnd = AnchorPeriodEnd,
                Cadence = Cadence,
                ImportMode = UploadImportMode.Simplified,
                CreatedAt = DateTimeOffset.UtcNow,
                Status = UploadBatchStatuses.Draft
            };
            _db.UploadBatches.Add(batch);
            await _db.SaveChangesAsync();
            return RedirectToPage("/Operations/Uploads/Simplified/Detect", new { id = batch.Id });
        }

        var classicBatch = new UploadBatch
        {
            TenantId = tenant!.Id,
            PeriodEnd = PeriodEnd,
            ImportMode = UploadImportMode.Classic,
            CreatedAt = DateTimeOffset.UtcNow,
            Status = UploadBatchStatuses.Draft
        };

        _db.UploadBatches.Add(classicBatch);
        await _db.SaveChangesAsync();
        return RedirectToPage("Details", new { id = classicBatch.Id });
    }
}

