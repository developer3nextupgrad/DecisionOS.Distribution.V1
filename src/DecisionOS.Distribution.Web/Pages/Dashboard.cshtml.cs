using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Security;
using DecisionOS.Distribution.Infrastructure;
using DecisionOS.Distribution.Infrastructure.Workbooks;
using DecisionOS.Distribution.Domain.Workflow;
using DecisionOS.Distribution.Web.Pages.Shared;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages;

[Authorize(Policy = "AnyDistributionRole")]
public class DashboardModel : PageModel
{
    private static readonly string[] SevenPillarCodes =
    [
        "GrossMargin%", "AR_PastDue31p%", "AP_PastDue31p%", "DOH", "CCC", "NetProfit%", "PerfectOrderRate"
    ];

    private readonly DecisionOsDbContext _db;
    private readonly DashboardContextService _context;
    private readonly DecisionOsFeatureOptions _features;
    private readonly IHoldoverWorkflowService _workflow;

    public DashboardModel(
        DecisionOsDbContext db,
        DashboardContextService context,
        IOptions<DecisionOsFeatureOptions> features,
        IHoldoverWorkflowService workflow)
    {
        _db = db;
        _context = context;
        _features = features.Value;
        _workflow = workflow;
    }

    [BindProperty(SupportsGet = true)] public string ClientId { get; set; } = null!;
    [BindProperty(SupportsGet = true)] public string PeriodEnd { get; set; } = null!;
    [BindProperty(SupportsGet = true)] public string? CustomerId { get; set; }
    [BindProperty(SupportsGet = true)] public string? View { get; set; }
    [BindProperty(SupportsGet = true)] public int? DriverId { get; set; }

    public Tenant? Tenant { get; set; }
    public string? CustomerDisplayName { get; set; }
    public ContextSelectorViewModel Selector { get; set; } = new();
    public DateOnly ParsedPeriodEnd { get; set; }
    public List<KpiSnapshot> Snapshots { get; set; } = new();
    public Alert? TopAlert { get; set; }
    public List<DriverValue> Drivers { get; set; } = new();
    public List<ActionItem> ActionItems { get; set; } = new();
    public bool HoldoverCarriedForward { get; set; }
    public DateOnly? HoldoverSourcePeriod { get; set; }
    public WeeklyFocus? Focus { get; set; }
    public int GreenCount { get; set; }
    public int YellowCount { get; set; }
    public int RedCount { get; set; }
    public int GrayCount { get; set; }
    public IReadOnlyList<Domain.Routing.RoutingQueueItem> WatchlistItems { get; private set; } =
        Array.Empty<Domain.Routing.RoutingQueueItem>();
    public IReadOnlyDictionary<string, string> PillarDisplayNames { get; private set; } =
        new Dictionary<string, string>();

    public Dictionary<int, DashboardKpiInsight> KpiInsightsBySnapshotId { get; private set; } = new();
    public string KpiInsightsJson { get; private set; } = "{}";
    public string HoldoverInsightsJson { get; private set; } = "{}";
    public string AssignableUsersJson { get; private set; } = "[]";
    public bool WorkflowEnabled { get; private set; }
    public int? DeepLinkDriverId { get; private set; }

    public bool CatalogModeEnabled { get; private set; }
    public int TotalCatalogKpiCount { get; private set; }
    public bool CanManageKpiSelection { get; private set; }
    public bool HasPartialWeekData { get; private set; }
    public string? PreviousPeriodEnd { get; private set; }
    public string? NextPeriodEnd { get; private set; }

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
            var latestWithData = await _db.KpiSnapshots.AsNoTracking()
                .Where(s => s.TenantId == Tenant.Id)
                .Select(s => s.PeriodEnd)
                .Distinct()
                .OrderByDescending(p => p)
                .FirstOrDefaultAsync();

            if (latestWithData != default && latestWithData != periodEnd)
            {
                return RedirectToPage(new
                {
                    clientId = ClientId,
                    periodEnd = latestWithData.ToString("yyyy-MM-dd"),
                    customerId = CustomerId
                });
            }

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

        var resolvedDefs = kpiDefs
            .GroupBy(d => d.Code, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.BusinessProfileId == profileId).First())
            .ToList();

        CatalogModeEnabled = _features.Catalog.Enabled && _features.Scoring.UseCatalogEngine;
        TotalCatalogKpiCount = CatalogModeEnabled
            ? await _db.CatalogKpis.CountAsync()
            : 0;
        CanManageKpiSelection = User.IsInRole(AppRoles.Admin)
            || User.IsInRole(AppRoles.Operator)
            || User.IsInRole(AppRoles.Developer);

        var weekSnapshots = await _db.KpiSnapshots.AsNoTracking()
            .Where(s => s.TenantId == Tenant.Id && s.PeriodEnd == periodEnd)
            .ToListAsync();

        GreenCount = weekSnapshots.Count(s => s.Status == "GREEN");
        YellowCount = weekSnapshots.Count(s => s.Status == "YELLOW");
        RedCount = weekSnapshots.Count(s => s.Status == "RED");
        GrayCount = weekSnapshots.Count(s => s.Status == "GRAY");

        Snapshots = await LoadDashboardSnapshotsAsync(Tenant.Id, periodEnd, resolvedDefs);
        HasPartialWeekData = weekSnapshots.Any(s => s.Status.Equals("GRAY", StringComparison.OrdinalIgnoreCase))
            || Snapshots.Any(s => s.Status.Equals("GRAY", StringComparison.OrdinalIgnoreCase));

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

        var availableWeeks = await _context.GetWeeksAsync(Tenant.Id, CustomerId);
        var weekList = availableWeeks.ToList();
        var weekIndex = weekList.FindIndex(w => w == periodEnd);
        if (weekIndex >= 0 && weekIndex < weekList.Count - 1)
            PreviousPeriodEnd = weekList[weekIndex + 1].ToString("yyyy-MM-dd");
        if (weekIndex > 0)
            NextPeriodEnd = weekList[weekIndex - 1].ToString("yyyy-MM-dd");

        Focus = await _db.WeeklyFocuses
            .Include(w => w.KpiDefinition)
            .FirstOrDefaultAsync(w => w.TenantId == Tenant.Id && w.PeriodEnd == periodEnd);

        ActionItems = await _db.ActionItems
            .AsNoTracking()
            .Include(a => a.KpiDefinition)
            .Where(a => a.TenantId == Tenant.Id &&
                        (a.PeriodEnd == periodEnd || a.Status != ActionStatuses.Completed))
            .OrderByDescending(a => a.PeriodEnd)
            .ThenBy(a => a.Priority)
            .Take(20)
            .ToListAsync();

        KpiInsightsBySnapshotId = DashboardKpiInsightBuilder.Build(
            Snapshots, Drivers, TopAlert, Focus, FormatValue, FormatTarget, FormatWoW);
        KpiInsightsJson = JsonSerializer.Serialize(
            KpiInsightsBySnapshotId,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        WorkflowEnabled = _features.Workflow.AssignmentsEnabled;
        DeepLinkDriverId = DriverId;

        IReadOnlyDictionary<int, Guid?> assignees = new Dictionary<int, Guid?>();
        if (WorkflowEnabled && Drivers.Count > 0)
        {
            assignees = await _workflow.GetAssigneeIdsForDriversAsync(
                Tenant.Id,
                Drivers.Select(d => d.Id).ToList());
            var users = await _workflow.GetAssignableUsersAsync();
            AssignableUsersJson = JsonSerializer.Serialize(
                users,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }

        var holdoverInsights = Drivers.ToDictionary(
            d => d.Id,
            d => new
            {
                d.Id,
                pillarCode = d.PillarCode,
                pillarLabel = PillarLabel(d.PillarCode),
                d.DriverName,
                dimension1 = d.Dimension1,
                dimension2 = d.Dimension2,
                owner = d.Owner ?? "",
                assigneeUserId = assignees.TryGetValue(d.Id, out var uid) ? uid : null,
                assignedSummary = d.AssignedSummary ?? "",
                targetSummary = d.TargetSummary ?? "",
                currentSummary = d.CurrentSummary ?? "",
                status = d.Status,
                statusLabel = OwnerLanguage.PlainStatusLabel(d.Status),
                fixProgressPercent = FixProgressDisplayPercent(d),
                whyItMatters = OwnerLanguage.ExpandFinanceAbbreviations(d.WhyItMatters) ?? "",
                context = d.Context ?? "",
                metrics = FormatHoldoverMetrics(d)
            });
        HoldoverInsightsJson = JsonSerializer.Serialize(
            holdoverInsights,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        if (_features.Routing.Enabled)
        {
            WatchlistItems = await _db.RoutingQueueItems.AsNoTracking()
                .Where(q => q.TenantId == Tenant.Id && q.PeriodEnd == periodEnd
                    && q.QueueType == Domain.Routing.RoutingQueueTypes.Watchlist)
                .OrderByDescending(q => q.FinalScore)
                .Take(10)
                .ToListAsync();
        }

        return Page();
    }

    private async Task<List<KpiSnapshot>> LoadDashboardSnapshotsAsync(
        Guid tenantId,
        DateOnly periodEnd,
        IReadOnlyList<KpiDefinition> resolvedDefs)
    {
        var all = await _db.KpiSnapshots
            .Include(s => s.KpiDefinition)
            .Where(s => s.TenantId == tenantId && s.PeriodEnd == periodEnd)
            .ToListAsync();

        var catalogMode = _features.Catalog.Enabled && _features.Scoring.UseCatalogEngine;
        var useTop7 = _features.Scoring.UseDynamicTop7 || catalogMode;

        if (useTop7)
        {
            var topRanks = await _db.IssuePriorityScores.AsNoTracking()
                .Where(s => s.TenantId == tenantId && s.PeriodEnd == periodEnd && s.Rank <= 7)
                .OrderBy(s => s.Rank)
                .ToListAsync();

            if (topRanks.Count > 0)
            {
                var byDefId = all.ToDictionary(s => s.KpiDefinitionId);
                var ordered = new List<KpiSnapshot>();
                foreach (var rank in topRanks)
                {
                    if (rank.KpiDefinitionId is not null && byDefId.TryGetValue(rank.KpiDefinitionId.Value, out var snap))
                        ordered.Add(snap);
                }
                if (ordered.Count > 0)
                    return catalogMode ? ordered.Take(7).ToList() : EnsureSevenPillarDisplay(ordered, resolvedDefs, tenantId, periodEnd);
            }

            if (catalogMode && all.Count > 0)
            {
                return all
                    .OrderByDescending(s => s.Status == "RED")
                    .ThenByDescending(s => s.Status == "YELLOW")
                    .ThenByDescending(s => s.Status == "GREEN")
                    .ThenBy(s => s.KpiDefinition.Name)
                    .Take(7)
                    .ToList();
            }
        }

        var display = all
            .OrderByDescending(s => s.Status == "RED")
            .ThenByDescending(s => s.Status == "YELLOW")
            .ThenBy(s => s.KpiDefinition.Name)
            .ToList();
        return catalogMode ? display.Take(7).ToList() : EnsureSevenPillarDisplay(display, resolvedDefs, tenantId, periodEnd);
    }

    private static List<KpiSnapshot> EnsureSevenPillarDisplay(
        List<KpiSnapshot> existing,
        IReadOnlyList<KpiDefinition> defs,
        Guid tenantId,
        DateOnly periodEnd)
    {
        var byCode = existing.ToDictionary(s => s.KpiDefinition.Code, StringComparer.OrdinalIgnoreCase);
        var defsByCode = defs.ToDictionary(d => d.Code, StringComparer.OrdinalIgnoreCase);
        var result = new List<KpiSnapshot>(existing);
        var tempId = -1;

        foreach (var code in SevenPillarCodes)
        {
            if (byCode.ContainsKey(code)) continue;
            if (!defsByCode.TryGetValue(code, out var def)) continue;

            var missingItems = DashboardKpiInsight.MissingDataByCode.TryGetValue(code, out var items)
                ? items
                : Array.Empty<string>();

            result.Add(new KpiSnapshot
            {
                Id = tempId--,
                TenantId = tenantId,
                PeriodEnd = periodEnd,
                KpiDefinitionId = def.Id,
                KpiDefinition = def,
                Value = 0m,
                Status = "GRAY",
                DataConfidence = "Low",
                CardDetailLine1 = missingItems.Length > 0 ? missingItems[0] : "Insufficient data for this KPI.",
                CardDetailLine2 = "Other KPIs for this week still display when data is available."
            });
        }

        return result
            .OrderByDescending(s => s.Status == "RED")
            .ThenByDescending(s => s.Status == "YELLOW")
            .ThenBy(s => s.KpiDefinition.Name)
            .ToList();
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

    public static string FormatActionStatus(string status) => status switch
    {
        ActionStatuses.NotStarted => "Not started",
        ActionStatuses.InProgress => "In progress",
        ActionStatuses.AtRisk => "At risk",
        ActionStatuses.Completed => "Completed",
        ActionStatuses.Deferred => "Deferred",
        ActionStatuses.Blocked => "Blocked",
        _ => status
    };

    public static IReadOnlyList<string> ActionStatusOptions { get; } =
    [
        ActionStatuses.NotStarted,
        ActionStatuses.InProgress,
        ActionStatuses.AtRisk,
        ActionStatuses.Completed,
        ActionStatuses.Deferred,
        ActionStatuses.Blocked
    ];
}
