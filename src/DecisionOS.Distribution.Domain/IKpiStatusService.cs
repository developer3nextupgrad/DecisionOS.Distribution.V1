namespace DecisionOS.Distribution.Domain;

public interface IKpiStatusService
{
    string ComputeStatus(KpiDefinition definition, decimal value);
}
