namespace DecisionOS.Distribution.Domain;

public class KpiStatusService : IKpiStatusService
{
    public string ComputeStatus(KpiDefinition definition, decimal value)
    {
        return definition.Direction switch
        {
            KpiDirection.HigherIsBetter => ComputeHigherIsBetter(definition, value),
            KpiDirection.LowerIsBetter => ComputeLowerIsBetter(definition, value),
            _ => "UNKNOWN"
        };
    }

    private static string ComputeHigherIsBetter(KpiDefinition def, decimal value)
    {
        if (value >= def.Target) return "GREEN";
        if (value >= def.RedThreshold) return "YELLOW";
        return "RED";
    }

    private static string ComputeLowerIsBetter(KpiDefinition def, decimal value)
    {
        if (value <= def.Target) return "GREEN";
        if (value <= def.AmberThreshold) return "YELLOW";
        return "RED";
    }
}
