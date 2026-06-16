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
    public string? DataConfidence { get; init; }
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
            ["GrossMargin%"] =
            [
                "Sales export with Net_Sales (or equivalent) populated",
                "Sales export with COGS populated",
                "Positive net sales for the week"
            ],
            ["AR_PastDue31p%"] =
            [
                "Accounts receivable aging with Open_Balance",
                "Days_Past_Due or aging bucket (31+, 60+, 90+)",
                "Positive total AR balance for the week"
            ],
            ["AP_PastDue31p%"] =
            [
                "Accounts payable aging with Open_Balance",
                "Days_Past_Due or aging bucket for past-due detection",
                "Positive total AP balance for the week"
            ],
            ["DOH"] =
            [
                "Inventory snapshot with Inventory_Value",
                "Sales COGS for the same week (used as daily burn rate)",
                "Positive inventory and COGS totals"
            ],
            ["CCC"] =
            [
                "Sales: Net_Sales and COGS",
                "AR and AP open balances for the week",
                "Inventory value (for days-in-inventory component)"
            ],
            ["NetProfit%"] =
            [
                "Weekly rollup with Net Profit % (ratio or percent), or",
                "Net Income ($) or Operating Profit ($) on the same row as Net Sales",
                "Mapped on Weekly_Financials (or rollup) with a week-ending date"
            ],
            ["PerfectOrderRate"] =
            [
                "Fulfillment / service metrics feed for the week",
                "Not computed from sales/AR alone in V1 web import"
            ]
        };
}

public sealed class DashboardKpiDriverInsight
{
    public string DriverName { get; init; } = "";
    public string Status { get; init; } = "";
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
                    Owner = string.IsNullOrWhiteSpace(d.Owner) ? null : d.Owner.Trim(),
                    FixProgressPercent = FixProgressPercent(d),
                    WhyItMatters = string.IsNullOrWhiteSpace(d.WhyItMatters) ? null : d.WhyItMatters.Trim()
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
                DataConfidence = snapshot.DataConfidence,
                StatusHeadline = BuildStatusHeadline(def, snapshot),
                GapSummary = BuildGapSummary(def, snapshot, formatTarget),
                ThresholdSummary = KpiThresholdLegend.DescribeBands(def),
                ColorCodingNote = KpiThresholdLegend.DescribeForSnapshot(def, snapshot.Status),
                CardDetailLine1 = snapshot.CardDetailLine1,
                CardDetailLine2 = snapshot.CardDetailLine2,
                RecommendedAction = def.RecommendedAction,
                DiagnosticChecks = def.DiagnosticChecks,
                MissingDataItems = ResolveMissingItems(def.Code, snapshot.Status),
                MissingDataSummary = BuildMissingSummary(def.Code, snapshot.Status),
                IsTopAlert = isTopAlert,
                AlertReason = isTopAlert ? topAlert?.ReasonSummary : null,
                IsWeeklyFocusKpi = isFocus,
                WeeklyFocusAction = isFocus ? focus?.RecommendedAction : null,
                RelatedDrivers = related
            };
        }

        return result;
    }

    private static IReadOnlyList<string> ResolveMissingItems(string code, string status)
    {
        if (!status.Equals("GRAY", StringComparison.OrdinalIgnoreCase))
            return Array.Empty<string>();

        return DashboardKpiInsight.MissingDataByCode.TryGetValue(code, out var items)
            ? items
            : ["Upload package for this week is incomplete for this KPI."];
    }

    private static string? BuildMissingSummary(string code, string status)
    {
        if (!status.Equals("GRAY", StringComparison.OrdinalIgnoreCase))
            return null;

        return DashboardKpiInsight.MissingDataByCode.ContainsKey(code)
            ? "This KPI could not be scored from the current upload. Add or fix the items below, then re-import."
            : "Insufficient source data for this KPI in the uploaded package.";
    }

    private static string BuildStatusHeadline(KpiDefinition def, KpiSnapshot snapshot)
    {
        if (snapshot.Status.Equals("GRAY", StringComparison.OrdinalIgnoreCase))
            return "Insufficient data — upload more source files or columns to score this KPI.";

        return snapshot.Status.ToUpperInvariant() switch
        {
            "GREEN" => def.Direction == KpiDirection.HigherIsBetter
                ? "On target — at or above goal."
                : "On target — at or below goal.",
            "YELLOW" => def.Direction == KpiDirection.HigherIsBetter
                ? "Attention — below target but above the red band."
                : "Attention — above target but not yet in the red band.",
            "RED" => def.Direction == KpiDirection.HigherIsBetter
                ? "Critical — below the red threshold."
                : "Critical — above the red threshold.",
            _ => "Status unknown for this week."
        };
    }

    private static string BuildGapSummary(KpiDefinition def, KpiSnapshot snapshot, Func<KpiDefinition, string> formatTarget)
    {
        if (snapshot.Status.Equals("GRAY", StringComparison.OrdinalIgnoreCase))
            return "No measured value for this week.";

        var value = snapshot.Value;
        var target = def.Target;

        if (def.Direction == KpiDirection.HigherIsBetter)
        {
            if (value >= target)
                return $"At or above target ({formatTarget(def)}).";
            var gap = target - value;
            return FormatGap(def, gap, "below target", formatTarget(def));
        }

        if (value <= target)
            return $"At or below target ({formatTarget(def)}).";
        var over = value - target;
        return FormatGap(def, over, "above target", formatTarget(def));
    }

    private static string FormatGap(KpiDefinition def, decimal gap, string direction, string targetLabel)
    {
        if (def.Unit == "pct")
            return $"{(gap * 100m):F1} pp {direction} (target {targetLabel}).";
        if (def.Unit == "days")
            return $"{gap:F0} days {direction} (target {targetLabel}).";
        return $"{gap:F2} {direction} (target {targetLabel}).";
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
