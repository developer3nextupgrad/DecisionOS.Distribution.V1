using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.VerticalLibraries;

public class IndexModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public IndexModel(DecisionOsDbContext db) => _db = db;

    public IReadOnlyList<Domain.VerticalLibrary> Items { get; private set; } = Array.Empty<Domain.VerticalLibrary>();

    public async Task OnGetAsync()
    {
        Items = await _db.VerticalLibraries
            .OrderByDescending(v => v.IsActive)
            .ThenBy(v => v.Name)
            .ToListAsync();
    }
}

