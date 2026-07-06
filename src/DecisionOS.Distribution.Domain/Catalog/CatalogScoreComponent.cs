namespace DecisionOS.Distribution.Domain.Catalog;

public class CatalogScoreComponent
{
    public string Component { get; set; } = null!;
    public string? ValueRange { get; set; }
    public decimal WeightPercent { get; set; }
    public string? RequirementLevel { get; set; }
    public string? ImplementationNotes { get; set; }
}
