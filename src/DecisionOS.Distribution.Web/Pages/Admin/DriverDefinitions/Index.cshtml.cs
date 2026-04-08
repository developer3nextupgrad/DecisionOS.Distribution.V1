using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.DriverDefinitions;

public class IndexModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public IndexModel(DecisionOsDbContext db) => _db = db;

    public IReadOnlyList<Domain.DriverDefinition> Items { get; private set; } = Array.Empty<Domain.DriverDefinition>();

    public async Task OnGetAsync()
    {
        Items = await _db.DriverDefinitions
            .OrderBy(d => d.PillarCode)
            .ThenBy(d => d.SortOrder)
            .ThenBy(d => d.DriverCode)
            .ToListAsync();
    }
}
