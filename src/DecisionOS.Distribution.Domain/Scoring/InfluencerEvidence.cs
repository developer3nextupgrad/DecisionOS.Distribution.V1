namespace DecisionOS.Distribution.Domain.Scoring;

public class InfluencerEvidence
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public DateOnly PeriodEnd { get; set; }
    public int DriverValueId { get; set; }
    public DriverValue DriverValue { get; set; } = null!;
    public string InfluencerId { get; set; } = null!;
    public Catalog.CatalogInfluencer Influencer { get; set; } = null!;
    public string? Severity { get; set; }
    public string EvidenceSummary { get; set; } = "";
    public string? Confidence { get; set; }
    public int Weight { get; set; }
}
