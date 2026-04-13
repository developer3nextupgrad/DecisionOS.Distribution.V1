using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.Tenants.Overrides;

public class KpisModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public KpisModel(DecisionOsDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public Guid TenantId { get; set; }

    public Tenant? Tenant { get; private set; }
    public IReadOnlyList<Row> Rows { get; private set; } = Array.Empty<Row>();

    public sealed record Row(
        string KpiCode,
        string KpiName,
        KpiDefinition Effective,
        TenantKpiOverride? Override);

    public async Task<IActionResult> OnGetAsync()
    {
        Tenant = await _db.Tenants
            .Include(t => t.BusinessProfile)
            .FirstOrDefaultAsync(t => t.Id == TenantId);
        if (Tenant is null) return NotFound();

        var resolver = new DefinitionResolver(_db);
        var effective = await resolver.ResolveKpiDefinitionsAsync(Tenant);
        var overrides = await _db.TenantKpiOverrides
            .Where(o => o.TenantId == TenantId)
            .ToListAsync();
        var byCode = overrides.ToDictionary(o => o.KpiCode, StringComparer.OrdinalIgnoreCase);

        Rows = effective.Values
            .OrderBy(d => d.AlertPriority)
            .ThenBy(d => d.Name)
            .Select(d => new Row(
                d.Code,
                d.Name,
                d,
                byCode.GetValueOrDefault(d.Code)))
            .ToList();

        return Page();
    }
}

