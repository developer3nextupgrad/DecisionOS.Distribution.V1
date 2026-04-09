using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.KpiDefinitions;

public class IndexModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public IndexModel(DecisionOsDbContext db) => _db = db;

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public Guid? ProfileId { get; set; }

    public IReadOnlyList<Domain.KpiDefinition> Items { get; private set; } = Array.Empty<Domain.KpiDefinition>();

    public async Task OnGetAsync()
    {
        Items = await _db.KpiDefinitions
            .Where(k => k.BusinessProfileId == ProfileId)
            .OrderBy(k => k.AlertPriority)
            .ThenBy(k => k.Name)
            .ToListAsync();
    }
}
