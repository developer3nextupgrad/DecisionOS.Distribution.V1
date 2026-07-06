using DecisionOS.Distribution.Domain.Scoring;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Infrastructure.Scoring.Calculators;

public abstract class KpiCalculatorBase : IKpiCalculator
{
    protected readonly DecisionOsDbContext Db;
    protected KpiCalculatorBase(DecisionOsDbContext db) => Db = db;
    public abstract string LegacyCode { get; }
    public abstract Task<KpiCalculationResult> CalculateAsync(KpiCalculationContext context, CancellationToken ct = default);

    protected decimal? DirectOrNull(KpiCalculationContext context) =>
        context.DirectKpiValues?.TryGetValue(context.KpiCode, out var v) == true ? v : null;
}

public sealed class GrossMarginKpiCalculator : KpiCalculatorBase
{
    public GrossMarginKpiCalculator(DecisionOsDbContext db) : base(db) { }
    public override string LegacyCode => "GrossMargin%";

    public override async Task<KpiCalculationResult> CalculateAsync(KpiCalculationContext context, CancellationToken ct = default)
    {
        var direct = DirectOrNull(context);
        if (direct is not null) return new KpiCalculationResult { Value = direct };

        var rows = await Db.NormalizedSalesRows.AsNoTracking()
            .Where(r => r.UploadBatchId == context.UploadBatchId && r.PeriodEnd == context.PeriodEnd)
            .ToListAsync(ct);
        var net = rows.Where(r => r.NetSales is not null).Sum(r => r.NetSales!.Value);
        var cogs = rows.Where(r => r.Cogs is not null).Sum(r => r.Cogs!.Value);
        if (net <= 0 || cogs <= 0) return new KpiCalculationResult();
        return new KpiCalculationResult { Value = (net - cogs) / net };
    }
}

public sealed class ArPastDueKpiCalculator : KpiCalculatorBase
{
    public ArPastDueKpiCalculator(DecisionOsDbContext db) : base(db) { }
    public override string LegacyCode => "AR_PastDue31p%";

    public override async Task<KpiCalculationResult> CalculateAsync(KpiCalculationContext context, CancellationToken ct = default)
    {
        var direct = DirectOrNull(context);
        if (direct is not null) return new KpiCalculationResult { Value = direct };

        var rows = await Db.NormalizedArRows.AsNoTracking()
            .Where(r => r.UploadBatchId == context.UploadBatchId && r.PeriodEnd == context.PeriodEnd)
            .ToListAsync(ct);
        var total = rows.Where(r => r.OpenBalance is not null).Sum(r => r.OpenBalance!.Value);
        if (total <= 0) return new KpiCalculationResult();
        var past = rows.Where(r => ScoringHelpers.IsPastDue31(r.DaysPastDue, r.AgingBucket) && r.OpenBalance is not null)
            .Sum(r => r.OpenBalance!.Value);
        return new KpiCalculationResult { Value = past / total };
    }
}

public sealed class ApPastDueKpiCalculator : KpiCalculatorBase
{
    public ApPastDueKpiCalculator(DecisionOsDbContext db) : base(db) { }
    public override string LegacyCode => "AP_PastDue31p%";

    public override async Task<KpiCalculationResult> CalculateAsync(KpiCalculationContext context, CancellationToken ct = default)
    {
        var direct = DirectOrNull(context);
        if (direct is not null) return new KpiCalculationResult { Value = direct };

        var rows = await Db.NormalizedApRows.AsNoTracking()
            .Where(r => r.UploadBatchId == context.UploadBatchId && r.PeriodEnd == context.PeriodEnd)
            .ToListAsync(ct);
        var total = rows.Where(r => r.OpenBalance is not null).Sum(r => r.OpenBalance!.Value);
        if (total <= 0) return new KpiCalculationResult();
        var past = rows.Where(r => ScoringHelpers.IsPastDue31(r.DaysPastDue, r.AgingBucket) && r.OpenBalance is not null)
            .Sum(r => r.OpenBalance!.Value);
        return new KpiCalculationResult { Value = past / total };
    }
}

public sealed class DohKpiCalculator : KpiCalculatorBase
{
    public DohKpiCalculator(DecisionOsDbContext db) : base(db) { }
    public override string LegacyCode => "DOH";

    public override async Task<KpiCalculationResult> CalculateAsync(KpiCalculationContext context, CancellationToken ct = default)
    {
        var direct = DirectOrNull(context);
        if (direct is not null) return new KpiCalculationResult { Value = direct };

        var invRows = await Db.NormalizedInventoryRows.AsNoTracking()
            .Where(r => r.UploadBatchId == context.UploadBatchId && r.PeriodEnd == context.PeriodEnd)
            .ToListAsync(ct);
        var inv = invRows.Where(r => r.InventoryValue is not null).Sum(r => r.InventoryValue!.Value);
        if (inv <= 0) return new KpiCalculationResult();
        var salesRows = await Db.NormalizedSalesRows.AsNoTracking()
            .Where(r => r.UploadBatchId == context.UploadBatchId && r.PeriodEnd == context.PeriodEnd)
            .ToListAsync(ct);
        var salesCogs = salesRows.Where(r => r.Cogs is not null).Sum(r => r.Cogs!.Value);
        if (salesCogs <= 0) return new KpiCalculationResult();
        var perDay = salesCogs / 7m;
        if (perDay <= 0) return new KpiCalculationResult();
        return new KpiCalculationResult { Value = inv / perDay };
    }
}

public sealed class CccKpiCalculator : KpiCalculatorBase
{
    public CccKpiCalculator(DecisionOsDbContext db) : base(db) { }
    public override string LegacyCode => "CCC";

    public override async Task<KpiCalculationResult> CalculateAsync(KpiCalculationContext context, CancellationToken ct = default)
    {
        var direct = DirectOrNull(context);
        if (direct is not null) return new KpiCalculationResult { Value = direct };

        var salesRows = await Db.NormalizedSalesRows.AsNoTracking()
            .Where(r => r.UploadBatchId == context.UploadBatchId && r.PeriodEnd == context.PeriodEnd)
            .ToListAsync(ct);
        var netSales = salesRows.Where(r => r.NetSales is not null).Sum(r => r.NetSales!.Value);
        var cogs = salesRows.Where(r => r.Cogs is not null).Sum(r => r.Cogs!.Value);
        if (netSales <= 0 || cogs <= 0) return new KpiCalculationResult();

        var arRows = await Db.NormalizedArRows.AsNoTracking()
            .Where(r => r.UploadBatchId == context.UploadBatchId && r.PeriodEnd == context.PeriodEnd)
            .ToListAsync(ct);
        var apRows = await Db.NormalizedApRows.AsNoTracking()
            .Where(r => r.UploadBatchId == context.UploadBatchId && r.PeriodEnd == context.PeriodEnd)
            .ToListAsync(ct);
        var ar = arRows.Where(r => r.OpenBalance is not null).Sum(r => r.OpenBalance!.Value);
        var ap = apRows.Where(r => r.OpenBalance is not null).Sum(r => r.OpenBalance!.Value);
        var dso = ar / (netSales / 7m);

        var invRows = await Db.NormalizedInventoryRows.AsNoTracking()
            .Where(r => r.UploadBatchId == context.UploadBatchId && r.PeriodEnd == context.PeriodEnd)
            .ToListAsync(ct);
        var inv = invRows.Where(r => r.InventoryValue is not null).Sum(r => r.InventoryValue!.Value);
        if (inv <= 0) return new KpiCalculationResult();
        var salesCogs = cogs;
        var perDay = salesCogs / 7m;
        if (perDay <= 0) return new KpiCalculationResult();
        var dio = inv / perDay;

        var dpo = ap / (cogs / 7m);
        return new KpiCalculationResult { Value = dso + dio - dpo };
    }
}

public sealed class NetProfitKpiCalculator : KpiCalculatorBase
{
    public NetProfitKpiCalculator(DecisionOsDbContext db) : base(db) { }
    public override string LegacyCode => "NetProfit%";
    public override Task<KpiCalculationResult> CalculateAsync(KpiCalculationContext context, CancellationToken ct = default)
        => Task.FromResult(new KpiCalculationResult { Value = DirectOrNull(context) });
}

public sealed class PerfectOrderRateKpiCalculator : KpiCalculatorBase
{
    public PerfectOrderRateKpiCalculator(DecisionOsDbContext db) : base(db) { }
    public override string LegacyCode => "PerfectOrderRate";
    public override Task<KpiCalculationResult> CalculateAsync(KpiCalculationContext context, CancellationToken ct = default)
        => Task.FromResult(new KpiCalculationResult { Value = DirectOrNull(context) });
}
