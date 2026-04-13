using System.ComponentModel.DataAnnotations;
using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.Tenants.Overrides;

public class DriversEditModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public DriversEditModel(DecisionOsDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public Guid TenantId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? PillarCode { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? DriverCode { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? DriverKey { get; set; }

    public Tenant? Tenant { get; private set; }
    public SelectList? DriverOptions { get; private set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        public string PillarCode { get; set; } = "";
        [Required]
        public string DriverCode { get; set; } = "";
        public bool? IsActive { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public int? SortOrder { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        Tenant = await _db.Tenants.Include(t => t.BusinessProfile).FirstOrDefaultAsync(t => t.Id == TenantId);
        if (Tenant is null) return NotFound();

        var resolver = new DefinitionResolver(_db);
        var effective = await resolver.ResolveDriverDefinitionsAsync(Tenant);

        var keys = effective
            .OrderBy(d => d.PillarCode)
            .ThenBy(d => d.SortOrder)
            .ThenBy(d => d.DisplayName)
            .Select(d => $"{d.PillarCode}::{d.DriverCode}")
            .ToList();
        DriverOptions = new SelectList(keys);

        var selected = !string.IsNullOrWhiteSpace(DriverKey)
            ? DriverKey
            : (string.IsNullOrWhiteSpace(PillarCode) || string.IsNullOrWhiteSpace(DriverCode)
                ? keys.FirstOrDefault()
                : $"{PillarCode}::{DriverCode}");
        if (string.IsNullOrWhiteSpace(selected)) return RedirectToPage("Drivers", new { tenantId = TenantId });

        var parts = selected.Split("::", StringSplitOptions.RemoveEmptyEntries);
        var pillar = parts[0];
        var driver = parts.Length > 1 ? parts[1] : "";

        DriverKey = $"{pillar}::{driver}";

        var ov = await _db.TenantDriverOverrides
            .FirstOrDefaultAsync(o => o.TenantId == TenantId && o.PillarCode == pillar && o.DriverCode == driver);

        Input = new InputModel
        {
            PillarCode = pillar,
            DriverCode = driver,
            IsActive = ov?.IsActive,
            DisplayName = ov?.DisplayName,
            Description = ov?.Description,
            SortOrder = ov?.SortOrder
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? action = null)
    {
        Tenant = await _db.Tenants.Include(t => t.BusinessProfile).FirstOrDefaultAsync(t => t.Id == TenantId);
        if (Tenant is null) return NotFound();

        var resolver = new DefinitionResolver(_db);
        var effective = await resolver.ResolveDriverDefinitionsAsync(Tenant);

        var keys = effective
            .OrderBy(d => d.PillarCode)
            .ThenBy(d => d.SortOrder)
            .ThenBy(d => d.DisplayName)
            .Select(d => $"{d.PillarCode}::{d.DriverCode}")
            .ToList();
        DriverOptions = new SelectList(keys);

        if (!ModelState.IsValid)
            return Page();

        var pillar = Input.PillarCode.Trim();
        var driver = Input.DriverCode.Trim();
        DriverKey = $"{pillar}::{driver}";
        if (!effective.Any(d => string.Equals(d.PillarCode, pillar, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(d.DriverCode, driver, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(string.Empty, "Unknown driver key for this tenant/profile.");
            return Page();
        }

        var ov = await _db.TenantDriverOverrides
            .FirstOrDefaultAsync(o => o.TenantId == TenantId && o.PillarCode == pillar && o.DriverCode == driver);

        if (string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase))
        {
            if (ov is not null)
            {
                _db.TenantDriverOverrides.Remove(ov);
                await _db.SaveChangesAsync();
            }
            return RedirectToPage("Drivers", new { tenantId = TenantId });
        }

        if (ov is null)
        {
            ov = new TenantDriverOverride { TenantId = TenantId, PillarCode = pillar, DriverCode = driver };
            _db.TenantDriverOverrides.Add(ov);
        }

        ov.IsActive = Input.IsActive;
        ov.DisplayName = string.IsNullOrWhiteSpace(Input.DisplayName) ? null : Input.DisplayName.Trim();
        ov.Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim();
        ov.SortOrder = Input.SortOrder;

        await _db.SaveChangesAsync();
        return RedirectToPage("Drivers", new { tenantId = TenantId });
    }
}

