namespace DecisionOS.Distribution.Domain;

/// <summary>
/// Controlled tenant-specific override to a KPI definition (avoid hard-coded tenant logic).
/// Keyed by Tenant + KPI Code.
/// </summary>
public class TenantKpiOverride
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string KpiCode { get; set; } = null!;

    public decimal? Target { get; set; }
    public decimal? AmberThreshold { get; set; }
    public decimal? RedThreshold { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }

    public int? AlertPriority { get; set; }

    public string? RecommendedAction { get; set; }
    public string? DiagnosticChecks { get; set; }

    public bool IsActive { get; set; } = true;
}

