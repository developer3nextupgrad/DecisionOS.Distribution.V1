namespace DecisionOS.Distribution.Domain;

public class AlertService : IAlertService
{
    public Alert? SelectTopAlert(Guid tenantId, DateOnly periodEnd, IReadOnlyList<KpiSnapshot> snapshots, IReadOnlyList<KpiDefinition> definitions)
    {
        var nonGreen = snapshots
            .Where(s => s.Status is "RED" or "YELLOW")
            .ToList();

        if (nonGreen.Count == 0)
            return null;

        var definitionsById = definitions.ToDictionary(d => d.Id);

        var winner = nonGreen
            .OrderByDescending(s => StatusScore(s.Status))
            .ThenBy(s => definitionsById[s.KpiDefinitionId].AlertPriority)
            .ThenByDescending(s => RelativeDeviation(s, definitionsById))
            .First();

        var definition = definitionsById[winner.KpiDefinitionId];

        return new Alert
        {
            TenantId = tenantId,
            PeriodEnd = periodEnd,
            KpiDefinitionId = winner.KpiDefinitionId,
            Severity = winner.Status,
            ReasonSummary =
                $"{definition.Name} is {winner.Status} at {FormatKpiValue(definition, winner.Value)} (target: {FormatKpiValue(definition, definition.Target)})"
        };
    }

    private static string FormatKpiValue(KpiDefinition def, decimal v) => def.Unit switch
    {
        "pct" => (v * 100m).ToString("F1") + "%",
        "days" => v.ToString("F0") + " days",
        _ => v.ToString("F2")
    };

    private static int StatusScore(string status) => status switch
    {
        "RED" => 3,
        "YELLOW" => 2,
        _ => 1
    };

    private static decimal RelativeDeviation(KpiSnapshot snapshot, Dictionary<int, KpiDefinition> definitions)
    {
        if (!definitions.TryGetValue(snapshot.KpiDefinitionId, out var def) || def.Target == 0)
            return 0;

        return Math.Abs(snapshot.Value - def.Target) / def.Target;
    }
}
