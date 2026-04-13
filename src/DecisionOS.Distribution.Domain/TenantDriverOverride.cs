namespace DecisionOS.Distribution.Domain;

/// <summary>
/// Controlled tenant-specific override to a driver definition entry (catalog adjustments only).
/// Keyed by Tenant + PillarCode + DriverCode.
/// </summary>
public class TenantDriverOverride
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string PillarCode { get; set; } = null!;
    public string DriverCode { get; set; } = null!;

    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}

