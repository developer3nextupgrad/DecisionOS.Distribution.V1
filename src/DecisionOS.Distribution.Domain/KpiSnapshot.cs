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
}
