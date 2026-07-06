using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Catalog;
using DecisionOS.Distribution.Domain.Scoring;
using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Domain.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DecisionOS.Distribution.Infrastructure.Scoring;

public sealed class KpiCalculationOrchestrator : IKpiCalculationOrchestrator, IWeeklyScoringService
{
    private readonly DecisionOsDbContext _db;
    private readonly IKpiStatusService _kpiStatusService;
    private readonly IAlertService _alertService;
    private readonly IWeeklyFocusService _weeklyFocusService;
    private readonly WeeklyScoringService _legacyScoring;
    private readonly IEnumerable<IKpiCalculator> _calculators;
    private readonly IPriorityRankingService _priorityRanking;
    private readonly IDriverEvaluationService _driverEvaluation;
    private readonly IInfluencerEvidenceService _influencerEvidence;
    private readonly IModuleRoutingService _moduleRouting;
    private readonly ICatalogKpiDefinitionSyncService _catalogSync;
    private readonly DecisionOsFeatureOptions _features;

    public KpiCalculationOrchestrator(
        DecisionOsDbContext db,
        IKpiStatusService kpiStatusService,
        IAlertService alertService,
        IWeeklyFocusService weeklyFocusService,
        WeeklyScoringService legacyScoring,
        IEnumerable<IKpiCalculator> calculators,
        IPriorityRankingService priorityRanking,
        IDriverEvaluationService driverEvaluation,
        IInfluencerEvidenceService influencerEvidence,
        IModuleRoutingService moduleRouting,
        ICatalogKpiDefinitionSyncService catalogSync,
        IOptions<DecisionOsFeatureOptions> features)
    {
        _db = db;
        _kpiStatusService = kpiStatusService;
        _alertService = alertService;
        _weeklyFocusService = weeklyFocusService;
        _legacyScoring = legacyScoring;
        _calculators = calculators;
        _priorityRanking = priorityRanking;
        _driverEvaluation = driverEvaluation;
        _influencerEvidence = influencerEvidence;
        _moduleRouting = moduleRouting;
        _catalogSync = catalogSync;
        _features = features.Value;
    }

    public async Task<WeeklyScoringResult> ScorePeriodAsync(WeeklyScoringRequest request, CancellationToken ct = default)
    {
        if (!_features.Scoring.UseCatalogEngine)
            return await _legacyScoring.ScorePeriodAsync(request, ct);

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == request.TenantId, ct);
        if (tenant is null) return new WeeklyScoringResult();

        var catalogCount = await _db.CatalogKpis.CountAsync(ct);
        if (catalogCount == 0)
            return await _legacyScoring.ScorePeriodAsync(request, ct);

        await _catalogSync.SyncGlobalDefinitionsAsync(ct);

        var resolver = new DefinitionResolver(_db);
        var resolvedDefs = await resolver.ResolveKpiDefinitionsAsync(tenant, ct);
        var kpiDefs = resolvedDefs.Values.ToList();
        var calcByCode = _calculators.ToDictionary(c => c.LegacyCode, StringComparer.OrdinalIgnoreCase);
        var catalogKpis = await _db.CatalogKpis.OrderBy(k => k.KpiId).ToListAsync(ct);

        var period = request.PeriodEnd;
        _db.KpiSnapshots.RemoveRange(_db.KpiSnapshots.Where(s => s.TenantId == tenant.Id && s.PeriodEnd == period));

        var priorPeriod = period.AddDays(-7);
        var priorSnapshots = await _db.KpiSnapshots.AsNoTracking()
            .Where(s => s.TenantId == tenant.Id && s.PeriodEnd == priorPeriod)
            .ToDictionaryAsync(s => s.KpiDefinitionId, ct);

        foreach (var ck in catalogKpis)
        {
            var code = ck.LegacyCode ?? ck.KpiId;
            if (!resolvedDefs.TryGetValue(code, out var def))
                continue;

            if (ck.LegacyCode is not null && calcByCode.TryGetValue(ck.LegacyCode, out var calculator))
            {
                await WriteCalculatedSnapshotAsync(tenant.Id, period, request, def, ck.LegacyCode, calculator, priorSnapshots, ct);
            }
            else
            {
                _db.KpiSnapshots.Add(new KpiSnapshot
                {
                    TenantId = tenant.Id,
                    PeriodEnd = period,
                    KpiDefinitionId = def.Id,
                    Value = 0m,
                    Status = "GRAY",
                    DataConfidence = "Low",
                    CardDetailLine1 = $"Not enough data to score {ck.Name} yet.",
                    CardDetailLine2 = string.IsNullOrWhiteSpace(ck.PrimaryDataNeeds)
                        ? "Add the required upload fields from the KPI catalog, then re-import."
                        : ck.PrimaryDataNeeds
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        var snapshots = await _db.KpiSnapshots.Include(s => s.KpiDefinition)
            .Where(s => s.TenantId == tenant.Id && s.PeriodEnd == period)
            .ToListAsync(ct);

        var topAlert = _alertService.SelectTopAlert(tenant.Id, period, snapshots, kpiDefs);
        _db.Alerts.RemoveRange(_db.Alerts.Where(a => a.TenantId == tenant.Id && a.PeriodEnd == period));
        if (topAlert is not null) _db.Alerts.Add(topAlert);

        _db.WeeklyFocuses.RemoveRange(_db.WeeklyFocuses.Where(f => f.TenantId == tenant.Id && f.PeriodEnd == period));
        var focus = _weeklyFocusService.GenerateWeeklyFocus(tenant.Id, period, topAlert, kpiDefs);
        if (focus is not null) _db.WeeklyFocuses.Add(focus);

        await _db.SaveChangesAsync(ct);

        var priorityScores = await _priorityRanking.RankAndPersistAsync(tenant.Id, period, snapshots, ct);
        var driversWritten = await _driverEvaluation.EvaluateDriversAsync(tenant.Id, period, request.UploadBatchId, snapshots, ct);
        await _influencerEvidence.AttachEvidenceAsync(tenant.Id, period, ct);

        if (_features.Routing.Enabled)
            await _moduleRouting.RouteIssuesAsync(tenant.Id, period, priorityScores, snapshots, ct);

        return new WeeklyScoringResult { SnapshotsWritten = snapshots.Count, DriverRowsProcessed = driversWritten };
    }

    private async Task WriteCalculatedSnapshotAsync(
        Guid tenantId,
        DateOnly period,
        WeeklyScoringRequest request,
        KpiDefinition def,
        string code,
        IKpiCalculator calculator,
        Dictionary<int, KpiSnapshot> priorSnapshots,
        CancellationToken ct)
    {
        var ctx = new KpiCalculationContext
        {
            TenantId = tenantId,
            PeriodEnd = period,
            UploadBatchId = request.UploadBatchId,
            DataConfidence = request.DataConfidence,
            DirectKpiValues = request.DirectKpiValues,
            Definition = def,
            KpiCode = code
        };

        var result = await calculator.CalculateAsync(ctx, ct);
        if (result.IsMissingData)
        {
            _db.KpiSnapshots.Add(new KpiSnapshot
            {
                TenantId = tenantId,
                PeriodEnd = period,
                KpiDefinitionId = def.Id,
                Value = 0m,
                Status = "GRAY",
                DataConfidence = "Low",
                CardDetailLine1 = KpiScoringNarrative.MissingDataHeadline(code),
                CardDetailLine2 = "Open the KPI card for the full missing-data checklist."
            });
            return;
        }

        var status = _kpiStatusService.ComputeStatus(def, result.Value!.Value);
        decimal? wow = priorSnapshots.TryGetValue(def.Id, out var prior)
            ? result.Value.Value - prior.Value
            : null;
        var (line1, line2) = KpiScoringNarrative.ForScoredKpi(def, result.Value.Value, status);

        _db.KpiSnapshots.Add(new KpiSnapshot
        {
            TenantId = tenantId,
            PeriodEnd = period,
            KpiDefinitionId = def.Id,
            Value = result.Value.Value,
            Status = status,
            WeekOverWeekDelta = wow,
            DataConfidence = request.DataConfidence,
            CardDetailLine1 = line1,
            CardDetailLine2 = line2
        });
    }
}
