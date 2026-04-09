namespace DecisionOS.Distribution.Domain;

public class KpiDefinition
{
    public int Id { get; set; }
    public Guid? BusinessProfileId { get; set; }
    public BusinessProfile? BusinessProfile { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Unit { get; set; } = null!;
    public KpiDirection Direction { get; set; }
    public decimal Target { get; set; }
    public decimal AmberThreshold { get; set; }
    public decimal RedThreshold { get; set; }
    /// <summary>Optional inclusive bounds validated during import.</summary>
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    /// <summary>Lower value = higher business priority when severities tie (cash before profit, etc.).</summary>
    public int AlertPriority { get; set; } = 100;
    public string RecommendedAction { get; set; } = null!;
    public string DiagnosticChecks { get; set; } = null!;
}
