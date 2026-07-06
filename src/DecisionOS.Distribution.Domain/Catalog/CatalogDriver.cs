namespace DecisionOS.Distribution.Domain.Catalog;

public class CatalogDriver
{
    public string DriverId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Definition { get; set; } = "";
    public string? Category { get; set; }
    public string? EvidenceFields { get; set; }
    public string? RelatedKpis { get; set; }
    public string? PrimaryModules { get; set; }
}
