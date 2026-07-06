using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Uploads;
using Microsoft.EntityFrameworkCore;
using DecisionOS.Distribution.Infrastructure.Scoring;

namespace DecisionOS.Distribution.Infrastructure;

public sealed class WeeklyScoringService : IWeeklyScoringService
{
    private readonly DecisionOsDbContext _db;
    private readonly IKpiStatusService _kpiStatusService;
    private readonly IAlertService _alertService;
    private readonly IWeeklyFocusService _weeklyFocusService;

    public WeeklyScoringService(
        DecisionOsDbContext db,
        IKpiStatusService kpiStatusService,
        IAlertService alertService,
        IWeeklyFocusService weeklyFocusService)
    {
        _db = db;
        _kpiStatusService = kpiStatusService;
        _alertService = alertService;
        _weeklyFocusService = weeklyFocusService;
    }

    public async Task<WeeklyScoringResult> ScorePeriodAsync(WeeklyScoringRequest request, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == request.TenantId, ct);
        if (tenant is null) return new WeeklyScoringResult();

        var resolver = new DefinitionResolver(_db);
        var resolvedDefs = await resolver.ResolveKpiDefinitionsAsync(tenant, ct);
        var kpiDefs = resolvedDefs.Values.ToList();
        var defsByCode = resolvedDefs;

        var batchId = request.UploadBatchId;
        var period = request.PeriodEnd;

        async Task<decimal?> GrossMarginPct()
        {
            if (request.DirectKpiValues?.TryGetValue("GrossMargin%", out var direct) == true && direct is not null)
                return direct;
            var rows = await _db.NormalizedSalesRows.AsNoTracking()
                .Where(r => r.UploadBatchId == batchId && r.PeriodEnd == period).ToListAsync(ct);
            var net = rows.Where(r => r.NetSales is not null).Sum(r => r.NetSales!.Value);
            var cogs = rows.Where(r => r.Cogs is not null).Sum(r => r.Cogs!.Value);
            if (net <= 0 || cogs <= 0) return null;
            return (net - cogs) / net;
        }

        async Task<decimal?> ArPastDue31Pct()
        {
            if (request.DirectKpiValues?.TryGetValue("AR_PastDue31p%", out var direct) == true && direct is not null)
                return direct;
            var rows = await _db.NormalizedArRows.AsNoTracking()
                .Where(r => r.UploadBatchId == batchId && r.PeriodEnd == period).ToListAsync(ct);
            var total = rows.Where(r => r.OpenBalance is not null).Sum(r => r.OpenBalance!.Value);
            if (total <= 0) return null;
            var past = rows.Where(r => ScoringHelpers.IsPastDue31(r.DaysPastDue, r.AgingBucket) && r.OpenBalance is not null)
                .Sum(r => r.OpenBalance!.Value);
            return past / total;
        }

        async Task<decimal?> ApPastDue31Pct()
        {
            if (request.DirectKpiValues?.TryGetValue("AP_PastDue31p%", out var direct) == true && direct is not null)
                return direct;
            var rows = await _db.NormalizedApRows.AsNoTracking()
                .Where(r => r.UploadBatchId == batchId && r.PeriodEnd == period).ToListAsync(ct);
            var total = rows.Where(r => r.OpenBalance is not null).Sum(r => r.OpenBalance!.Value);
            if (total <= 0) return null;
            var past = rows.Where(r => ScoringHelpers.IsPastDue31(r.DaysPastDue, r.AgingBucket) && r.OpenBalance is not null)
                .Sum(r => r.OpenBalance!.Value);
            return past / total;
        }

        async Task<decimal?> DoH()
        {
            if (request.DirectKpiValues?.TryGetValue("DOH", out var direct) == true && direct is not null)
                return direct;
            var invRows = await _db.NormalizedInventoryRows.AsNoTracking()
                .Where(r => r.UploadBatchId == batchId && r.PeriodEnd == period).ToListAsync(ct);
            var inv = invRows.Where(r => r.InventoryValue is not null).Sum(r => r.InventoryValue!.Value);
            if (inv <= 0) return null;
            var salesRows = await _db.NormalizedSalesRows.AsNoTracking()
                .Where(r => r.UploadBatchId == batchId && r.PeriodEnd == period).ToListAsync(ct);
            var salesCogs = salesRows.Where(r => r.Cogs is not null).Sum(r => r.Cogs!.Value);
            if (salesCogs <= 0) return null;
            var perDay = salesCogs / 7m;
            if (perDay <= 0) return null;
            return inv / perDay;
        }

        async Task<decimal?> CCC()
        {
            if (request.DirectKpiValues?.TryGetValue("CCC", out var direct) == true && direct is not null)
                return direct;
            var salesRows = await _db.NormalizedSalesRows.AsNoTracking()
                .Where(r => r.UploadBatchId == batchId && r.PeriodEnd == period).ToListAsync(ct);
            var netSales = salesRows.Where(r => r.NetSales is not null).Sum(r => r.NetSales!.Value);
            var cogs = salesRows.Where(r => r.Cogs is not null).Sum(r => r.Cogs!.Value);
            if (netSales <= 0 || cogs <= 0) return null;
            var arRows = await _db.NormalizedArRows.AsNoTracking()
                .Where(r => r.UploadBatchId == batchId && r.PeriodEnd == period).ToListAsync(ct);
            var apRows = await _db.NormalizedApRows.AsNoTracking()
                .Where(r => r.UploadBatchId == batchId && r.PeriodEnd == period).ToListAsync(ct);
            var ar = arRows.Where(r => r.OpenBalance is not null).Sum(r => r.OpenBalance!.Value);
            var ap = apRows.Where(r => r.OpenBalance is not null).Sum(r => r.OpenBalance!.Value);
            var dso = ar / (netSales / 7m);
            var dio = await DoH();
            var dpo = ap / (cogs / 7m);
            if (dio is null) return null;
            return dso + dio.Value - dpo;
        }

        decimal? DirectOrNull(string code) =>
            request.DirectKpiValues?.TryGetValue(code, out var v) == true ? v : null;

        var computed = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase)
        {
            ["GrossMargin%"] = await GrossMarginPct(),
            ["AR_PastDue31p%"] = await ArPastDue31Pct(),
            ["AP_PastDue31p%"] = await ApPastDue31Pct(),
            ["DOH"] = await DoH(),
            ["CCC"] = await CCC(),
            ["NetProfit%"] = DirectOrNull("NetProfit%"),
            ["PerfectOrderRate"] = DirectOrNull("PerfectOrderRate")
        };

        _db.KpiSnapshots.RemoveRange(_db.KpiSnapshots.Where(s => s.TenantId == tenant.Id && s.PeriodEnd == period));

        var priorPeriod = period.AddDays(-7);
        var priorSnapshots = await _db.KpiSnapshots.AsNoTracking()
            .Where(s => s.TenantId == tenant.Id && s.PeriodEnd == priorPeriod)
            .ToDictionaryAsync(s => s.KpiDefinitionId, ct);

        foreach (var kvp in computed)
        {
            if (!defsByCode.TryGetValue(kvp.Key, out var def)) continue;
            if (kvp.Value is null)
            {
                _db.KpiSnapshots.Add(new KpiSnapshot
                {
                    TenantId = tenant.Id,
                    PeriodEnd = period,
                    KpiDefinitionId = def.Id,
                    Value = 0m,
                    Status = "GRAY",
                    DataConfidence = "Low",
                    CardDetailLine1 = KpiScoringNarrative.MissingDataHeadline(kvp.Key),
                    CardDetailLine2 = "Open the KPI card for the full missing-data checklist."
                });
            }
            else
            {
                var status = _kpiStatusService.ComputeStatus(def, kvp.Value.Value);
                decimal? wow = priorSnapshots.TryGetValue(def.Id, out var prior)
                    ? kvp.Value.Value - prior.Value
                    : null;
                var (line1, line2) = KpiScoringNarrative.ForScoredKpi(def, kvp.Value.Value, status);

                _db.KpiSnapshots.Add(new KpiSnapshot
                {
                    TenantId = tenant.Id,
                    PeriodEnd = period,
                    KpiDefinitionId = def.Id,
                    Value = kvp.Value.Value,
                    Status = status,
                    WeekOverWeekDelta = wow,
                    DataConfidence = request.DataConfidence,
                    CardDetailLine1 = line1,
                    CardDetailLine2 = line2
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

        return new WeeklyScoringResult { SnapshotsWritten = snapshots.Count };
    }
}
