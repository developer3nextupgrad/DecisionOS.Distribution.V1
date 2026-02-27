namespace DecisionOS.Distribution.Domain;

public class WeeklyFocus
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public DateOnly PeriodEnd { get; set; }
    public int KpiDefinitionId { get; set; }
    public KpiDefinition KpiDefinition { get; set; } = null!;
    public string DecisionQuestion { get; set; } = null!;
    public string RecommendedAction { get; set; } = null!;
    public string WhyNow { get; set; } = null!;
    public string Owner { get; set; } = null!;
    public string Cadence { get; set; } = null!;
}
