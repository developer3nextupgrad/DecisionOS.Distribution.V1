namespace DecisionOS.Distribution.Domain;

public interface IAlertService
{
    Alert? SelectTopAlert(Guid tenantId, DateOnly periodEnd, IReadOnlyList<KpiSnapshot> snapshots, IReadOnlyList<KpiDefinition> definitions);
}
