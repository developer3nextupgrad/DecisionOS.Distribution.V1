using DecisionOS.Distribution.Domain;

namespace DecisionOS.Distribution.Infrastructure;

internal static class KpiScoringNarrative
{
    public static string MissingDataHeadline(string code) => code switch
    {
        "GrossMargin%" => "Missing sales net revenue and/or COGS.",
        "AR_PastDue31p%" => "Missing AR aging balances or past-due signals.",
        "AP_PastDue31p%" => "Missing AP aging balances or past-due signals.",
        "DOH" => "Missing inventory value and/or weekly COGS.",
        "CCC" => "Missing sales, AR, AP, and/or inventory for cash cycle.",
        "NetProfit%" => "Net Profit % not supplied in this upload.",
        "PerfectOrderRate" => "Perfect Order rate not supplied in this upload.",
        _ => "Insufficient data from uploaded package."
    };

    public static (string Line1, string Line2) ForScoredKpi(KpiDefinition def, decimal value, string status)
    {
        var gap = DescribeGap(def, value);
        var line1 = status switch
        {
            "GREEN" => "On target this week.",
            "YELLOW" => $"Attention: {gap}",
            "RED" => $"Critical: {gap}",
            _ => gap
        };

        var action = def.RecommendedAction.Trim();
        var line2 = action.Length <= 90 ? action : action[..87] + "...";
        return (line1, line2);
    }

    private static string DescribeGap(KpiDefinition def, decimal value)
    {
        if (def.Direction == KpiDirection.HigherIsBetter)
        {
            if (value >= def.Target) return "at or above target.";
            var gap = def.Target - value;
            return def.Unit == "pct"
                ? $"{gap * 100m:F1} pp below target"
                : def.Unit == "days"
                    ? $"{gap:F0} days below target"
                    : $"{gap:F2} below target";
        }

        if (value <= def.Target) return "at or below target.";
        var over = value - def.Target;
        return def.Unit == "pct"
            ? $"{over * 100m:F1} pp above target"
            : def.Unit == "days"
                ? $"{over:F0} days above target"
                : $"{over:F2} above target";
    }
}
