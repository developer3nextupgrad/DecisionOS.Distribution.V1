using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Scoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DecisionOS.Distribution.Infrastructure.Scoring;

public sealed class InfluencerEvidenceService : IInfluencerEvidenceService
{
    private readonly DecisionOsDbContext _db;
    private readonly DecisionOsFeatureOptions _features;

    public InfluencerEvidenceService(DecisionOsDbContext db, IOptions<DecisionOsFeatureOptions> features)
    {
        _db = db;
        _features = features.Value;
    }

    public async Task<int> AttachEvidenceAsync(Guid tenantId, DateOnly periodEnd, CancellationToken ct = default)
    {
        if (!_features.Scoring.UseCatalogEngine || !_features.Catalog.Enabled)
            return 0;

        var drivers = await _db.DriverValues
            .Where(d => d.TenantId == tenantId && d.PeriodEnd == periodEnd && d.CatalogDriverId != null)
            .ToListAsync(ct);

        if (drivers.Count == 0) return 0;

        _db.InfluencerEvidences.RemoveRange(
            _db.InfluencerEvidences.Where(e => e.TenantId == tenantId && e.PeriodEnd == periodEnd));

        var driverIds = drivers.Select(d => d.CatalogDriverId!).ToHashSet();
        var maps = await _db.CatalogDriverInfluencerMaps.AsNoTracking()
            .Include(m => m.Influencer)
            .Where(m => driverIds.Contains(m.DriverId))
            .ToListAsync(ct);

        var written = 0;
        foreach (var driver in drivers)
        {
            var related = maps.Where(m => m.DriverId == driver.CatalogDriverId);
            foreach (var map in related)
            {
                _db.InfluencerEvidences.Add(new InfluencerEvidence
                {
                    TenantId = tenantId,
                    PeriodEnd = periodEnd,
                    DriverValueId = driver.Id,
                    InfluencerId = map.InfluencerId,
                    Severity = map.Influencer.DefaultSeverity,
                    EvidenceSummary = map.Influencer.Definition,
                    Confidence = "Medium",
                    Weight = (int)(map.DefaultWeight ?? 50m)
                });
                written++;
            }
        }

        if (written > 0) await _db.SaveChangesAsync(ct);
        return written;
    }
}
