namespace DecisionOS.Distribution.Domain;

public interface IWeeklyFocusService
{
    WeeklyFocus? GenerateWeeklyFocus(Guid tenantId, DateOnly periodEnd, Alert? topAlert, IReadOnlyList<KpiDefinition> definitions);
}
