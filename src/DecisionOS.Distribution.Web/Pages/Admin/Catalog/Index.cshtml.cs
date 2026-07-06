using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.Catalog;

public class IndexModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public IndexModel(DecisionOsDbContext db) => _db = db;

    public int KpiCount { get; private set; }
    public int DriverCount { get; private set; }
    public int InfluencerCount { get; private set; }
    public int KpiDriverMapCount { get; private set; }
    public int DriverInfluencerMapCount { get; private set; }
    public int ScoreComponentCount { get; private set; }
    public int ModuleCount { get; private set; }

    public async Task OnGetAsync()
    {
        KpiCount = await _db.CatalogKpis.CountAsync();
        DriverCount = await _db.CatalogDrivers.CountAsync();
        InfluencerCount = await _db.CatalogInfluencers.CountAsync();
        KpiDriverMapCount = await _db.CatalogKpiDriverMaps.CountAsync();
        DriverInfluencerMapCount = await _db.CatalogDriverInfluencerMaps.CountAsync();
        ScoreComponentCount = await _db.CatalogScoreComponents.CountAsync();
        ModuleCount = await _db.CatalogModules.CountAsync();
    }
}
