namespace DecisionOS.Distribution.Domain;

public class DriverValue
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public DateOnly PeriodEnd { get; set; }
    public string PillarCode { get; set; } = null!;
    /// <summary>Optional; validated against <see cref="DriverDefinition"/> when catalog is present.</summary>
    public string? DriverCode { get; set; }
    public string DriverName { get; set; } = null!;
    public string? Dimension1 { get; set; }
    public string? Dimension2 { get; set; }
    public decimal Current { get; set; }
    public decimal? WeekOverWeekDelta { get; set; }
    public string? Context { get; set; }
    public int Rank { get; set; }
    public string Status { get; set; } = null!;
    public string WhyItMatters { get; set; } = null!;
    /// <summary>Holdover row: accountable owner (concept dashboard).</summary>
    public string? Owner { get; set; }
    /// <summary>Display fragments for Assigned | Target | Current column.</summary>
    public string? AssignedSummary { get; set; }
    public string? TargetSummary { get; set; }
    public string? CurrentSummary { get; set; }
    /// <summary>0–100 fix progress; null = derive visual from status only.</summary>
    public int? FixProgressPercent { get; set; }
}
