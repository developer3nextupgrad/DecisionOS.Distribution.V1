using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.DriverDefinitions;

public class IndexModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public IndexModel(DecisionOsDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public Guid? ProfileId { get; set; }

    public SelectList? ProfileOptions { get; private set; }

    public IReadOnlyList<Domain.DriverDefinition> Items { get; private set; } = Array.Empty<Domain.DriverDefinition>();

    public async Task OnGetAsync()
    {
        var profiles = await _db.BusinessProfiles
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync();
        ProfileOptions = new SelectList(profiles, "Id", "Name", ProfileId);

        Items = await _db.DriverDefinitions
            .Where(d => d.BusinessProfileId == ProfileId)
            .OrderBy(d => d.PillarCode)
            .ThenBy(d => d.SortOrder)
            .ThenBy(d => d.DriverCode)
            .ToListAsync();
    }
}
