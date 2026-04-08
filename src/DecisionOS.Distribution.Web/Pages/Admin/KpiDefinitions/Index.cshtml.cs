using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.KpiDefinitions;

public class IndexModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public IndexModel(DecisionOsDbContext db) => _db = db;

    public IReadOnlyList<Domain.KpiDefinition> Items { get; private set; } = Array.Empty<Domain.KpiDefinition>();

    public async Task OnGetAsync()
    {
        Items = await _db.KpiDefinitions.OrderBy(k => k.AlertPriority).ThenBy(k => k.Name).ToListAsync();
    }
}
