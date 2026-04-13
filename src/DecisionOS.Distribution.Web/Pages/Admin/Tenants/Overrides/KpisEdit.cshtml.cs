using System.ComponentModel.DataAnnotations;
using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.Tenants.Overrides;

public class KpisEditModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public KpisEditModel(DecisionOsDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public Guid TenantId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? KpiCode { get; set; }

    public Tenant? Tenant { get; private set; }
    public SelectList? KpiOptions { get; private set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        public string KpiCode { get; set; } = "";
        public bool IsActive { get; set; } = true;

        public decimal? Target { get; set; }
        public decimal? AmberThreshold { get; set; }
        public decimal? RedThreshold { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public int? AlertPriority { get; set; }
        public string? RecommendedAction { get; set; }
        public string? DiagnosticChecks { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        Tenant = await _db.Tenants.Include(t => t.BusinessProfile).FirstOrDefaultAsync(t => t.Id == TenantId);
        if (Tenant is null) return NotFound();

        var resolver = new DefinitionResolver(_db);
        var effective = await resolver.ResolveKpiDefinitionsAsync(Tenant);
        KpiOptions = new SelectList(effective.Values.OrderBy(x => x.AlertPriority).Select(x => x.Code).ToList());

        var code = string.IsNullOrWhiteSpace(KpiCode) ? effective.Keys.OrderBy(x => x).FirstOrDefault() : KpiCode.Trim();
        if (string.IsNullOrWhiteSpace(code)) return RedirectToPage("Kpis", new { tenantId = TenantId });

        var ov = await _db.TenantKpiOverrides.FirstOrDefaultAsync(o => o.TenantId == TenantId && o.KpiCode == code);
        Input = new InputModel
        {
            KpiCode = code,
            IsActive = ov?.IsActive ?? true,
            Target = ov?.Target,
            AmberThreshold = ov?.AmberThreshold,
            RedThreshold = ov?.RedThreshold,
            MinValue = ov?.MinValue,
            MaxValue = ov?.MaxValue,
            AlertPriority = ov?.AlertPriority,
            RecommendedAction = ov?.RecommendedAction,
            DiagnosticChecks = ov?.DiagnosticChecks
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? action = null)
    {
        Tenant = await _db.Tenants.Include(t => t.BusinessProfile).FirstOrDefaultAsync(t => t.Id == TenantId);
        if (Tenant is null) return NotFound();

        var resolver = new DefinitionResolver(_db);
        var effective = await resolver.ResolveKpiDefinitionsAsync(Tenant);
        KpiOptions = new SelectList(effective.Values.OrderBy(x => x.AlertPriority).Select(x => x.Code).ToList());

        if (!ModelState.IsValid)
            return Page();

        var code = Input.KpiCode.Trim();
        if (!effective.ContainsKey(code))
        {
            ModelState.AddModelError(nameof(Input.KpiCode), "Unknown KPI code for this tenant/profile.");
            return Page();
        }

        var ov = await _db.TenantKpiOverrides.FirstOrDefaultAsync(o => o.TenantId == TenantId && o.KpiCode == code);
        if (string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase))
        {
            if (ov is not null)
            {
                _db.TenantKpiOverrides.Remove(ov);
                await _db.SaveChangesAsync();
            }
            return RedirectToPage("Kpis", new { tenantId = TenantId });
        }

        if (ov is null)
        {
            ov = new TenantKpiOverride { TenantId = TenantId, KpiCode = code };
            _db.TenantKpiOverrides.Add(ov);
        }

        ov.IsActive = Input.IsActive;
        ov.Target = Input.Target;
        ov.AmberThreshold = Input.AmberThreshold;
        ov.RedThreshold = Input.RedThreshold;
        ov.MinValue = Input.MinValue;
        ov.MaxValue = Input.MaxValue;
        ov.AlertPriority = Input.AlertPriority;
        ov.RecommendedAction = string.IsNullOrWhiteSpace(Input.RecommendedAction) ? null : Input.RecommendedAction.Trim();
        ov.DiagnosticChecks = string.IsNullOrWhiteSpace(Input.DiagnosticChecks) ? null : Input.DiagnosticChecks.Trim();

        await _db.SaveChangesAsync();
        return RedirectToPage("Kpis", new { tenantId = TenantId });
    }
}

