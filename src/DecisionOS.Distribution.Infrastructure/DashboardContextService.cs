using DecisionOS.Distribution.Domain.Normalized;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Infrastructure;

public sealed record DashboardCustomerOption(string CustomerId, string DisplayName);

public sealed class DashboardContextService
{
    /// <summary>Tenant-wide view (no buyer filter).</summary>
    public const string AllCustomersId = "";

    private readonly DecisionOsDbContext _db;

    public DashboardContextService(DecisionOsDbContext db) => _db = db;

    public async Task<IReadOnlyList<DashboardCustomerOption>> GetCustomersAsync(Guid tenantId, CancellationToken ct = default)
    {
        var sales = await _db.NormalizedSalesRows.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.CustomerId != null && r.CustomerId != "")
            .Select(r => new { r.CustomerId, r.CustomerName })
            .ToListAsync(ct);

        var ar = await _db.NormalizedArRows.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.CustomerId != null && r.CustomerId != "")
            .Select(r => new { r.CustomerId, r.CustomerName })
            .ToListAsync(ct);

        return sales.Concat(ar)
            .GroupBy(x => x.CustomerId!, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var name = g.Select(x => x.CustomerName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))?.Trim();
                var label = string.IsNullOrWhiteSpace(name) ? g.Key : $"{name} ({g.Key})";
                return new DashboardCustomerOption(g.Key, label);
            })
            .OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<DateOnly>> GetWeeksAsync(
        Guid tenantId,
        string? customerId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return await _db.KpiSnapshots.AsNoTracking()
                .Where(s => s.TenantId == tenantId)
                .Select(s => s.PeriodEnd)
                .Distinct()
                .OrderByDescending(p => p)
                .ToListAsync(ct);
        }

        var cid = customerId.Trim();
        var fromSales = _db.NormalizedSalesRows.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.CustomerId == cid)
            .Select(r => r.PeriodEnd);

        var fromAr = _db.NormalizedArRows.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.CustomerId == cid)
            .Select(r => r.PeriodEnd);

        var weeks = await fromSales.Union(fromAr)
            .Distinct()
            .OrderByDescending(p => p)
            .ToListAsync(ct);

        if (weeks.Count > 0)
            return weeks;

        return await _db.KpiSnapshots.AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .Select(s => s.PeriodEnd)
            .Distinct()
            .OrderByDescending(p => p)
            .ToListAsync(ct);
    }

    public async Task<string?> ResolveCustomerDisplayNameAsync(
        Guid tenantId,
        string? customerId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(customerId)) return null;

        var cid = customerId.Trim();
        var name = await _db.NormalizedSalesRows.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.CustomerId == cid && r.CustomerName != null && r.CustomerName != "")
            .Select(r => r.CustomerName)
            .FirstOrDefaultAsync(ct);

        if (!string.IsNullOrWhiteSpace(name)) return name.Trim();

        name = await _db.NormalizedArRows.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.CustomerId == cid && r.CustomerName != null && r.CustomerName != "")
            .Select(r => r.CustomerName)
            .FirstOrDefaultAsync(ct);

        return string.IsNullOrWhiteSpace(name) ? cid : name.Trim();
    }
}
