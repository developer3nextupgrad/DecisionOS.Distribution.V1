using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Catalog;
using DecisionOS.Distribution.Domain.Scoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DecisionOS.Distribution.Infrastructure.Scoring;

public sealed class PriorityRankingService : IPriorityRankingService
{
    private readonly DecisionOsDbContext _db;
    private readonly IKpiStatusService _kpiStatusService;
    private readonly DecisionOsFeatureOptions _features;

    private static readonly Dictionary<string, decimal> DefaultWeights = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Severity"] = 30m,
        ["Cash"] = 20m,
        ["Financial"] = 20m,
        ["Urgency"] = 15m,
        ["Actionability"] = 10m,
        ["Confidence"] = 5m
    };

    public PriorityRankingService(
        DecisionOsDbContext db,
        IKpiStatusService kpiStatusService,
        IOptions<DecisionOsFeatureOptions> features)
    {
        _db = db;
        _kpiStatusService = kpiStatusService;
        _features = features.Value;
    }

    public async Task<IReadOnlyList<IssuePriorityScore>> RankAndPersistAsync(
        Guid tenantId,
        DateOnly periodEnd,
        IReadOnlyList<KpiSnapshot> snapshots,
        CancellationToken ct = default)
    {
        _db.IssuePriorityScores.RemoveRange(
            _db.IssuePriorityScores.Where(s => s.TenantId == tenantId && s.PeriodEnd == periodEnd));

        if (!_features.Scoring.UseDynamicTop7 && !_features.Scoring.UseCatalogEngine)
            return Array.Empty<IssuePriorityScore>();

        var weights = await LoadWeightsAsync(ct);
        var selections = await _db.TenantKpiSelections.AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .ToDictionaryAsync(s => s.CatalogKpiId, ct);

        var catalogByCode = BuildCatalogLookup(await _db.CatalogKpis.AsNoTracking().ToListAsync(ct));

        var excludedIds = selections.Values
            .Where(s => s.IsExcluded)
            .Select(s => s.CatalogKpiId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidates = snapshots.Where(s =>
        {
            if (s.KpiDefinition?.Code is null) return false;
            if (TryResolveCatalogKpi(catalogByCode, s.KpiDefinition.Code, out var ck) && excludedIds.Contains(ck.KpiId))
                return false;
            return s.Status is "RED" or "YELLOW"
                || (s.Status == "GREEN" && TryResolveCatalogKpi(catalogByCode, s.KpiDefinition.Code, out var pin)
                    && selections.TryGetValue(pin.KpiId, out var sel) && sel.IsPinned);
        }).ToList();

        var scores = new List<IssuePriorityScore>();
        foreach (var snap in candidates)
        {
            var def = snap.KpiDefinition!;
            TryResolveCatalogKpi(catalogByCode, def.Code, out var catalogKpi);

            var severity = ScoreSeverity(snap, def);
            var cash = ScoreCash(catalogKpi?.Category, def.Code);
            var financial = ScoreFinancial(catalogKpi?.Category, def.Code);
            var urgency = ScoreUrgency(snap);
            var actionability = string.IsNullOrWhiteSpace(def.RecommendedAction) || snap.Status == "GRAY" ? 0m : 100m;
            var confidence = ScoreConfidence(snap.DataConfidence);

            var final = (severity * weights["Severity"]
                + cash * weights["Cash"]
                + financial * weights["Financial"]
                + urgency * weights["Urgency"]
                + actionability * weights["Actionability"]
                + confidence * weights["Confidence"]) / 100m;

            if (confidence < 40m) final = Math.Min(final, 40m);

            scores.Add(new IssuePriorityScore
            {
                TenantId = tenantId,
                PeriodEnd = periodEnd,
                KpiDefinitionId = def.Id,
                CatalogKpiId = catalogKpi?.KpiId,
                SeverityScore = severity,
                CashScore = cash,
                FinancialScore = financial,
                UrgencyScore = urgency,
                ActionabilityScore = actionability,
                ConfidenceScore = confidence,
                FinalScore = final,
                Status = snap.Status
            });
        }

        var ranked = scores.OrderByDescending(s => s.FinalScore).ToList();
        for (var i = 0; i < ranked.Count; i++)
            ranked[i].Rank = i + 1;

        _db.IssuePriorityScores.AddRange(ranked);
        await _db.SaveChangesAsync(ct);
        return ranked;
    }

    private async Task<Dictionary<string, decimal>> LoadWeightsAsync(CancellationToken ct)
    {
        var components = await _db.CatalogScoreComponents.AsNoTracking().ToListAsync(ct);
        if (components.Count == 0) return DefaultWeights;

        var map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in components)
            map[c.Component] = c.WeightPercent;
        foreach (var d in DefaultWeights)
            if (!map.ContainsKey(d.Key)) map[d.Key] = d.Value;
        return map;
    }

    private static decimal ScoreSeverity(KpiSnapshot snap, KpiDefinition def)
    {
        if (snap.Status == "GRAY") return 0m;
        if (snap.Status == "GREEN") return 20m;
        if (snap.Status == "YELLOW") return 60m;
        var gap = def.Direction == KpiDirection.HigherIsBetter
            ? Math.Max(0m, def.RedThreshold - snap.Value)
            : Math.Max(0m, snap.Value - def.RedThreshold);
        return Math.Min(100m, 80m + gap * 10m);
    }

    private static decimal ScoreCash(string? category, string code)
    {
        if (code is "AR_PastDue31p%" or "AP_PastDue31p%" or "CCC" or "DOH") return 90m;
        if (category?.Contains("cash", StringComparison.OrdinalIgnoreCase) == true) return 85m;
        return 40m;
    }

    private static decimal ScoreFinancial(string? category, string code)
    {
        if (code is "GrossMargin%" or "NetProfit%") return 90m;
        if (category?.Contains("profit", StringComparison.OrdinalIgnoreCase) == true) return 80m;
        return 50m;
    }

    private static decimal ScoreUrgency(KpiSnapshot snap)
    {
        if (snap.WeekOverWeekDelta is null) return 50m;
        var delta = snap.WeekOverWeekDelta.Value;
        if (snap.Status == "RED" && delta > 0) return 90m;
        if (snap.Status == "RED" && delta < 0) return 70m;
        if (snap.Status == "YELLOW") return 60m;
        return 40m;
    }

    private static Dictionary<string, CatalogKpi> BuildCatalogLookup(IReadOnlyList<CatalogKpi> catalogKpis)
    {
        var map = new Dictionary<string, CatalogKpi>(StringComparer.OrdinalIgnoreCase);
        foreach (var ck in catalogKpis)
        {
            map[ck.KpiId] = ck;
            if (!string.IsNullOrWhiteSpace(ck.LegacyCode))
                map[ck.LegacyCode] = ck;
        }
        return map;
    }

    private static bool TryResolveCatalogKpi(
        IReadOnlyDictionary<string, CatalogKpi> catalogByCode,
        string definitionCode,
        out CatalogKpi catalogKpi)
    {
        if (catalogByCode.TryGetValue(definitionCode, out catalogKpi!))
            return true;
        catalogKpi = null!;
        return false;
    }

    private static decimal ScoreConfidence(string? dataConfidence) => dataConfidence?.ToLowerInvariant() switch
    {
        "high" => 90m,
        "medium" => 60m,
        "low" => 30m,
        _ => 50m
    };
}
