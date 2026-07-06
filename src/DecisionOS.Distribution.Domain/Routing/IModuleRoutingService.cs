using DecisionOS.Distribution.Domain.Scoring;

namespace DecisionOS.Distribution.Domain.Routing;

public interface IModuleRoutingService
{
    Task<int> RouteIssuesAsync(
        Guid tenantId,
        DateOnly periodEnd,
        IReadOnlyList<IssuePriorityScore> priorityScores,
        IReadOnlyList<KpiSnapshot> snapshots,
        CancellationToken ct = default);
}
