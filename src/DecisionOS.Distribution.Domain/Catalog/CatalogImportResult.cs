namespace DecisionOS.Distribution.Domain.Catalog;

public sealed class CatalogImportResult
{
    public int KpisImported { get; set; }
    public int DriversImported { get; set; }
    public int InfluencersImported { get; set; }
    public int KpiDriverMapsImported { get; set; }
    public int DriverInfluencerMapsImported { get; set; }
    public int ScoreComponentsImported { get; set; }
    public int ModulesImported { get; set; }
    public int OutputAreasImported { get; set; }
    public List<string> Errors { get; } = new();
    public bool Success => Errors.Count == 0;
}
