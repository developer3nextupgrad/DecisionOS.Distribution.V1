namespace DecisionOS.Distribution.Domain.Import;

public static class ImportRowValidator
{
    private static readonly HashSet<string> AllowedStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "GREEN", "YELLOW", "RED" };

    public static void ValidateKpiValue(
        KpiDefinition definition,
        decimal value,
        ImportValidationResult result,
        int rowNumber)
    {
        if (definition.MinValue is { } min && value < min)
            result.Add("KPI", $"Value {value} is below configured minimum {min}.", rowNumber, "value");

        if (definition.MaxValue is { } max && value > max)
            result.Add("KPI", $"Value {value} is above configured maximum {max}.", rowNumber, "value");

        if (definition.Unit == "pct" && (value < 0m || value > 1.5m))
            result.Add("KPI", "Percentage KPIs are expected as fractions 0–1 (e.g. 0.27 for 27%).", rowNumber, "value");
    }

    public static void ValidateDriverRow(
        string pillarCode,
        string driverName,
        string? driverCode,
        decimal current,
        int rank,
        string status,
        int? fixProgress,
        ILookup<string, DriverDefinition> activeDefinitionsByPillar,
        ImportValidationResult result,
        int rowNumber)
    {
        if (string.IsNullOrWhiteSpace(pillarCode))
            result.Add("Driver", "pillar_code is required.", rowNumber, "pillar_code");

        if (string.IsNullOrWhiteSpace(driverName))
            result.Add("Driver", "driver_name is required.", rowNumber, "driver_name");

        if (rank < 1)
            result.Add("Driver", "rank must be >= 1.", rowNumber, "rank");

        if (string.IsNullOrWhiteSpace(status) || !AllowedStatuses.Contains(status.Trim()))
            result.Add("Driver", "status must be GREEN, YELLOW, or RED.", rowNumber, "status");

        if (fixProgress is < 0 or > 100)
            result.Add("Driver", "fix_progress must be between 0 and 100.", rowNumber, "fix_progress");

        var defsForPillar = activeDefinitionsByPillar[pillarCode].Where(d => d.IsActive).ToList();
        if (defsForPillar.Count == 0)
            return;

        if (string.IsNullOrWhiteSpace(driverCode))
        {
            result.Add("Driver",
                $"driver_code is required when a driver catalog exists for pillar '{pillarCode}'.",
                rowNumber, "driver_code");
            return;
        }

        var match = defsForPillar.FirstOrDefault(d =>
            string.Equals(d.DriverCode, driverCode.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null)
            result.Add("Driver",
                $"driver_code '{driverCode}' is not in the active catalog for pillar '{pillarCode}'.",
                rowNumber, "driver_code");
    }
}
