namespace DecisionOS.Distribution.Domain;

public class WeeklyFocusService : IWeeklyFocusService
{
    public WeeklyFocus? GenerateWeeklyFocus(Guid tenantId, DateOnly periodEnd, Alert? topAlert, IReadOnlyList<KpiDefinition> definitions)
    {
        if (topAlert is null)
            return null;

        var definition = definitions.FirstOrDefault(d => d.Id == topAlert.KpiDefinitionId);
        if (definition is null)
            return null;

        return new WeeklyFocus
        {
            TenantId = tenantId,
            PeriodEnd = periodEnd,
            KpiDefinitionId = definition.Id,
            DecisionQuestion = $"Will we address {definition.Name} this week?",
            RecommendedAction = definition.RecommendedAction,
            WhyNow = $"{definition.Name} is {topAlert.Severity}. {definition.DiagnosticChecks}",
            Owner = "Operations",
            Cadence = "Weekly"
        };
    }
}
