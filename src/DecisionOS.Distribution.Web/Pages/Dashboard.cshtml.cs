using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Infrastructure;
using DecisionOS.Distribution.Infrastructure.Workbooks;
using DecisionOS.Distribution.Web.Pages.Shared;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages;

[Authorize(Policy = "AnyDistributionRole")]
public class DashboardModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    private readonly DashboardContextService _context;

    public DashboardModel(DecisionOsDbContext db, DashboardContextService context)
    {
        _db = db;
        _context = context;
    }

    [BindProperty(SupportsGet = true)] public string ClientId { get; set; } = null!;
    [BindProperty(SupportsGet = true)] public string PeriodEnd { get; set; } = null!;
    [BindProperty(SupportsGet = true)] public string? CustomerId { get; set; }

    public Tenant? Tenant { get; set; }
    public string? CustomerDisplayName { get; set; }
    public ContextSelectorViewModel Selector { get; set; } = new();
    public DateOnly ParsedPeriodEnd { get; set; }
    public List<KpiSnapshot> Snapshots { get; set; } = new();
    public Alert? TopAlert { get; set; }
    public List<DriverValue> Drivers { get; set; } = new();
    public bool HoldoverCarriedForward { get; set; }
    public DateOnly? HoldoverSourcePeriod { get; set; }
    public WeeklyFocus? Focus { get; set; }
    public int GreenCount { get; set; }
    public int YellowCount { get; set; }
    public int RedCount { get; set; }
    public int GrayCount { get; set; }
    public IReadOnlyDictionary<string, string> PillarDisplayNames { get; private set; } =
        new Dictionary<string, string>();

    public Dictionary<int, DashboardKpiInsight> KpiInsightsBySnapshotId { get; private set; } = new();
    public string KpiInsightsJson { get; private set; } = "{}";

    public DateOnly NextReviewDate => ParsedPeriodEnd.AddDays(7);

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(PeriodEnd))
            return RedirectToPage("Index");

        if (!DateOnly.TryParse(PeriodEnd, out var periodEnd))
            return RedirectToPage("Index");

        Tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.ClientId == ClientId);
        if (Tenant is null) return RedirectToPage("Index");

        if (!WorkbookDateRules.IsPlausiblePeriod(periodEnd))
        {
            var weeks = await _context.GetWeeksAsync(Tenant.Id, CustomerId);
            if (weeks.Count > 0)
            {
                return RedirectToPage(new
                {
                    clientId = ClientId,
                    periodEnd = weeks[0].ToString("yyyy-MM-dd"),
                    customerId = CustomerId
                });
            }
            return RedirectToPage("Index", new { selectedClientId = ClientId, selectedCustomerId = CustomerId });
        }

        ParsedPeriodEnd = periodEnd;

        var profileId = Tenant.BusinessProfileId;
        var kpiDefs = await _db.KpiDefinitions
            .AsNoTracking()
            .Where(d => d.BusinessProfileId == null || d.BusinessProfileId == profileId)
            .ToListAsync();

        var resolved = kpiDefs
            .GroupBy(d => d.Code, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.BusinessProfileId == profileId).First())
            .ToDictionary(d => d.Code, d => d.Name, StringComparer.OrdinalIgnoreCase);

        PillarDisplayNames = resolved;

        Snapshots = await _db.KpiSnapshots
            .Include(s => s.KpiDefinition)
            .Where(s => s.TenantId == Tenant.Id && s.PeriodEnd == periodEnd)
            .OrderByDescending(s => s.Status == "RED")
            .ThenByDescending(s => s.Status == "YELLOW")
            .ThenBy(s => s.KpiDefinition.Name)
            .ToListAsync();

        if (Snapshots.Count == 0)
        {
            var weeks = await _context.GetWeeksAsync(Tenant.Id, CustomerId);
            if (weeks.Count > 0 && weeks[0] != periodEnd)
            {
                return RedirectToPage(new
                {
                    clientId = ClientId,
                    periodEnd = weeks[0].ToString("yyyy-MM-dd"),
                    customerId = CustomerId
                });
            }
        }

        TopAlert = await _db.Alerts
            .Include(a => a.KpiDefinition)
            .FirstOrDefaultAsync(a => a.TenantId == Tenant.Id && a.PeriodEnd == periodEnd);

        Drivers = await _db.DriverValues
            .Where(d => d.TenantId == Tenant.Id && d.PeriodEnd == periodEnd)
            .OrderBy(d => d.PillarCode)
            .ThenBy(d => d.Rank)
            .Take(50)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(CustomerId))
        {
            CustomerDisplayName = await _context.ResolveCustomerDisplayNameAsync(Tenant.Id, CustomerId);
            Drivers = ApplyBuyerDriverFilter(Drivers, CustomerId, CustomerDisplayName);
        }

        if (Drivers.Count == 0)
        {
            var sourcePeriod = await _db.DriverValues.AsNoTracking()
                .Where(d => d.TenantId == Tenant.Id &&
                            d.PeriodEnd.Year >= WorkbookDateRules.MinPeriodYear &&
                            d.PeriodEnd.Year <= WorkbookDateRules.MaxPeriodYear)
                .OrderByDescending(d => d.PeriodEnd)
                .Select(d => d.PeriodEnd)
                .FirstOrDefaultAsync();

            if (sourcePeriod != default)
            {
                var carried = await _db.DriverValues
                    .Where(d => d.TenantId == Tenant.Id && d.PeriodEnd == sourcePeriod)
                    .OrderBy(d => d.PillarCode)
                    .ThenBy(d => d.Rank)
                    .Take(50)
                    .ToListAsync();

                if (!string.IsNullOrWhiteSpace(CustomerId))
                {
                    CustomerDisplayName ??= await _context.ResolveCustomerDisplayNameAsync(Tenant.Id, CustomerId);
                    carried = ApplyBuyerDriverFilter(carried, CustomerId, CustomerDisplayName);
                }

                if (carried.Count > 0)
                {
                    Drivers = carried;
                    HoldoverCarriedForward = sourcePeriod != periodEnd;
                    HoldoverSourcePeriod = sourcePeriod;
                }
            }
        }

        await BuildSelectorAsync(periodEnd);

        Focus = await _db.WeeklyFocuses
            .Include(w => w.KpiDefinition)
            .FirstOrDefaultAsync(w => w.TenantId == Tenant.Id && w.PeriodEnd == periodEnd);

        GreenCount = Snapshots.Count(s => s.Status == "GREEN");
        YellowCount = Snapshots.Count(s => s.Status == "YELLOW");
        RedCount = Snapshots.Count(s => s.Status == "RED");
        GrayCount = Snapshots.Count(s => s.Status == "GRAY");

        KpiInsightsBySnapshotId = DashboardKpiInsightBuilder.Build(
            Snapshots, Drivers, TopAlert, Focus, FormatValue, FormatTarget, FormatWoW);
        KpiInsightsJson = JsonSerializer.Serialize(
            KpiInsightsBySnapshotId,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        return Page();
    }

    private async Task BuildSelectorAsync(DateOnly periodEnd)
    {
        var tenants = await _db.Tenants.OrderBy(t => t.Name).ToListAsync();
        var tenantItems = tenants.Select(t => new SelectListItem(t.Name, t.ClientId)).ToList();
        tenantItems.Insert(0, new SelectListItem("— Select distributor —", ""));

        var customerItems = new List<SelectListItem>
        {
            new("All buyers (distributor total)", "")
        };
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

    public string FormatValue(KpiSnapshot snapshot)
    {
        if (snapshot.Status.Equals("GRAY", StringComparison.OrdinalIgnoreCase))
            return "—";

        if (snapshot.KpiDefinition.Unit == "pct")
            return (snapshot.Value * 100m).ToString("F1") + "%";
        if (snapshot.KpiDefinition.Unit == "days")
            return snapshot.Value.ToString("F0") + " days";
        return snapshot.Value.ToString("F2");
    }

    public string FormatTarget(KpiDefinition def)
    {
        if (def.Unit == "pct") return (def.Target * 100m).ToString("F1") + "%";
        if (def.Unit == "days") return def.Target.ToString("F0") + " days";
        return def.Target.ToString("F2");
    }

    public string FormatWoW(KpiSnapshot snapshot)
    {
        if (snapshot.WeekOverWeekDelta is null) return "\u2014";
        var delta = snapshot.WeekOverWeekDelta.Value;
        var prefix = delta >= 0 ? "+" : "";
        if (snapshot.KpiDefinition.Unit == "pct")
            return prefix + (delta * 100m).ToString("F1") + "pp";
        return prefix + delta.ToString("F1");
    }

    public string ProgressToneClass(string status) => status.ToUpperInvariant() switch
    {
        "GREEN" => "green",
        "YELLOW" => "amber",
        "RED" => "red",
        "GRAY" => "neutral",
        _ => "neutral"
    };

    public string PillarLabel(string pillarCode) =>
        PillarDisplayNames.TryGetValue(pillarCode, out var name) ? name : pillarCode;

    public string PillarBadgeStatus(DriverValue driver)
    {
        var snap = Snapshots.FirstOrDefault(s => s.KpiDefinition.Code == driver.PillarCode);
        return snap?.Status ?? driver.Status;
    }

    public string FormatHoldoverMetrics(DriverValue d)
    {
        if (!string.IsNullOrWhiteSpace(d.AssignedSummary) ||
            !string.IsNullOrWhiteSpace(d.TargetSummary) ||
            !string.IsNullOrWhiteSpace(d.CurrentSummary))
        {
            static string Cell(string? s) => string.IsNullOrWhiteSpace(s) ? "\u2014" : s.Trim();
            return $"{Cell(d.AssignedSummary)} | {Cell(d.TargetSummary)} | {Cell(d.CurrentSummary)}";
        }

        return d.Current.ToString("F2");
    }

    private static List<DriverValue> ApplyBuyerDriverFilter(
        List<DriverValue> drivers,
        string customerId,
        string? customerDisplayName)
    {
        var cid = customerId.Trim();
        var cname = customerDisplayName ?? cid;
        return drivers.Where(d =>
                string.IsNullOrWhiteSpace(d.Dimension1) ||
                string.Equals(d.Dimension1, cid, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(d.Dimension1, cname, StringComparison.OrdinalIgnoreCase) ||
                d.DriverName.Contains(cid, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(cname) &&
                 d.DriverName.Contains(cname, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public int FixProgressDisplayPercent(DriverValue d)
    {
        if (d.FixProgressPercent is >= 0 and <= 100)
            return d.FixProgressPercent.Value;

        return d.Status.ToUpperInvariant() switch
        {
            "GREEN" => 100,
            "YELLOW" => 55,
            "RED" => 25,
            _ => 0
        };
    }
}
