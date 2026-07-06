using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Infrastructure.Catalog;

public sealed class CatalogKpiDefinitionSyncService : ICatalogKpiDefinitionSyncService
{
    private readonly DecisionOsDbContext _db;

    public CatalogKpiDefinitionSyncService(DecisionOsDbContext db) => _db = db;

    public async Task<int> SyncGlobalDefinitionsAsync(CancellationToken ct = default)
    {
        var catalogKpis = await _db.CatalogKpis.OrderBy(k => k.KpiId).ToListAsync(ct);
        if (catalogKpis.Count == 0) return 0;

        var globals = await _db.KpiDefinitions
            .Where(k => k.BusinessProfileId == null)
            .ToListAsync(ct);

        var byCode = globals.ToDictionary(k => k.Code, StringComparer.OrdinalIgnoreCase);
        var byLegacy = globals
            .Where(k => catalogKpis.Any(c => string.Equals(c.LegacyCode, k.Code, StringComparison.OrdinalIgnoreCase)))
            .ToDictionary(k => k.Code, StringComparer.OrdinalIgnoreCase);

        var written = 0;
        foreach (var ck in catalogKpis)
        {
            var code = ResolveDefinitionCode(ck);
            if (byCode.ContainsKey(code))
            {
                UpdateNameIfNeeded(byCode[code], ck);
                continue;
            }

            if (ck.LegacyCode is not null && byLegacy.TryGetValue(ck.LegacyCode, out var legacy))
            {
                UpdateNameIfNeeded(legacy, ck);
                byCode[code] = legacy;
                continue;
            }

            var def = CreateDefinition(ck, code);
            _db.KpiDefinitions.Add(def);
            byCode[code] = def;
            written++;
        }

        if (written > 0)
            await _db.SaveChangesAsync(ct);

        return written;
    }

    private static string ResolveDefinitionCode(CatalogKpi ck) =>
        ck.LegacyCode ?? ck.KpiId;

    private static void UpdateNameIfNeeded(KpiDefinition def, CatalogKpi ck)
    {
        if (!string.Equals(def.Name, ck.Name, StringComparison.Ordinal))
            def.Name = ck.Name;
    }

    private static KpiDefinition CreateDefinition(CatalogKpi ck, string code)
    {
        var (unit, direction, target, amber, red, priority) = InferThresholds(ck);
        return new KpiDefinition
        {
            Code = code,
            Name = ck.Name,
            Unit = unit,
            Direction = direction,
            Target = target,
            AmberThreshold = amber,
            RedThreshold = red,
            AlertPriority = priority,
            RecommendedAction = string.IsNullOrWhiteSpace(ck.Definition)
                ? $"Review {ck.Name} and take corrective action this week."
                : ck.Definition,
            DiagnosticChecks = string.IsNullOrWhiteSpace(ck.PrimaryDataNeeds)
                ? "Upload the data described in the KPI catalog for this measure."
                : ck.PrimaryDataNeeds
        };
    }

    private static (string Unit, KpiDirection Direction, decimal Target, decimal Amber, decimal Red, int Priority)
        InferThresholds(CatalogKpi ck)
    {
        if (ck.LegacyCode is not null)
            return ck.LegacyCode switch
            {
                "GrossMargin%" or "NetProfit%" or "PerfectOrderRate" or "AR_PastDue31p%" or "AP_PastDue31p%" =>
                    ("pct", ck.LegacyCode is "GrossMargin%" or "NetProfit%" or "PerfectOrderRate"
                        ? KpiDirection.HigherIsBetter
                        : KpiDirection.LowerIsBetter, 0.25m, 0.20m, 0.15m, 80),
                "DOH" or "CCC" => ("days", KpiDirection.LowerIsBetter, 45m, 55m, 70m, 80),
                _ => ("index", KpiDirection.LowerIsBetter, 1m, 1.2m, 1.5m, 100)
            };

        var name = ck.Name.ToLowerInvariant();
        if (name.Contains('%') || name.Contains("margin") || name.Contains("profit") || name.Contains("rate"))
            return ("pct", KpiDirection.HigherIsBetter, 0.25m, 0.20m, 0.15m, 100);
        if (name.Contains("day") || name.Contains("cycle") || name.Contains("doh"))
            return ("days", KpiDirection.LowerIsBetter, 45m, 55m, 70m, 100);

        return ("index", KpiDirection.LowerIsBetter, 1m, 1.2m, 1.5m, 100);
    }
}
