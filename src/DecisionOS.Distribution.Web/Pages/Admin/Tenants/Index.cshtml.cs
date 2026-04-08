using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.Tenants;

public class IndexModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public IndexModel(DecisionOsDbContext db) => _db = db;

    public IReadOnlyList<Domain.Tenant> Items { get; private set; } = Array.Empty<Domain.Tenant>();

    public async Task OnGetAsync()
    {
        Items = await _db.Tenants.OrderBy(t => t.Name).ToListAsync();
    }
}
