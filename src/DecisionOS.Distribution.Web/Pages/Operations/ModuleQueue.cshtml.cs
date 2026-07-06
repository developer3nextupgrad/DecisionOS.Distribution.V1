using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Operations;

public class ModuleQueueModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public ModuleQueueModel(DecisionOsDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public string? ClientId { get; set; }

    public IReadOnlyList<Domain.Routing.RoutingQueueItem> Items { get; private set; } =
        Array.Empty<Domain.Routing.RoutingQueueItem>();

    public async Task OnGetAsync()
    {
        var query = _db.RoutingQueueItems.AsNoTracking()
            .Where(q => q.QueueType == Domain.Routing.RoutingQueueTypes.ModuleAction);

        if (!string.IsNullOrWhiteSpace(ClientId))
        {
            var tenant = await _db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.ClientId == ClientId);
            if (tenant is not null)
                query = query.Where(q => q.TenantId == tenant.Id);
        }

        Items = await query
            .OrderByDescending(q => q.CreatedAt)
            .Take(100)
            .ToListAsync();
    }
}
