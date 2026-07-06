using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.Profiles;

public class ApplyDefaultsModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public ApplyDefaultsModel(DecisionOsDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public Domain.BusinessProfile? Profile { get; private set; }
    public int KpisCloned { get; private set; }
    public int DriversCloned { get; private set; }
    public int InfluencersCloned { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Profile = await _db.BusinessProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == Id);
        if (Profile is null) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Profile = await _db.BusinessProfiles.FirstOrDefaultAsync(p => p.Id == Id);
        if (Profile is null) return NotFound();

        var existingKpis = await _db.KpiDefinitions.AnyAsync(k => k.BusinessProfileId == Id);
        var existingDrivers = await _db.DriverDefinitions.AnyAsync(d => d.BusinessProfileId == Id);
        var existingInfluencers = await _db.InfluencerDefinitions.AnyAsync(i => i.BusinessProfileId == Id);

        if (!existingKpis)
        {
            var globals = await _db.KpiDefinitions.Where(k => k.BusinessProfileId == null).ToListAsync();
            foreach (var g in globals)
            {
                _db.KpiDefinitions.Add(new Domain.KpiDefinition
                {
                    BusinessProfileId = Id,
                    Code = g.Code,
                    Name = g.Name,
                    Unit = g.Unit,
                    Direction = g.Direction,
                    Target = g.Target,
                    AmberThreshold = g.AmberThreshold,
                    RedThreshold = g.RedThreshold,
                    MinValue = g.MinValue,
                    MaxValue = g.MaxValue,
                    AlertPriority = g.AlertPriority,
                    RecommendedAction = g.RecommendedAction,
                    DiagnosticChecks = g.DiagnosticChecks
                });
            }

            KpisCloned = globals.Count;
        }

        if (!existingDrivers)
        {
            var globals = await _db.DriverDefinitions.Where(d => d.BusinessProfileId == null).ToListAsync();
            foreach (var g in globals)
            {
                _db.DriverDefinitions.Add(new Domain.DriverDefinition
                {
                    BusinessProfileId = Id,
                    PillarCode = g.PillarCode,
                    DriverCode = g.DriverCode,
                    DisplayName = g.DisplayName,
                    Description = g.Description,
                    SortOrder = g.SortOrder,
                    IsActive = g.IsActive
                });
            }

            DriversCloned = globals.Count;
        }

        if (!existingInfluencers)
        {
            var globals = await _db.InfluencerDefinitions.Where(i => i.BusinessProfileId == null).ToListAsync();
            foreach (var g in globals)
            {
                _db.InfluencerDefinitions.Add(new Domain.InfluencerDefinition
                {
                    BusinessProfileId = Id,
                    PillarCode = g.PillarCode,
                    DriverCode = g.DriverCode,
                    InfluencerCode = g.InfluencerCode,
                    DisplayName = g.DisplayName,
                    Description = g.Description,
                    Weight = g.Weight,
                    Direction = g.Direction,
                    IsActive = g.IsActive
                });
            }

            InfluencersCloned = globals.Count;
        }

        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}

