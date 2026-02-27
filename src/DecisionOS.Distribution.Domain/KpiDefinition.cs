namespace DecisionOS.Distribution.Domain;

public class KpiDefinition
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Unit { get; set; } = null!;
    public KpiDirection Direction { get; set; }
    public decimal Target { get; set; }
    public decimal AmberThreshold { get; set; }
    public decimal RedThreshold { get; set; }
    public string RecommendedAction { get; set; } = null!;
    public string DiagnosticChecks { get; set; } = null!;
}
