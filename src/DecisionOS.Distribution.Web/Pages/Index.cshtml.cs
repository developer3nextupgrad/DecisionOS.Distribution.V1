using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages;

public class IndexModel : PageModel
{
    private readonly DecisionOsDbContext _db;

    public IndexModel(DecisionOsDbContext db) => _db = db;

    public List<SelectListItem> Tenants { get; set; } = new();
    public List<SelectListItem> Weeks { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SelectedClientId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedPeriodEnd { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var tenants = await _db.Tenants.OrderBy(t => t.Name).ToListAsync();
        Tenants = tenants.Select(t => new SelectListItem(t.Name, t.ClientId)).ToList();

        if (!string.IsNullOrEmpty(SelectedClientId))
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.ClientId == SelectedClientId);
            if (tenant is not null)
            {
                var weeks = await _db.KpiSnapshots
                    .Where(s => s.TenantId == tenant.Id)
                    .Select(s => s.PeriodEnd)
                    .Distinct()
                    .OrderByDescending(p => p)
                    .ToListAsync();
                Weeks = weeks.Select(w => new SelectListItem(w.ToString("yyyy-MM-dd"), w.ToString("yyyy-MM-dd"))).ToList();
            }

            if (!string.IsNullOrEmpty(SelectedPeriodEnd))
            {
                return RedirectToPage("Dashboard", new { clientId = SelectedClientId, periodEnd = SelectedPeriodEnd });
            }
        }

        return Page();
    }
}
