namespace DecisionOS.Distribution.Domain.Catalog;

public class CatalogModule
{
    public string ModuleCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? PrimaryKpis { get; set; }
    public string? DefaultOutput { get; set; }
    public string? Description { get; set; }
}
