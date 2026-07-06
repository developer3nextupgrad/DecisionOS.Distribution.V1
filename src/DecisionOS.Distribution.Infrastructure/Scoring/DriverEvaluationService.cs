using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Scoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DecisionOS.Distribution.Infrastructure.Scoring;

public sealed class DriverEvaluationService : IDriverEvaluationService
{
    private readonly DecisionOsDbContext _db;
    private readonly IDriverRankingService _driverRanking;
    private readonly DecisionOsFeatureOptions _features;

    public DriverEvaluationService(
        DecisionOsDbContext db,
        IDriverRankingService driverRanking,
        IOptions<DecisionOsFeatureOptions> features)
    {
        _db = db;
        _driverRanking = driverRanking;
        _features = features.Value;
    }

    public async Task<int> EvaluateDriversAsync(
        Guid tenantId,
        DateOnly periodEnd,
        long uploadBatchId,
        IReadOnlyList<KpiSnapshot> snapshots,
        CancellationToken ct = default)
    {
        if (!_features.Scoring.UseCatalogEngine || !_features.Catalog.Enabled)
            return 0;

        var triggeredKpis = snapshots
            .Where(s => s.Status is "RED" or "YELLOW" && s.KpiDefinition?.Code is not null)
            .Select(s => s.KpiDefinition!.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (triggeredKpis.Count == 0) return 0;

        var catalogKpis = await _db.CatalogKpis.AsNoTracking()
            .Where(k => k.LegacyCode != null && triggeredKpis.Contains(k.LegacyCode))
            .ToListAsync(ct);

        var catalogKpiIds = catalogKpis.Select(k => k.KpiId).ToHashSet();
        var maps = await _db.CatalogKpiDriverMaps.AsNoTracking()
            .Include(m => m.Driver)
            .Where(m => catalogKpiIds.Contains(m.KpiId))
            .ToListAsync(ct);

        var existingHoldovers = await _db.DriverValues
            .Where(d => d.TenantId == tenantId && d.PeriodEnd == periodEnd)
            .ToListAsync(ct);

        if (maps.Count == 0) return existingHoldovers.Count;

        var written = 0;
        foreach (var map in maps)
        {
            var legacyCode = catalogKpis.First(k => k.KpiId == map.KpiId).LegacyCode!;
            var snap = snapshots.FirstOrDefault(s =>
                s.KpiDefinition?.Code.Equals(legacyCode, StringComparison.OrdinalIgnoreCase) == true);
            if (snap is null) continue;

            if (existingHoldovers.Any(d => d.CatalogDriverId == map.DriverId)) continue;

            var evidence = map.Driver.EvidenceFields ?? "";
            var hasEvidence = await HasEvidenceAsync(evidence, uploadBatchId, periodEnd, legacyCode, ct);
            if (!hasEvidence) continue;

            _db.DriverValues.Add(new DriverValue
            {
                TenantId = tenantId,
                PeriodEnd = periodEnd,
                PillarCode = legacyCode,
                CatalogDriverId = map.DriverId,
                DriverCode = map.DriverId,
                DriverName = map.Driver.Name,
                Current = snap.Value,
                Status = snap.Status,
                Rank = 0,
                WhyItMatters = map.Driver.Definition,
                Context = map.RuleNotes
            });
            written++;
        }

        if (written > 0)
        {
            await _db.SaveChangesAsync(ct);
            var allDrivers = await _db.DriverValues
                .Where(d => d.TenantId == tenantId && d.PeriodEnd == periodEnd)
                .ToListAsync(ct);
            foreach (var pillar in allDrivers.Select(d => d.PillarCode).Distinct())
                _driverRanking.RankDriversForPillar(allDrivers, pillar);
            await _db.SaveChangesAsync(ct);
        }

        return existingHoldovers.Count + written;
    }

    private async Task<bool> HasEvidenceAsync(
        string evidenceFields,
        long uploadBatchId,
        DateOnly periodEnd,
        string pillarCode,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(evidenceFields)) return true;

        var fields = evidenceFields.Split(',', ';')
            .Select(f => f.Trim().ToLowerInvariant())
            .Where(f => f.Length > 0)
            .ToList();

        if (fields.Count == 0) return true;

        if (pillarCode.Equals("AR_PastDue31p%", StringComparison.OrdinalIgnoreCase))
        {
            var count = await _db.NormalizedArRows.AsNoTracking()
                .CountAsync(r => r.UploadBatchId == uploadBatchId && r.PeriodEnd == periodEnd, ct);
            return count > 0;
        }

        if (pillarCode.Equals("AP_PastDue31p%", StringComparison.OrdinalIgnoreCase))
        {
            var count = await _db.NormalizedApRows.AsNoTracking()
                .CountAsync(r => r.UploadBatchId == uploadBatchId && r.PeriodEnd == periodEnd, ct);
            return count > 0;
        }

        if (pillarCode.Equals("GrossMargin%", StringComparison.OrdinalIgnoreCase))
        {
            var count = await _db.NormalizedSalesRows.AsNoTracking()
                .CountAsync(r => r.UploadBatchId == uploadBatchId && r.PeriodEnd == periodEnd, ct);
            return count > 0;
        }

        if (pillarCode.Equals("DOH", StringComparison.OrdinalIgnoreCase))
        {
            var count = await _db.NormalizedInventoryRows.AsNoTracking()
                .CountAsync(r => r.UploadBatchId == uploadBatchId && r.PeriodEnd == periodEnd, ct);
            return count > 0;
        }

        return true;
    }
}
