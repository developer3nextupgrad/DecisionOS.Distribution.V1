namespace DecisionOS.Distribution.Domain;

/// <summary>
/// Influencers are upstream factors that affect driver behavior (and therefore KPI outcomes).
/// They are defined as a catalog per business profile.
/// </summary>
public class InfluencerDefinition
{
    public int Id { get; set; }
    public Guid? BusinessProfileId { get; set; }
    public BusinessProfile? BusinessProfile { get; set; }

    /// <summary>Matches <see cref="KpiDefinition.Code"/>.</summary>
    public string PillarCode { get; set; } = null!;

    /// <summary>Matches <see cref="DriverDefinition.DriverCode"/> within the same profile.</summary>
    public string DriverCode { get; set; } = null!;

    /// <summary>Stable key used in imports/config (e.g. carrier_capacity).</summary>
    public string InfluencerCode { get; set; } = null!;

    public string DisplayName { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>Relative impact importance (0-100); higher means more influence.</summary>
    public int Weight { get; set; } = 50;

    public InfluencerImpactDirection Direction { get; set; } = InfluencerImpactDirection.Neutral;
    public bool IsActive { get; set; } = true;
}

