namespace DecisionOS.Distribution.Domain.Catalog;

public class CatalogKpi
{
    public string KpiId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Definition { get; set; } = "";
    public string? Category { get; set; }
    public string? EntityScope { get; set; }
    public string? Cadence { get; set; }
    public string? PrimaryDataNeeds { get; set; }
    public string? DefaultStatusModel { get; set; }
    public bool MgmtLayerCandidate { get; set; }
    public string? DeveloperNotes { get; set; }
    public string? PrimaryModules { get; set; }
    /// <summary>Maps to existing <see cref="KpiDefinition.Code"/> when applicable.</summary>
    public string? LegacyCode { get; set; }
}
