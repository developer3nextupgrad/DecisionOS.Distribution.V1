using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages;

[Authorize(Policy = "AnyDistributionRole")]
public class DashboardModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public DashboardModel(DecisionOsDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)] public string ClientId { get; set; } = null!;
    [BindProperty(SupportsGet = true)] public string PeriodEnd { get; set; } = null!;

    public Tenant? Tenant { get; set; }
    public DateOnly ParsedPeriodEnd { get; set; }
    public List<KpiSnapshot> Snapshots { get; set; } = new();
    public Alert? TopAlert { get; set; }
    public List<DriverValue> Drivers { get; set; } = new();
    public WeeklyFocus? Focus { get; set; }
    public int GreenCount { get; set; }
    public int YellowCount { get; set; }
    public int RedCount { get; set; }
    public IReadOnlyDictionary<string, string> PillarDisplayNames { get; private set; } =
        new Dictionary<string, string>();

    public DateOnly NextReviewDate => ParsedPeriodEnd.AddDays(7);

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(PeriodEnd))
            return RedirectToPage("Index");

        if (!DateOnly.TryParse(PeriodEnd, out var periodEnd))
            return RedirectToPage("Index");

        ParsedPeriodEnd = periodEnd;

        Tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.ClientId == ClientId);
        if (Tenant is null) return RedirectToPage("Index");

        PillarDisplayNames = await _db.KpiDefinitions.AsNoTracking().ToDictionaryAsync(d => d.Code, d => d.Name);

        Snapshots = await _db.KpiSnapshots
            .Include(s => s.KpiDefinition)
            .Where(s => s.TenantId == Tenant.Id && s.PeriodEnd == periodEnd)
            .OrderByDescending(s => s.Status == "RED")
            .ThenByDescending(s => s.Status == "YELLOW")
            .ThenBy(s => s.KpiDefinition.Name)
            .ToListAsync();

        TopAlert = await _db.Alerts
            .Include(a => a.KpiDefinition)
            .FirstOrDefaultAsync(a => a.TenantId == Tenant.Id && a.PeriodEnd == periodEnd);

        Drivers = await _db.DriverValues
            .Where(d => d.TenantId == Tenant.Id && d.PeriodEnd == periodEnd)
            .OrderBy(d => d.PillarCode)
            .ThenBy(d => d.Rank)
            .Take(50)
            .ToListAsync();

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
