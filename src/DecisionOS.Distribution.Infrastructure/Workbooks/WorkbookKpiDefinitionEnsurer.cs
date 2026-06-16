using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Uploads;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Infrastructure.Workbooks;

public static class WorkbookKpiDefinitionEnsurer
{
    public static async Task EnsureAsync(
        DecisionOsDbContext db,
        WorkbookDetectionResult detection,
        CancellationToken ct = default)
    {
        var existing = await db.KpiDefinitions.Select(k => k.Code).ToListAsync(ct);
        var set = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var sheet in detection.Sheets.Where(s => s.Kind == WorkbookSheetKind.WeeklyRollup))
        {
            TryAdd(set, "GrossMargin%", HasMapped(sheet, "Gross_Margin_Percent") || (HasMapped(sheet, "Net_Sales") && HasMapped(sheet, "COGS")));
            TryAdd(set, "AR_PastDue31p%", HasMapped(sheet, "AR_Over_60_Pct"));
            TryAdd(set, "AP_PastDue31p%", HasMapped(sheet, "AP_Past_Due_Pct"));
            TryAdd(set, "PerfectOrderRate", HasMapped(sheet, "Fill_Rate_Pct"));
            TryAdd(set, "NetProfit%", HasMapped(sheet, "Net_Profit_Percent") || HasMapped(sheet, "Net_Income") || HasMapped(sheet, "Operating_Profit"));
        }

        await db.SaveChangesAsync(ct);

        void TryAdd(HashSet<string> codes, string code, bool condition)
        {
            if (!condition || codes.Contains(code)) return;
            db.KpiDefinitions.Add(CreateDefault(code));
            codes.Add(code);
        }
    }

    private static bool HasMapped(DetectedSheet sheet, string systemField) =>
        sheet.ColumnMappings.Values.Any(v =>
            string.Equals(v, systemField, StringComparison.OrdinalIgnoreCase));

    private static KpiDefinition CreateDefault(string code) => new()
    {
        Code = code,
        Name = code,
        Unit = code.Contains('%') ? "pct" : "days",
        Direction = code.Contains("AR", StringComparison.Ordinal) ||
                    code.Contains("AP", StringComparison.Ordinal) ||
                    code is "DOH" or "CCC"
            ? KpiDirection.LowerIsBetter
            : KpiDirection.HigherIsBetter,
        Target = 0.1m,
        AmberThreshold = 0.08m,
        RedThreshold = 0.05m,
        AlertPriority = 100,
        RecommendedAction = "Review imported workbook KPI.",
        DiagnosticChecks = "Auto-created from simplified workbook import."
    };
}
