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

    [BindProperty(SupportsGet = true)] public string ClientId { get; set; } = null!;
    [BindProperty(SupportsGet = true)] public string PeriodEnd { get; set; } = null!;

    public Tenant? Tenant { get; set; }
    public DateOnly ParsedPeriodEnd { get; set; }
    public List<KpiSnapshot> Snapshots { get; set; } = new();
    public List<DriverDisplay> Drivers { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!DateOnly.TryParse(PeriodEnd, out var periodEnd)) return RedirectToPage("Index");
        ParsedPeriodEnd = periodEnd;

        Tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.ClientId == ClientId);
        if (Tenant is null) return RedirectToPage("Index");

        Snapshots = await _db.KpiSnapshots
            .Include(s => s.KpiDefinition)
            .Where(s => s.TenantId == Tenant.Id && s.PeriodEnd == periodEnd)
            .OrderByDescending(s => s.Status == "RED")
            .ThenByDescending(s => s.Status == "YELLOW")
            .ToListAsync();

        var driverRaw = await _db.DriverValues
            .Where(d => d.TenantId == Tenant.Id && d.PeriodEnd == periodEnd)
            .OrderBy(d => d.Rank)
            .ToListAsync();

        Drivers = driverRaw.Select(d => new DriverDisplay
        {
            DriverName = d.DriverName,
            Dimension1 = d.Dimension1,
            WhyItMatters = d.WhyItMatters,
            // d.Current is likely already decimal; if it is double, use (decimal)d.Current
            Current = (decimal)d.Current,
            // Fix: Use the 'm' suffix to indicate this is a decimal literal
            Target = 100.0m,
            Status = d.Status
        }).ToList();

        return Page();
    }

    // Ensure arithmetic uses decimal types consistently
    public string FormatValue(KpiSnapshot s) => s.KpiDefinition.Unit == "pct" ? (s.Value * 100m).ToString("F1") + "%" : s.Value.ToString("N1");
    public string FormatTarget(KpiDefinition d) => d.Unit == "pct" ? (d.Target * 100m).ToString("F1") + "%" : d.Target.ToString("N1");
    public string FormatWoW(KpiSnapshot s) => (s.WeekOverWeekDelta ?? 0m) >= 0m ? "+" + s.WeekOverWeekDelta?.ToString("F1") : s.WeekOverWeekDelta?.ToString("F1");

    public class DriverDisplay
    {
        public string DriverName { get; set; } = "";
        public string Dimension1 { get; set; } = "";
        public string WhyItMatters { get; set; } = "";
        // Changed to decimal to resolve conversion errors
        public decimal Current { get; set; }
        public decimal Target { get; set; }
        public string Status { get; set; } = "";
    }
}