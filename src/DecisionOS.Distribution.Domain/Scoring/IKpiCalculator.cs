namespace DecisionOS.Distribution.Domain.Scoring;

public sealed class KpiCalculationContext
{
    public Guid TenantId { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public long UploadBatchId { get; init; }
    public string DataConfidence { get; init; } = "High";
    public IReadOnlyDictionary<string, decimal?>? DirectKpiValues { get; init; }
    public KpiDefinition Definition { get; init; } = null!;
    public string KpiCode { get; init; } = null!;
}

public sealed class KpiCalculationResult
{
    public decimal? Value { get; init; }
    public bool IsMissingData => Value is null;
}

public interface IKpiCalculator
{
    string LegacyCode { get; }
    Task<KpiCalculationResult> CalculateAsync(KpiCalculationContext context, CancellationToken ct = default);
}
