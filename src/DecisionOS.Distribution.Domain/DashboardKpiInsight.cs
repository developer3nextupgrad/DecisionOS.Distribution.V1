using System.Text.Json.Serialization;

namespace DecisionOS.Distribution.Domain;

public sealed class DashboardKpiInsight
{
    public int SnapshotId { get; init; }
    public string Name { get; init; } = "";
    public string Code { get; init; } = "";
    public string Unit { get; init; } = "";
    public string FormattedValue { get; init; } = "";
    public string FormattedTarget { get; init; } = "";
    public string FormattedWow { get; init; } = "";
    public string Status { get; init; } = "";
    public string StatusLabel { get; init; } = "";
    public string? DataConfidence { get; init; }
    public string? DataConfidenceLabel { get; init; }
    public string StatusHeadline { get; init; } = "";
    public string GapSummary { get; init; } = "";
    public string ThresholdSummary { get; init; } = "";
    public string ColorCodingNote { get; init; } = "";
    public string? CardDetailLine1 { get; init; }
    public string? CardDetailLine2 { get; init; }
    public string RecommendedAction { get; init; } = "";
    public string DiagnosticChecks { get; init; } = "";
    public IReadOnlyList<string> MissingDataItems { get; init; } = Array.Empty<string>();
    public string? MissingDataSummary { get; init; }
    public bool IsTopAlert { get; init; }
    public string? AlertReason { get; init; }
    public bool IsWeeklyFocusKpi { get; init; }
    public string? WeeklyFocusAction { get; init; }
    public IReadOnlyList<DashboardKpiDriverInsight> RelatedDrivers { get; init; } = Array.Empty<DashboardKpiDriverInsight>();

    [JsonIgnore]
    public static IReadOnlyDictionary<string, string[]> MissingDataByCode { get; } =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["GrossMargin%"] = OwnerLanguage.PlainMissingDataItems("GrossMargin%").ToArray(),
            ["AR_PastDue31p%"] = OwnerLanguage.PlainMissingDataItems("AR_PastDue31p%").ToArray(),
            ["AP_PastDue31p%"] = OwnerLanguage.PlainMissingDataItems("AP_PastDue31p%").ToArray(),
            ["DOH"] = OwnerLanguage.PlainMissingDataItems("DOH").ToArray(),
            ["CCC"] = OwnerLanguage.PlainMissingDataItems("CCC").ToArray(),
            ["NetProfit%"] = OwnerLanguage.PlainMissingDataItems("NetProfit%").ToArray(),
            ["PerfectOrderRate"] = OwnerLanguage.PlainMissingDataItems("PerfectOrderRate").ToArray()
        };
}

public sealed class DashboardKpiDriverInsight
{
    public string DriverName { get; init; } = "";
    public string Status { get; init; } = "";
    public string StatusLabel { get; init; } = "";
    public string? Owner { get; init; }
    public int FixProgressPercent { get; init; }
    public string? WhyItMatters { get; init; }
}

public static class DashboardKpiInsightBuilder
{
    public static Dictionary<int, DashboardKpiInsight> Build(
        IReadOnlyList<KpiSnapshot> snapshots,
        IReadOnlyList<DriverValue> drivers,
        Alert? topAlert,
        WeeklyFocus? focus,
        Func<KpiSnapshot, string> formatValue,
        Func<KpiDefinition, string> formatTarget,
        Func<KpiSnapshot, string> formatWow)
    {
        var result = new Dictionary<int, DashboardKpiInsight>();
        foreach (var snapshot in snapshots)
        {
            var def = snapshot.KpiDefinition;
            var related = drivers
                .Where(d => string.Equals(d.PillarCode, def.Code, StringComparison.OrdinalIgnoreCase))
                .OrderBy(d => d.Rank)
                .Take(4)
                .Select(d => new DashboardKpiDriverInsight
                {
                    DriverName = d.DriverName,
                    Status = d.Status,
                    StatusLabel = OwnerLanguage.PlainStatusLabel(d.Status),
                    Owner = string.IsNullOrWhiteSpace(d.Owner) ? null : d.Owner.Trim(),
                    FixProgressPercent = FixProgressPercent(d),
                    WhyItMatters = string.IsNullOrWhiteSpace(d.WhyItMatters)
                        ? null
                        : OwnerLanguage.ExpandFinanceAbbreviations(d.WhyItMatters.Trim())
                })
                .ToList();

            var isTopAlert = topAlert?.KpiDefinitionId == def.Id;
            var isFocus = focus?.KpiDefinitionId == def.Id;

            result[snapshot.Id] = new DashboardKpiInsight
            {
                SnapshotId = snapshot.Id,
                Name = def.Name,
                Code = def.Code,
                Unit = def.Unit,
                FormattedValue = formatValue(snapshot),
                FormattedTarget = formatTarget(def),
                FormattedWow = formatWow(snapshot),
                Status = snapshot.Status,
                StatusLabel = OwnerLanguage.PlainStatusLabel(snapshot.Status),
                DataConfidence = snapshot.DataConfidence,
                DataConfidenceLabel = OwnerLanguage.PlainDataConfidence(snapshot.DataConfidence),
                StatusHeadline = OwnerLanguage.PlainStatusHeadline(def, snapshot),
                GapSummary = BuildGapSummary(def, snapshot, formatTarget),
                ThresholdSummary = OwnerLanguage.PlainThresholdBands(def),
                ColorCodingNote = OwnerLanguage.PlainColorCodingNote(def, snapshot.Status),
                CardDetailLine1 = OwnerLanguage.ExpandFinanceAbbreviations(snapshot.CardDetailLine1),
                CardDetailLine2 = OwnerLanguage.ExpandFinanceAbbreviations(snapshot.CardDetailLine2),
                RecommendedAction = OwnerLanguage.ExpandFinanceAbbreviations(def.RecommendedAction),
                DiagnosticChecks = OwnerLanguage.ExpandFinanceAbbreviations(def.DiagnosticChecks),
                MissingDataItems = ResolveMissingItems(def.Code, snapshot.Status),
                MissingDataSummary = OwnerLanguage.PlainMissingDataSummary(def.Code, snapshot.Status),
                IsTopAlert = isTopAlert,
                AlertReason = isTopAlert ? OwnerLanguage.ExpandFinanceAbbreviations(topAlert?.ReasonSummary) : null,
                IsWeeklyFocusKpi = isFocus,
                WeeklyFocusAction = isFocus ? OwnerLanguage.ExpandFinanceAbbreviations(focus?.RecommendedAction) : null,
                RelatedDrivers = related
            };
        }

        return result;
    }

    private static IReadOnlyList<string> ResolveMissingItems(string code, string status)
    {
        if (!status.Equals("GRAY", StringComparison.OrdinalIgnoreCase))
            return Array.Empty<string>();

        return OwnerLanguage.PlainMissingDataItems(code).ToList();
    }

    private static string BuildGapSummary(KpiDefinition def, KpiSnapshot snapshot, Func<KpiDefinition, string> formatTarget)
    {
        if (snapshot.Status.Equals("GRAY", StringComparison.OrdinalIgnoreCase))
            return "No measured value for this week.";

        var value = snapshot.Value;
        var target = def.Target;
        var goal = formatTarget(def);

        if (def.Direction == KpiDirection.HigherIsBetter)
        {
            if (value >= target)
                return $"At or above your goal ({goal}).";
            return OwnerLanguage.FormatGap(def, target - value, "below", goal);
        }

        if (value <= target)
            return $"At or below your goal ({goal}).";
        return OwnerLanguage.FormatGap(def, value - target, "above", goal);
    }

    private static int FixProgressPercent(DriverValue d)
    {
        if (d.FixProgressPercent is >= 0 and <= 100)
            return d.FixProgressPercent.Value;

        return d.Status.ToUpperInvariant() switch
        {
            "GREEN" => 100,
            "YELLOW" => 55,
            "RED" => 25,
            _ => 0
        };
    }
}
