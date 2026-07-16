using DecisionOS.Distribution.Domain.Uploads;

namespace DecisionOS.Distribution.Infrastructure.Workbooks;

/// <summary>Turns technical detection warnings into operator-friendly messages + fixes.</summary>
public static class ExcelMapperWarningGuide
{
    public static ExcelMapperUserMessage Explain(string rawWarning)
    {
        if (string.IsNullOrWhiteSpace(rawWarning))
            return new ExcelMapperUserMessage { Message = rawWarning };

        if (rawWarning.Contains("could not be classified", StringComparison.OrdinalIgnoreCase))
        {
            var sheet = ExtractQuoted(rawWarning) ?? "this tab";
            return new ExcelMapperUserMessage
            {
                Message = $"We are not sure what tab “{sheet}” contains.",
                SuggestedFix =
                    "Open that row → choose the correct Role (Sales, Weekly financials, AR, etc.) → Save mappings. " +
                    "If it is a notes/README tab, set Role to Skip."
            };
        }

        if (rawWarning.Contains("No weekly rollup", StringComparison.OrdinalIgnoreCase))
            return new ExcelMapperUserMessage
            {
                Message = "No weekly totals tab was found.",
                SuggestedFix =
                    "If one of your tabs has one row per week with sales/margin totals, set its Role to “Weekly financials”. " +
                    "Otherwise KPIs will rely on sales detail alone."
            };

        if (rawWarning.Contains("No sales detail", StringComparison.OrdinalIgnoreCase))
            return new ExcelMapperUserMessage
            {
                Message = "No sales detail tab was found.",
                SuggestedFix =
                    "Find the tab with product or customer sales lines and set Role to “Sales by product / customer”, then Save."
            };

        if (rawWarning.Contains("No week-ending dates", StringComparison.OrdinalIgnoreCase))
            return new ExcelMapperUserMessage
            {
                Message = "No week-ending dates were found in the file.",
                SuggestedFix =
                    "Map a date column to “Week ending date” on the weekly financials or sales tab (Edit mappings), then Save."
            };

        if (rawWarning.Contains("Anchor auto-adjusted", StringComparison.OrdinalIgnoreCase) ||
            rawWarning.Contains("Anchor date excluded", StringComparison.OrdinalIgnoreCase))
            return new ExcelMapperUserMessage
            {
                Message = "Week filter dates needed adjustment.",
                SuggestedFix =
                    "When you upload the mapped file in Operations → Uploads (Simplified), leave the anchor blank or set it to the earliest week shown here."
            };

        if (rawWarning.Contains("AR_Over_60", StringComparison.OrdinalIgnoreCase))
            return new ExcelMapperUserMessage
            {
                Message = "AR past-due % from the weekly tab is an approximation of the dashboard AR past-due KPI.",
                SuggestedFix = "No action needed unless you prefer to map detailed AR aging instead."
            };

        if (rawWarning.Contains("implausible date", StringComparison.OrdinalIgnoreCase))
            return new ExcelMapperUserMessage
            {
                Message = "Some values looked like dates but were ignored (often ID numbers).",
                SuggestedFix = "Confirm your week-ending column maps to “Week ending date”, not an ID column."
            };

        if (rawWarning.Contains("invoice dates excluded", StringComparison.OrdinalIgnoreCase) ||
            rawWarning.Contains("Reporting weeks taken", StringComparison.OrdinalIgnoreCase))
            return new ExcelMapperUserMessage
            {
                Message = "Week list comes from weekly totals and sales week-ending columns (not every invoice date).",
                SuggestedFix = "That is expected. Check the week chips below look correct for your upload."
            };

        return new ExcelMapperUserMessage
        {
            Message = rawWarning,
            SuggestedFix = "Review sheet roles and column mappings below, then Save before downloading."
        };
    }

    public static string LowConfidenceMessage(string sheetName) =>
        $"We’re unsure what “{sheetName}” is for.";

    public static string LowConfidenceFix() =>
        "Choose the correct Role in the dropdown, then click Save mappings. The warning clears after you save.";

    private static string? ExtractQuoted(string text)
    {
        var start = text.IndexOf('\'');
        var end = text.LastIndexOf('\'');
        if (start >= 0 && end > start)
            return text[(start + 1)..end];
        return null;
    }
}
