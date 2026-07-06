namespace DecisionOS.Distribution.Domain.Scoring;

public class TenantKpiSelection
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string CatalogKpiId { get; set; } = null!;
    public Catalog.CatalogKpi CatalogKpi { get; set; } = null!;
    public bool IsPinned { get; set; }
    public bool IsExcluded { get; set; }
}
