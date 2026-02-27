namespace DecisionOS.Distribution.Domain;

public class DriverValue
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public DateOnly PeriodEnd { get; set; }
    public string PillarCode { get; set; } = null!;
    public string DriverName { get; set; } = null!;
    public string? Dimension1 { get; set; }
    public string? Dimension2 { get; set; }
    public decimal Current { get; set; }
    public decimal? WeekOverWeekDelta { get; set; }
    public string? Context { get; set; }
    public int Rank { get; set; }
    public string Status { get; set; } = null!;
    public string WhyItMatters { get; set; } = null!;
}
