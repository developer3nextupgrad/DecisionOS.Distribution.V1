using DecisionOS.Distribution.Domain;

namespace DecisionOS.Distribution.Infrastructure;

internal static class KpiScoringNarrative
{
    public static string MissingDataHeadline(string code) => code switch
    {
        "GrossMargin%" => "Missing sales total and/or product cost.",
        "AR_PastDue31p%" => "Missing customer aging or past-due balances.",
        "AP_PastDue31p%" => "Missing vendor aging or past-due balances.",
        "DOH" => "Missing inventory value and/or weekly product cost.",
        "CCC" => "Missing sales, customer balances, vendor balances, and/or inventory.",
        "NetProfit%" => "Net profit percent not supplied in this upload.",
        "PerfectOrderRate" => "On-time delivery rate not supplied in this upload.",
        _ => "Not enough information in the uploaded files."
    };

    public static (string Line1, string Line2) ForScoredKpi(KpiDefinition def, decimal value, string status)
    {
        var gap = DescribeGap(def, value);
        var line1 = status switch
        {
            "GREEN" => "On track this week.",
            "YELLOW" => $"Needs attention: {gap}",
            "RED" => $"Urgent: {gap}",
            _ => gap
        };

        return (line1, OwnerLanguage.ExpandFinanceAbbreviations(def.RecommendedAction.Trim()));
    }

    private static string DescribeGap(KpiDefinition def, decimal value)
    {
        if (def.Direction == KpiDirection.HigherIsBetter)
        {
            if (value >= def.Target) return "at or above your goal.";
            var gap = def.Target - value;
            return OwnerLanguage.FormatGap(def, gap, "below", FormatGoal(def));
        }

        if (value <= def.Target) return "at or below your goal.";
        var over = value - def.Target;
        return OwnerLanguage.FormatGap(def, over, "above", FormatGoal(def));
    }

    private static string FormatGoal(KpiDefinition def) => def.Unit switch
    {
        "pct" => (def.Target * 100m).ToString("F1") + "%",
        "days" => def.Target.ToString("F0") + " days",
        _ => def.Target.ToString("F2")
    };
}
