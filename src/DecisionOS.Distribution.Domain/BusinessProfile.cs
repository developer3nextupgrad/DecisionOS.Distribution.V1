namespace DecisionOS.Distribution.Domain;

/// <summary>
/// Industry / business-type profile that scopes KPI and driver standards.
/// Tenants can point to a profile to use that KPI/driver set.
/// </summary>
public class BusinessProfile
{
    public Guid Id { get; set; }
    public Guid? VerticalLibraryId { get; set; }
    public VerticalLibrary? VerticalLibrary { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Framework config fields (V1 manual selection, V2 can automate)
    public string? ActiveKpiProfileCode { get; set; }
    public string? LocationStructure { get; set; }
    public string? ChannelStructure { get; set; }
    public string? ThresholdProfileCode { get; set; }
}

