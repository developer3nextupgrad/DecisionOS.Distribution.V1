namespace DecisionOS.Distribution.Domain.Catalog;

public class CatalogOutputArea
{
    public string OutputAreaCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? RoutingNotes { get; set; }
}
