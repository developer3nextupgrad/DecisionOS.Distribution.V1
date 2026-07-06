namespace DecisionOS.Distribution.Domain.Scoring;

public class IssuePriorityScore
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public DateOnly PeriodEnd { get; set; }
    public int? KpiDefinitionId { get; set; }
    public KpiDefinition? KpiDefinition { get; set; }
    public string? CatalogKpiId { get; set; }
    public Catalog.CatalogKpi? CatalogKpi { get; set; }
    public decimal SeverityScore { get; set; }
    public decimal CashScore { get; set; }
    public decimal FinancialScore { get; set; }
    public decimal UrgencyScore { get; set; }
    public decimal ActionabilityScore { get; set; }
    public decimal ConfidenceScore { get; set; }
    public decimal FinalScore { get; set; }
    public int Rank { get; set; }
    public string Status { get; set; } = null!;
}
