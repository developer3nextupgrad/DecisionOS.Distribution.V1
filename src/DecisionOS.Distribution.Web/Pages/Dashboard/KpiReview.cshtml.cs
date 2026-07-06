using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Scoring;
using DecisionOS.Distribution.Domain.Security;
using DecisionOS.Distribution.Infrastructure;
using DecisionOS.Distribution.Web.Pages.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DecisionOS.Distribution.Web.Pages.Dashboard;

[Authorize(Policy = "AnyDistributionRole")]
public class KpiReviewModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    private readonly DashboardContextService _context;
    private readonly DecisionOsFeatureOptions _features;

    public KpiReviewModel(
        DecisionOsDbContext db,
        DashboardContextService context,
        IOptions<DecisionOsFeatureOptions> features)
    {
        _db = db;
        _context = context;
        _features = features.Value;
    }

    [BindProperty(SupportsGet = true)] public string ClientId { get; set; } = null!;
    [BindProperty(SupportsGet = true)] public string PeriodEnd { get; set; } = null!;
    [BindProperty(SupportsGet = true)] public string? CustomerId { get; set; }

    public Tenant? Tenant { get; private set; }
    public DateOnly ParsedPeriodEnd { get; private set; }
    public ContextSelectorViewModel Selector { get; set; } = new();
    public IReadOnlyList<Row> Rows { get; private set; } = Array.Empty<Row>();
    public bool CatalogModeEnabled { get; private set; }
    public bool CanManageSelection { get; private set; }
    public string? StatusMessage { get; private set; }

    public sealed record Row(
        string CatalogKpiId,
        string Name,
        string? LegacyCode,
        string Status,
        decimal? Value,
        string ValueDisplay,
        int? Rank,
        decimal? FinalScore,
        bool IsTop7,
        bool IsPinned,
        bool IsExcluded,
        string? DataNeeds);

    public async Task<IActionResult> OnGetAsync(string? message = null)
    {
        StatusMessage = message;
        if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(PeriodEnd))
            return RedirectToPage("/Index");

        if (!DateOnly.TryParse(PeriodEnd, out var periodEnd))
            return RedirectToPage("/Index");

        Tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.ClientId == ClientId);
        if (Tenant is null) return RedirectToPage("/Index");

        ParsedPeriodEnd = periodEnd;
        CatalogModeEnabled = _features.Catalog.Enabled && _features.Scoring.UseCatalogEngine;
        CanManageSelection = User.IsInRole(AppRoles.Admin)
            || User.IsInRole(AppRoles.Operator)
            || User.IsInRole(AppRoles.Developer);

        var snapshots = await _db.KpiSnapshots.AsNoTracking()
            .Include(s => s.KpiDefinition)
            .Where(s => s.TenantId == Tenant.Id && s.PeriodEnd == periodEnd)
            .ToListAsync();

        var snapByCode = snapshots.ToDictionary(s => s.KpiDefinition.Code, StringComparer.OrdinalIgnoreCase);
        var scores = await _db.IssuePriorityScores.AsNoTracking()
            .Where(s => s.TenantId == Tenant.Id && s.PeriodEnd == periodEnd)
            .ToDictionaryAsync(s => s.KpiDefinitionId ?? 0);

        var selections = await _db.TenantKpiSelections.AsNoTracking()
            .Where(s => s.TenantId == Tenant.Id)
            .ToDictionaryAsync(s => s.CatalogKpiId);

        var catalogKpis = await _db.CatalogKpis.AsNoTracking()
            .OrderBy(k => k.KpiId)
            .ToListAsync();

        Rows = catalogKpis.Select(ck =>
        {
            var code = ck.LegacyCode ?? ck.KpiId;
            snapByCode.TryGetValue(code, out var snap);
            IssuePriorityScore? score = null;
            if (snap is not null)
                scores.TryGetValue(snap.KpiDefinitionId, out score);

            selections.TryGetValue(ck.KpiId, out var sel);
            var rank = score?.Rank;
            var isTop7 = rank is > 0 and <= 7;

            return new Row(
                ck.KpiId,
                ck.Name,
                ck.LegacyCode,
                snap?.Status ?? "GRAY",
                snap?.Status is "GRAY" ? null : snap?.Value,
                FormatValue(snap),
                rank,
                score?.FinalScore,
                isTop7,
                sel?.IsPinned == true,
                sel?.IsExcluded == true,
                ck.PrimaryDataNeeds);
        }).ToList();

        await BuildSelectorAsync(periodEnd);
        return Page();
    }

    public async Task<IActionResult> OnPostToggleAsync(string catalogKpiId, string action)
    {
        if (!User.IsInRole(AppRoles.Admin) && !User.IsInRole(AppRoles.Operator) && !User.IsInRole(AppRoles.Developer))
            return Forbid();

        Tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.ClientId == ClientId);
        if (Tenant is null) return RedirectToPage("/Index");

        var sel = await _db.TenantKpiSelections
            .FirstOrDefaultAsync(s => s.TenantId == Tenant.Id && s.CatalogKpiId == catalogKpiId);

        if (sel is null)
        {
            sel = new TenantKpiSelection { TenantId = Tenant.Id, CatalogKpiId = catalogKpiId };
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
        return RedirectToPage(new { clientId = ClientId, periodEnd = PeriodEnd, customerId = CustomerId, message = "saved" });
    }

    private async Task BuildSelectorAsync(DateOnly periodEnd)
    {
        var tenants = await _db.Tenants.OrderBy(t => t.Name).ToListAsync();
        var tenantItems = tenants.Select(t => new SelectListItem(t.Name, t.ClientId)).ToList();
        tenantItems.Insert(0, new SelectListItem("— Select distributor —", ""));

        var customerItems = new List<SelectListItem> { new("All buyers (distributor total)", "") };
        var weekItems = new List<SelectListItem> { new("— Select week —", "") };

        if (Tenant is not null)
        {
            var customers = await _context.GetCustomersAsync(Tenant.Id);
            customerItems.AddRange(customers.Select(c => new SelectListItem(c.DisplayName, c.CustomerId)));
            var weeks = await _context.GetWeeksAsync(Tenant.Id, CustomerId);
            weekItems.AddRange(weeks.Select(w => new SelectListItem(w.ToString("yyyy-MM-dd"), w.ToString("yyyy-MM-dd"))));
        }

        Selector = new ContextSelectorViewModel
        {
            Tenants = tenantItems,
            Customers = customerItems,
            Weeks = weekItems,
            SelectedClientId = ClientId,
            SelectedCustomerId = CustomerId ?? "",
            SelectedPeriodEnd = periodEnd.ToString("yyyy-MM-dd"),
            ShowGoButton = true
        };
    }

    private static string FormatValue(KpiSnapshot? snap)
    {
        if (snap is null || snap.Status.Equals("GRAY", StringComparison.OrdinalIgnoreCase))
            return "—";

        if (snap.KpiDefinition.Unit == "pct")
            return (snap.Value * 100m).ToString("F1") + "%";
        if (snap.KpiDefinition.Unit == "days")
            return snap.Value.ToString("F0") + " days";
        return snap.Value.ToString("F2");
    }

    public static string PlainStatus(string status) => OwnerLanguage.PlainStatusLabel(status);
}
