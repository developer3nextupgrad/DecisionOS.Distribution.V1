using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.Profiles;

public class IndexModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public IndexModel(DecisionOsDbContext db) => _db = db;

    public IReadOnlyList<Domain.BusinessProfile> Items { get; private set; } = Array.Empty<Domain.BusinessProfile>();
    public IReadOnlyDictionary<Guid, string> VerticalNameById { get; private set; } = new Dictionary<Guid, string>();

    public async Task OnGetAsync()
    {
        VerticalNameById = await _db.VerticalLibraries
            .AsNoTracking()
            .ToDictionaryAsync(v => v.Id, v => v.Name);

        Items = await _db.BusinessProfiles
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }
}

