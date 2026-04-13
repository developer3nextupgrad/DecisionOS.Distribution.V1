using DecisionOS.Distribution.Domain;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Infrastructure;

/// <summary>
/// Resolves the effective KPI/Driver standards for a tenant using:
/// profile overrides (preferred) -> global defaults -> controlled tenant overrides.
/// </summary>
public sealed class DefinitionResolver
{
    private readonly DecisionOsDbContext _db;
    public DefinitionResolver(DecisionOsDbContext db) => _db = db;

    public async Task<Dictionary<string, KpiDefinition>> ResolveKpiDefinitionsAsync(Tenant tenant, CancellationToken ct = default)
    {
        var profileId = tenant.BusinessProfileId;
        var defs = await _db.KpiDefinitions
            .Where(d => d.BusinessProfileId == null || d.BusinessProfileId == profileId)
            .ToListAsync(ct);

        if (profileId is not null && defs.All(d => d.BusinessProfileId is null))
            defs = await _db.KpiDefinitions.Where(d => d.BusinessProfileId == null).ToListAsync(ct);

        var resolved = defs
            .GroupBy(d => d.Code, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.BusinessProfileId == profileId).First())
            .ToDictionary(d => d.Code, d => d, StringComparer.OrdinalIgnoreCase);

        var overrides = await _db.TenantKpiOverrides
            .AsNoTracking()
            .Where(o => o.TenantId == tenant.Id && o.IsActive)
            .ToListAsync(ct);

        foreach (var o in overrides)
        {
            if (!resolved.TryGetValue(o.KpiCode, out var def)) continue;
            def.Target = o.Target ?? def.Target;
            def.AmberThreshold = o.AmberThreshold ?? def.AmberThreshold;
            def.RedThreshold = o.RedThreshold ?? def.RedThreshold;
            def.MinValue = o.MinValue ?? def.MinValue;
            def.MaxValue = o.MaxValue ?? def.MaxValue;
            def.AlertPriority = o.AlertPriority ?? def.AlertPriority;
            def.RecommendedAction = o.RecommendedAction ?? def.RecommendedAction;
            def.DiagnosticChecks = o.DiagnosticChecks ?? def.DiagnosticChecks;
        }

        return resolved;
    }

    public async Task<List<DriverDefinition>> ResolveDriverDefinitionsAsync(Tenant tenant, CancellationToken ct = default)
    {
        var profileId = tenant.BusinessProfileId;
        var defs = await _db.DriverDefinitions
            .Where(d => d.BusinessProfileId == null || d.BusinessProfileId == profileId)
            .ToListAsync(ct);

        if (profileId is not null && defs.All(d => d.BusinessProfileId is null))
            defs = await _db.DriverDefinitions.Where(d => d.BusinessProfileId == null).ToListAsync(ct);

        var resolved = defs
            .GroupBy(d =>
                ((d.PillarCode ?? string.Empty).Trim().ToUpperInvariant() + "||" +
                 (d.DriverCode ?? string.Empty).Trim().ToUpperInvariant()))
            .Select(g => g.OrderByDescending(x => x.BusinessProfileId == profileId).First())
            .ToList();

        var overrides = await _db.TenantDriverOverrides
            .AsNoTracking()
            .Where(o => o.TenantId == tenant.Id)
            .ToListAsync(ct);

        foreach (var o in overrides)
        {
            var key = ((o.PillarCode ?? string.Empty).Trim().ToUpperInvariant() + "||" +
                       (o.DriverCode ?? string.Empty).Trim().ToUpperInvariant());

            var def = resolved.FirstOrDefault(d =>
                ((d.PillarCode ?? string.Empty).Trim().ToUpperInvariant() + "||" +
                 (d.DriverCode ?? string.Empty).Trim().ToUpperInvariant()) == key);

            if (def is null) continue;
            def.DisplayName = o.DisplayName ?? def.DisplayName;
            def.Description = o.Description ?? def.Description;
            def.SortOrder = o.SortOrder ?? def.SortOrder;
            def.IsActive = o.IsActive ?? def.IsActive;
        }

        return resolved;
    }
}

