namespace DecisionOS.Distribution.Domain;

/// <summary>Plain-language helpers for business-owner-facing copy (no jargon or abbreviations).</summary>
public static class OwnerLanguage
{
    public const string CccDiagnosticChecks =
        "Look at how fast customers pay you, how long stock sits on the shelf, and how long you take to pay vendors — focus on whichever changed the most since last week.";

    public static string PlainStatusLabel(string? status) => (status ?? "").ToUpperInvariant() switch
    {
        "GREEN" => "On track",
        "YELLOW" => "Needs attention",
        "RED" => "Urgent",
        "GRAY" => "Not enough data",
        _ => "Unknown"
    };

    public static string PlainDataConfidence(string? confidence) => (confidence ?? "").ToLowerInvariant() switch
    {
        "high" => "High — upload looks complete",
        "medium" => "Medium — most data present",
        "low" => "Low — key pieces may be missing",
        _ => confidence ?? "—"
    };

    public static string ExpandFinanceAbbreviations(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text
            .Replace("DSO", "how long customers take to pay you", StringComparison.OrdinalIgnoreCase)
            .Replace("DIO", "how long inventory sits before it sells", StringComparison.OrdinalIgnoreCase)
            .Replace("DPO", "how long you take to pay vendors", StringComparison.OrdinalIgnoreCase)
            .Replace("COGS", "product cost", StringComparison.OrdinalIgnoreCase)
            .Replace("A/R", "money customers owe you", StringComparison.OrdinalIgnoreCase)
            .Replace("AR ", "customer receivables ", StringComparison.OrdinalIgnoreCase)
            .Replace("A/P", "money you owe vendors", StringComparison.OrdinalIgnoreCase)
            .Replace("AP ", "vendor payables ", StringComparison.OrdinalIgnoreCase)
            .Replace("SKU", "product", StringComparison.OrdinalIgnoreCase)
            .Replace("Opex", "operating expenses", StringComparison.OrdinalIgnoreCase);
    }

    public static string FormatGap(KpiDefinition def, decimal gap, string directionWord, string goalLabel)
    {
        if (def.Unit == "pct")
            return $"{gap * 100m:F1} percentage points {directionWord} your goal ({goalLabel}).";
        if (def.Unit == "days")
            return $"{gap:F0} days {directionWord} your goal ({goalLabel}).";
        return $"{gap:F2} {directionWord} your goal ({goalLabel}).";
    }

    public static string PlainStatusHeadline(KpiDefinition def, KpiSnapshot snapshot)
    {
        if (snapshot.Status.Equals("GRAY", StringComparison.OrdinalIgnoreCase))
            return "We do not have enough information from your upload to score this item yet.";

        return snapshot.Status.ToUpperInvariant() switch
        {
            "GREEN" => def.Direction == KpiDirection.HigherIsBetter
                ? "On track — at or above your goal."
                : "On track — at or below your goal.",
            "YELLOW" => def.Direction == KpiDirection.HigherIsBetter
                ? "Needs attention — below your goal but not yet urgent."
                : "Needs attention — above your goal but not yet urgent.",
            "RED" => def.Direction == KpiDirection.HigherIsBetter
                ? "Urgent — well below your goal."
                : "Urgent — well above your goal.",
            _ => "Status could not be determined for this week."
        };
    }

    public static string PlainThresholdBands(KpiDefinition def)
    {
        static string Pct(decimal v) => (v * 100m).ToString("F1") + "%";
        static string Days(decimal v) => v.ToString("F0") + " days";

        if (def.Unit == "pct")
        {
            return def.Direction == KpiDirection.HigherIsBetter
                ? $"On track at {Pct(def.Target)} or higher · Needs attention from {Pct(def.RedThreshold)} up to {Pct(def.Target)} · Urgent below {Pct(def.RedThreshold)}"
                : $"On track at {Pct(def.Target)} or lower · Needs attention from {Pct(def.Target)} up to {Pct(def.AmberThreshold)} · Urgent above {Pct(def.AmberThreshold)}";
        }

        if (def.Unit == "days")
        {
            return def.Direction == KpiDirection.LowerIsBetter
                ? $"On track at {Days(def.Target)} or fewer · Needs attention from {Days(def.Target)} up to {Days(def.AmberThreshold)} · Urgent above {Days(def.AmberThreshold)}"
                : $"On track at {Days(def.Target)} or more · Needs attention from {Days(def.RedThreshold)} up to {Days(def.Target)} · Urgent below {Days(def.RedThreshold)}";
        }

        return KpiThresholdLegend.DescribeBands(def);
    }

    public static string PlainColorCodingNote(KpiDefinition def, string status)
    {
        var bands = PlainThresholdBands(def);
        if (status.Equals("GRAY", StringComparison.OrdinalIgnoreCase))
            return "Gray means we could not score this yet — your upload is missing required information. Once scored: " + bands;
        return "What the colors mean: " + bands;
    }

    public static IReadOnlyList<string> PlainMissingDataItems(string code)
    {
        if (MissingDataByCode.TryGetValue(code, out var items))
            return items;
        return ["Upload more complete weekly files, then import again."];
    }

    public static string? PlainMissingDataSummary(string code, string status)
    {
        if (!status.Equals("GRAY", StringComparison.OrdinalIgnoreCase))
            return null;

        return MissingDataByCode.ContainsKey(code)
            ? "Add the items below to your weekly upload, then import again so we can score this."
            : "Your upload does not include enough information for this measure.";
    }

    private static readonly Dictionary<string, string[]> MissingDataByCode =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["GrossMargin%"] =
            [
                "A sales report showing how much you sold this week",
                "Product cost for those sales (what you paid for the goods)",
                "Sales total greater than zero for the week"
            ],
            ["AR_PastDue31p%"] =
            [
                "A customer aging report showing who owes you and how much",
                "How many days each invoice is past due (31 days or more)",
                "Total amount customers still owe you"
            ],
            ["AP_PastDue31p%"] =
            [
                "A vendor aging report showing what you owe and to whom",
                "How many days each bill is past due (31 days or more)",
                "Total amount you still owe vendors"
            ],
            ["DOH"] =
            [
                "Current value of inventory on hand",
                "Product cost sold this week (to see how fast inventory moves)",
                "Both inventory value and weekly product cost greater than zero"
            ],
            ["CCC"] =
            [
                "Sales and product cost for the week",
                "How much customers owe you and how much you owe vendors",
                "Inventory value on hand"
            ],
            ["NetProfit%"] =
            [
                "Net profit as a percent of sales on your weekly financial summary, or",
                "Net income or operating profit in dollars on the same row as sales",
                "A week-ending date on your financial summary tab"
            ],
            ["PerfectOrderRate"] =
            [
                "On-time delivery and order accuracy from your operations or service reports",
                "This is not calculated from sales or customer aging alone"
            ]
        };
}
