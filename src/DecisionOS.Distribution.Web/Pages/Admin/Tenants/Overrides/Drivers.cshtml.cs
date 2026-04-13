using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.Tenants.Overrides;

public class DriversModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public DriversModel(DecisionOsDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public Guid TenantId { get; set; }

    public Tenant? Tenant { get; private set; }
    public IReadOnlyList<Row> Rows { get; private set; } = Array.Empty<Row>();

    public sealed record Row(
        string PillarCode,
        string DriverCode,
        string DisplayName,
        bool EffectiveIsActive,
        TenantDriverOverride? Override);

    public async Task<IActionResult> OnGetAsync()
    {
        Tenant = await _db.Tenants
            .Include(t => t.BusinessProfile)
            .FirstOrDefaultAsync(t => t.Id == TenantId);
        if (Tenant is null) return NotFound();

        var resolver = new DefinitionResolver(_db);
        var effective = await resolver.ResolveDriverDefinitionsAsync(Tenant);
        var overrides = await _db.TenantDriverOverrides.Where(o => o.TenantId == TenantId).ToListAsync();

        var byKey = overrides.ToDictionary(
            o => (Pillar: o.PillarCode.ToUpperInvariant(), Driver: o.DriverCode.ToUpperInvariant()),
            o => o);

        Rows = effective
            .OrderBy(d => d.PillarCode)
            .ThenBy(d => d.SortOrder)
            .ThenBy(d => d.DisplayName)
            .Select(d =>
            {
                var key = (Pillar: (d.PillarCode ?? "").ToUpperInvariant(), Driver: (d.DriverCode ?? "").ToUpperInvariant());
                byKey.TryGetValue(key, out var ov);
                return new Row(d.PillarCode ?? "", d.DriverCode ?? "", d.DisplayName, d.IsActive, ov);
            })
            .ToList();

        return Page();
    }
}

