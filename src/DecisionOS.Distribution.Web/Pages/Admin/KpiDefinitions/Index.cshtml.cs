using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.KpiDefinitions;

public class IndexModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public IndexModel(DecisionOsDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public Guid? ProfileId { get; set; }

    public SelectList? ProfileOptions { get; private set; }

    public IReadOnlyList<Domain.KpiDefinition> Items { get; private set; } = Array.Empty<Domain.KpiDefinition>();

    public async Task OnGetAsync()
    {
        var profiles = await _db.BusinessProfiles
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync();
        ProfileOptions = new SelectList(profiles, "Id", "Name", ProfileId);

        Items = await _db.KpiDefinitions
            .Where(k => k.BusinessProfileId == ProfileId)
            .OrderBy(k => k.AlertPriority)
            .ThenBy(k => k.Name)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var kpi = await _db.KpiDefinitions.FindAsync(id);

        if (kpi != null)
        {
            _db.KpiDefinitions.Remove(kpi);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage(new { ProfileId });
    }
}
