using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Scoring;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.Tenants.Overrides;

public class CatalogKpisModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public CatalogKpisModel(DecisionOsDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public Guid TenantId { get; set; }

    public Tenant? Tenant { get; private set; }
    public IReadOnlyList<Row> Rows { get; private set; } = Array.Empty<Row>();

    public sealed record Row(string CatalogKpiId, string Name, string? LegacyCode, bool IsPinned, bool IsExcluded);

    public async Task<IActionResult> OnGetAsync()
    {
        Tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == TenantId);
        if (Tenant is null) return NotFound();

        var selections = await _db.TenantKpiSelections
            .Where(s => s.TenantId == TenantId)
            .ToDictionaryAsync(s => s.CatalogKpiId);

        Rows = await _db.CatalogKpis.AsNoTracking()
            .Where(k => k.MgmtLayerCandidate)
            .OrderBy(k => k.KpiId)
            .Select(k => new Row(
                k.KpiId,
                k.Name,
                k.LegacyCode,
                selections.ContainsKey(k.KpiId) && selections[k.KpiId].IsPinned,
                selections.ContainsKey(k.KpiId) && selections[k.KpiId].IsExcluded))
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostToggleAsync(string catalogKpiId, string action)
    {
        Tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == TenantId);
        if (Tenant is null) return NotFound();

        var sel = await _db.TenantKpiSelections
            .FirstOrDefaultAsync(s => s.TenantId == TenantId && s.CatalogKpiId == catalogKpiId);

        if (sel is null)
        {
            sel = new TenantKpiSelection { TenantId = TenantId, CatalogKpiId = catalogKpiId };
            _db.TenantKpiSelections.Add(sel);
        }

        if (action == "pin")
        {
            sel.IsPinned = !sel.IsPinned;
            if (sel.IsPinned) sel.IsExcluded = false;
        }
        else if (action == "exclude")
        {
            sel.IsExcluded = !sel.IsExcluded;
            if (sel.IsExcluded) sel.IsPinned = false;
        }

        await _db.SaveChangesAsync();
        return RedirectToPage(new { tenantId = TenantId });
    }
}
