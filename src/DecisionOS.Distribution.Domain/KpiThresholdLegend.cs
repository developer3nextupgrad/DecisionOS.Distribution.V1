namespace DecisionOS.Distribution.Domain;

/// <summary>
/// Human-readable R/Y/G bands — must stay aligned with <see cref="KpiStatusService"/>.
/// </summary>
public static class KpiThresholdLegend
{
    public const string GrayStatusExplanation =
        "GRAY = not scored this week (insufficient upload data). Tile color is always gray; " +
        "green/yellow/red bands below do not apply until the KPI has a real measured value.";

    /// <summary>Band text matching <see cref="KpiStatusService"/> (Target / Amber / Red fields).</summary>
    public static string DescribeBands(KpiDefinition def)
    {
        static string Pct(decimal v) => (v * 100m).ToString("F1") + "%";
        static string Days(decimal v) => v.ToString("F0") + " days";

        if (def.Unit == "pct")
        {
            return def.Direction == KpiDirection.HigherIsBetter
                ? $"Green ≥ {Pct(def.Target)} · Yellow ≥ {Pct(def.RedThreshold)} · Red < {Pct(def.RedThreshold)}"
                : $"Green ≤ {Pct(def.Target)} · Yellow ≤ {Pct(def.AmberThreshold)} · Red > {Pct(def.AmberThreshold)}";
        }

        if (def.Unit == "days")
        {
            return def.Direction == KpiDirection.LowerIsBetter
                ? $"Green ≤ {Days(def.Target)} · Yellow ≤ {Days(def.AmberThreshold)} · Red > {Days(def.AmberThreshold)}"
                : $"Green ≥ {Days(def.Target)} · Yellow ≥ {Days(def.RedThreshold)} · Red < {Days(def.RedThreshold)}";
        }

        return def.Direction == KpiDirection.HigherIsBetter
            ? $"Green ≥ {def.Target:F2} · Yellow ≥ {def.RedThreshold:F2} · Red < {def.RedThreshold:F2}"
            : $"Green ≤ {def.Target:F2} · Yellow ≤ {def.AmberThreshold:F2} · Red > {def.AmberThreshold:F2}";
    }

    public static string DescribeForSnapshot(KpiDefinition def, string status)
    {
        var bands = DescribeBands(def);
        if (status.Equals("GRAY", StringComparison.OrdinalIgnoreCase))
            return GrayStatusExplanation + " When scored: " + bands;
        return "Color bands (scored KPI): " + bands;
    }
}
