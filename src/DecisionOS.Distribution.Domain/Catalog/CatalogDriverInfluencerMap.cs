namespace DecisionOS.Distribution.Domain.Catalog;

public class CatalogDriverInfluencerMap
{
    public string DriverId { get; set; } = null!;
    public CatalogDriver Driver { get; set; } = null!;
    public string InfluencerId { get; set; } = null!;
    public CatalogInfluencer Influencer { get; set; } = null!;
    public string? RelationshipType { get; set; }
    public decimal? DefaultWeight { get; set; }
    public string? RuleNotes { get; set; }
}
