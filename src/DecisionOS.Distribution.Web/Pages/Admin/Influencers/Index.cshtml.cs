using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.Influencers;

public class IndexModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public IndexModel(DecisionOsDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public Guid? ProfileId { get; set; }

    public SelectList? ProfileOptions { get; private set; }
    public IReadOnlyList<Domain.InfluencerDefinition> Items { get; private set; } = Array.Empty<Domain.InfluencerDefinition>();

    public async Task OnGetAsync()
    {
        var profiles = await _db.BusinessProfiles
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync();
        ProfileOptions = new SelectList(profiles, "Id", "Name", ProfileId);

        Items = await _db.InfluencerDefinitions
            .Where(i => i.BusinessProfileId == ProfileId)
            .OrderBy(i => i.PillarCode)
            .ThenBy(i => i.DriverCode)
            .ThenBy(i => i.Weight)
            .ThenBy(i => i.InfluencerCode)
            .ToListAsync();
    }
}

