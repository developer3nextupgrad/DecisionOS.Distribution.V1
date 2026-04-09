namespace DecisionOS.Distribution.Domain;

/// <summary>
/// Catalog of allowed drivers per KPI pillar; CSV rows reference <see cref="DriverCode"/>.
/// </summary>
public class DriverDefinition
{
    public int Id { get; set; }
    public Guid? BusinessProfileId { get; set; }
    public BusinessProfile? BusinessProfile { get; set; }
    /// <summary>Matches <see cref="KpiDefinition.Code"/>.</summary>
    public string PillarCode { get; set; } = null!;
    /// <summary>Stable key used in imports (e.g. acme_collections).</summary>
    public string DriverCode { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
