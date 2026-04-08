namespace DecisionOS.Distribution.Domain;

public class KpiSnapshot
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public DateOnly PeriodEnd { get; set; }
    public int KpiDefinitionId { get; set; }
    public KpiDefinition KpiDefinition { get; set; } = null!;
    public decimal Value { get; set; }
    public string Status { get; set; } = null!;
    public decimal? WeekOverWeekDelta { get; set; }
    /// <summary>Optional narrative lines shown on executive KPI tiles (from CSV).</summary>
    public string? CardDetailLine1 { get; set; }
    public string? CardDetailLine2 { get; set; }
}
