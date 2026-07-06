namespace DecisionOS.Distribution.Domain.Catalog;

public class CatalogKpiDriverMap
{
    public string KpiId { get; set; } = null!;
    public CatalogKpi Kpi { get; set; } = null!;
    public string DriverId { get; set; } = null!;
    public CatalogDriver Driver { get; set; } = null!;
    public string? MapType { get; set; }
    public string? PrimaryModules { get; set; }
    public string? RuleNotes { get; set; }
}
