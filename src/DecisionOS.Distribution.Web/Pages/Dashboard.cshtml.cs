using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages;

public class DashboardModel : PageModel
{
    private readonly DecisionOsDbContext _db;

    public DashboardModel(DecisionOsDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public string ClientId { get; set; } = null!;

    [BindProperty(SupportsGet = true)]
    public string PeriodEnd { get; set; } = null!;

    public Tenant? Tenant { get; set; }
    public DateOnly ParsedPeriodEnd { get; set; }
    public List<KpiSnapshot> Snapshots { get; set; } = new();
    public Alert? TopAlert { get; set; }
    public List<DriverValue> Drivers { get; set; } = new();
    public WeeklyFocus? Focus { get; set; }
    public int GreenCount { get; set; }
    public int YellowCount { get; set; }
    public int RedCount { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(PeriodEnd))
            return RedirectToPage("Index");

        if (!DateOnly.TryParse(PeriodEnd, out var periodEnd))
            return RedirectToPage("Index");

        ParsedPeriodEnd = periodEnd;

        Tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.ClientId == ClientId);
        if (Tenant is null) return RedirectToPage("Index");

        Snapshots = await _db.KpiSnapshots
            .Include(s => s.KpiDefinition)
            .Where(s => s.TenantId == Tenant.Id && s.PeriodEnd == periodEnd)
            .OrderBy(s => s.KpiDefinition.Name)
            .ToListAsync();

        TopAlert = await _db.Alerts
            .Include(a => a.KpiDefinition)
            .FirstOrDefaultAsync(a => a.TenantId == Tenant.Id && a.PeriodEnd == periodEnd);

        var driversQuery = _db.DriverValues
            .Where(d => d.TenantId == Tenant.Id && d.PeriodEnd == periodEnd);
        if (TopAlert is not null)
            driversQuery = driversQuery.Where(d => d.PillarCode == TopAlert.KpiDefinition.Code);
        Drivers = await driversQuery.OrderBy(d => d.Rank).Take(10).ToListAsync();

        Focus = await _db.WeeklyFocuses
            .Include(w => w.KpiDefinition)
            .FirstOrDefaultAsync(w => w.TenantId == Tenant.Id && w.PeriodEnd == periodEnd);

        GreenCount = Snapshots.Count(s => s.Status == "GREEN");
        YellowCount = Snapshots.Count(s => s.Status == "YELLOW");
        RedCount = Snapshots.Count(s => s.Status == "RED");

        return Page();
    }

    public string FormatValue(KpiSnapshot snapshot)
    {
        if (snapshot.KpiDefinition.Unit == "pct")
            return (snapshot.Value * 100).ToString("F1") + "%";
        if (snapshot.KpiDefinition.Unit == "days")
            return snapshot.Value.ToString("F0") + " days";
        return snapshot.Value.ToString("F2");
    }

    public string FormatTarget(KpiDefinition def)
    {
        if (def.Unit == "pct") return (def.Target * 100).ToString("F1") + "%";
        if (def.Unit == "days") return def.Target.ToString("F0") + " days";
        return def.Target.ToString("F2");
    }

    public string FormatWoW(KpiSnapshot snapshot)
    {
        if (snapshot.WeekOverWeekDelta is null) return "\u2014";
        var delta = snapshot.WeekOverWeekDelta.Value;
        var prefix = delta >= 0 ? "+" : "";
        if (snapshot.KpiDefinition.Unit == "pct")
            return prefix + (delta * 100).ToString("F1") + "pp";
        return prefix + delta.ToString("F1");
    }

    public string WoWCssClass(KpiSnapshot snapshot)
    {
        if (snapshot.WeekOverWeekDelta is null) return "";
        var delta = snapshot.WeekOverWeekDelta.Value;
        var improving = snapshot.KpiDefinition.Direction == KpiDirection.HigherIsBetter ? delta > 0 : delta < 0;
        return improving ? "wow-positive" : "wow-negative";
    }

    public string StatusCssClass(string status) => status switch
    {
        "GREEN" => "status-green",
        "YELLOW" => "status-yellow",
        "RED" => "status-red",
        _ => ""
    };

    public string BorderCssClass(string status) => status switch
    {
        "GREEN" => "border-green",
        "YELLOW" => "border-yellow",
        "RED" => "border-red",
        _ => ""
    };
}
