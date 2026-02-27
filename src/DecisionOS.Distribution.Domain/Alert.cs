namespace DecisionOS.Distribution.Domain;

public class Alert
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public DateOnly PeriodEnd { get; set; }
    public int KpiDefinitionId { get; set; }
    public KpiDefinition KpiDefinition { get; set; } = null!;
    public string Severity { get; set; } = null!;
    public string ReasonSummary { get; set; } = null!;
}
