using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Routing;
using DecisionOS.Distribution.Domain.Scoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DecisionOS.Distribution.Infrastructure.Routing;

public sealed class ModuleRoutingService : IModuleRoutingService
{
    private readonly DecisionOsDbContext _db;
    private readonly DecisionOsFeatureOptions _features;

    public ModuleRoutingService(DecisionOsDbContext db, IOptions<DecisionOsFeatureOptions> features)
    {
        _db = db;
        _features = features.Value;
    }

    public async Task<int> RouteIssuesAsync(
        Guid tenantId,
        DateOnly periodEnd,
        IReadOnlyList<IssuePriorityScore> priorityScores,
        IReadOnlyList<KpiSnapshot> snapshots,
        CancellationToken ct = default)
    {
        if (!_features.Routing.Enabled) return 0;

        _db.RoutingQueueItems.RemoveRange(
            _db.RoutingQueueItems.Where(q => q.TenantId == tenantId && q.PeriodEnd == periodEnd));

        var modules = await _db.CatalogModules.AsNoTracking().ToListAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var items = new List<RoutingQueueItem>();

        foreach (var snap in snapshots.Where(s => s.Status == "GRAY"))
        {
            items.Add(new RoutingQueueItem
            {
                TenantId = tenantId,
                PeriodEnd = periodEnd,
                QueueType = RoutingQueueTypes.DataGap,
                Title = $"{snap.KpiDefinition?.Name ?? "KPI"} — missing data",
                Severity = snap.Status,
                Status = "Open",
                CreatedAt = now
            });
        }

        foreach (var score in priorityScores)
        {
            var snap = snapshots.FirstOrDefault(s => s.KpiDefinitionId == score.KpiDefinitionId);
            var title = snap?.KpiDefinition?.Name ?? score.CatalogKpiId ?? "Issue";

            if (score.ConfidenceScore < 40m)
            {
                items.Add(new RoutingQueueItem
                {
                    TenantId = tenantId,
                    PeriodEnd = periodEnd,
                    QueueType = RoutingQueueTypes.Review,
                    CatalogKpiId = score.CatalogKpiId,
                    Title = $"{title} — low confidence review",
                    Severity = score.Status,
                    FinalScore = score.FinalScore,
                    Status = "Open",
                    CreatedAt = now
                });
            }

            if (score.Rank is >= 1 and <= 7)
            {
                items.Add(new RoutingQueueItem
                {
                    TenantId = tenantId,
                    PeriodEnd = periodEnd,
                    QueueType = RoutingQueueTypes.Management,
                    CatalogKpiId = score.CatalogKpiId,
                    Title = title,
                    Severity = score.Status,
                    FinalScore = score.FinalScore,
                    Status = "Open",
                    CreatedAt = now
                });
            }
            else if (score.Rank is >= 8 and <= 20)
            {
                items.Add(new RoutingQueueItem
                {
                    TenantId = tenantId,
                    PeriodEnd = periodEnd,
                    QueueType = RoutingQueueTypes.Watchlist,
                    CatalogKpiId = score.CatalogKpiId,
                    Title = title,
                    Severity = score.Status,
                    FinalScore = score.FinalScore,
                    Status = "Open",
                    CreatedAt = now
                });
            }
            else if (score.Rank > 7)
            {
                items.Add(new RoutingQueueItem
                {
                    TenantId = tenantId,
                    PeriodEnd = periodEnd,
                    QueueType = RoutingQueueTypes.DrillDown,
                    CatalogKpiId = score.CatalogKpiId,
                    Title = title,
                    Severity = score.Status,
                    FinalScore = score.FinalScore,
                    Status = "Open",
                    CreatedAt = now
                });
            }

            var module = modules.FirstOrDefault(m =>
                m.PrimaryKpis?.Contains(score.CatalogKpiId ?? "", StringComparison.OrdinalIgnoreCase) == true
                || (snap?.KpiDefinition?.Code is not null
                    && m.PrimaryKpis?.Contains(snap.KpiDefinition.Code, StringComparison.OrdinalIgnoreCase) == true));

            if (module is not null)
            {
                items.Add(new RoutingQueueItem
                {
                    TenantId = tenantId,
                    PeriodEnd = periodEnd,
                    QueueType = RoutingQueueTypes.ModuleAction,
                    CatalogKpiId = score.CatalogKpiId,
                    ModuleCode = module.ModuleCode,
                    Title = $"{module.Name}: {title}",
                    Severity = score.Status,
                    FinalScore = score.FinalScore,
                    Status = "Open",
                    CreatedAt = now
                });
            }
        }

        if (items.Count == 0) return 0;
        _db.RoutingQueueItems.AddRange(items);
        await _db.SaveChangesAsync(ct);
        return items.Count;
    }
}
