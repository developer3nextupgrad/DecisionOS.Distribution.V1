using DecisionOS.Distribution.Domain.Uploads;

namespace DecisionOS.Distribution.Domain.Scoring;

public interface IPriorityRankingService
{
    Task<IReadOnlyList<IssuePriorityScore>> RankAndPersistAsync(
        Guid tenantId,
        DateOnly periodEnd,
        IReadOnlyList<KpiSnapshot> snapshots,
        CancellationToken ct = default);
}

public interface IDriverEvaluationService
{
    Task<int> EvaluateDriversAsync(
        Guid tenantId,
        DateOnly periodEnd,
        long uploadBatchId,
        IReadOnlyList<KpiSnapshot> snapshots,
        CancellationToken ct = default);
}

public interface IInfluencerEvidenceService
{
    Task<int> AttachEvidenceAsync(
        Guid tenantId,
        DateOnly periodEnd,
        CancellationToken ct = default);
}
