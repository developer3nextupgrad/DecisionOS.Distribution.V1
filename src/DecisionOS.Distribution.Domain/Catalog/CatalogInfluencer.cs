namespace DecisionOS.Distribution.Domain.Catalog;

public class CatalogInfluencer
{
    public string InfluencerId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Definition { get; set; } = "";
    public string? Category { get; set; }
    public string? EvidenceFields { get; set; }
    public string? DefaultSeverity { get; set; }
    public string? RelatedKpis { get; set; }
    public string? PrimaryModules { get; set; }
}
