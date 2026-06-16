namespace DecisionOS.Distribution.Infrastructure.Workbooks;

/// <summary>Friendly labels and guidance for import column mapping (simplified + classic).</summary>
public static class SystemFieldDisplayCatalog
{
    private sealed record FieldMeta(string Label, string Help, MappingValueKind Kind = MappingValueKind.General);

    private enum MappingValueKind
    {
        General,
        Percent,
        Dollars,
        Date,
        Id
    }

    private static readonly Dictionary<string, FieldMeta> Meta = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Period_End_Date"] = new("Week ending date", "The reporting week for this row (must match other tabs for the same week).", MappingValueKind.Date),
        ["Transaction_Date"] = new("Transaction / invoice date", "Date of the sale or transaction.", MappingValueKind.Date),
        ["Snapshot_Date"] = new("Inventory snapshot date", "As-of date for inventory counts.", MappingValueKind.Date),
        ["AR_Snapshot_Date"] = new("AR snapshot date", "Week-ending or as-of date for receivables.", MappingValueKind.Date),
        ["AP_Snapshot_Date"] = new("AP snapshot date", "Week-ending or as-of date for payables.", MappingValueKind.Date),
        ["Net_Sales"] = new("Net sales ($)", "Revenue after discounts for the week or line.", MappingValueKind.Dollars),
        ["COGS"] = new("Cost of goods sold ($)", "Product cost for the week or line.", MappingValueKind.Dollars),
        ["Gross_Profit"] = new("Gross profit ($)", "Net sales minus COGS (dollar amount).", MappingValueKind.Dollars),
        ["Gross_Margin_Percent"] = new("Gross margin %", "Margin as a ratio (0.28) or percent (28). Not a dollar column.", MappingValueKind.Percent),
        ["Net_Profit_Percent"] = new("Net profit %", "Bottom-line profit as ratio or percent for the week. Map Net_Profit_% / Net Profit % columns here.", MappingValueKind.Percent),
        ["Net_Income"] = new("Net income ($)", "Bottom-line profit in dollars for the week. System divides by net sales to get Net Profit %.", MappingValueKind.Dollars),
        ["Operating_Profit"] = new("Operating profit ($)", "Profit before interest/taxes in dollars. System divides by net sales for Net Profit % when % is not supplied.", MappingValueKind.Dollars),
        ["AR_Over_60_Pct"] = new("AR past-due % (ratio)", "Past-due receivables as ratio or percent — not dollar aging buckets.", MappingValueKind.Percent),
        ["AP_Past_Due_Pct"] = new("AP past-due % (ratio)", "Past-due payables as ratio or percent — not dollar aging buckets.", MappingValueKind.Percent),
        ["Fill_Rate_Pct"] = new("Fill / perfect order %", "Vendor or order fill rate as ratio or percent.", MappingValueKind.Percent),
        ["Inventory_Value"] = new("Inventory value ($)", "Total inventory dollars at week end.", MappingValueKind.Dollars),
        ["AR_Balance"] = new("AR ending balance ($)", "Total accounts receivable at week end (e.g. AR_Ending). Used for cash cycle.", MappingValueKind.Dollars),
        ["AP_Balance"] = new("AP ending balance ($)", "Total accounts payable at week end (e.g. AP_Ending). Used for cash cycle.", MappingValueKind.Dollars),
        ["Cash_Balance"] = new("Cash balance ($)", "Ending cash for the week.", MappingValueKind.Dollars),
        ["Open_Balance"] = new("Open balance ($)", "Unpaid invoice or bill amount.", MappingValueKind.Dollars),
        ["Days_Past_Due"] = new("Days past due", "Age of receivable or payable in days."),
        ["Aging_Bucket"] = new("Aging bucket label", "Text bucket such as 1-30, 31-60, 90+."),
        ["Customer_ID"] = new("Customer ID", "Buyer account code from your ERP.", MappingValueKind.Id),
        ["Customer_Name"] = new("Customer name", "Buyer display name.", MappingValueKind.Id),
        ["SKU_ID"] = new("SKU / item ID", "Product identifier.", MappingValueKind.Id),
        ["Quantity_Sold"] = new("Quantity sold", "Units sold."),
        ["Quantity_On_Hand"] = new("Quantity on hand", "Units in stock."),
        ["Average_Cost"] = new("Average unit cost ($)", "Inventory average cost per unit — not total COGS.", MappingValueKind.Dollars),
        ["Last_Cost"] = new("Last unit cost ($)", "Most recent unit cost.", MappingValueKind.Dollars),
        ["Unit_Of_Measure"] = new("Unit of measure", "EA, CASE, etc."),
        ["Ignore"] = new("Ignore", "Column is not imported.")
    };

    public static string GetLabel(string systemField)
    {
        if (Meta.TryGetValue(systemField, out var m))
            return m.Label;
        return systemField.Replace('_', ' ');
    }

    public static string GetHelp(string systemField)
    {
        if (Meta.TryGetValue(systemField, out var m))
            return m.Help;
        return "DecisionOS standard field used during import.";
    }

    public static string FormatOption(string systemField) =>
        Meta.TryGetValue(systemField, out var m)
            ? $"{m.Label} — {systemField}"
            : systemField;

    public static string? GetMappingWarning(string sourceHeader, string? systemField)
    {
        if (string.IsNullOrWhiteSpace(systemField) || systemField.Equals("Ignore", StringComparison.OrdinalIgnoreCase))
            return null;

        var norm = WorkbookParseHelper.NormalizeHeader(sourceHeader);
        var looksLikeDollars = norm.Contains("total", StringComparison.Ordinal) ||
                               norm.Contains("ending", StringComparison.Ordinal) ||
                               norm.Contains("balance", StringComparison.Ordinal) ||
                               norm.Contains("over90", StringComparison.Ordinal) ||
                               norm.Contains("over60", StringComparison.Ordinal) ||
                               norm.Contains("over30", StringComparison.Ordinal) ||
                               norm.Contains("amount", StringComparison.Ordinal);

        var looksLikePercent = norm.Contains("pct", StringComparison.Ordinal) ||
                               norm.Contains("percent", StringComparison.Ordinal) ||
                               sourceHeader.Contains('%');

        if (Meta.TryGetValue(systemField, out var meta))
        {
            if (meta.Kind == MappingValueKind.Percent && looksLikeDollars && !looksLikePercent)
                return "This Excel column looks like dollars; the selected field expects a percent/ratio.";

            if (meta.Kind == MappingValueKind.Dollars && looksLikePercent && !looksLikeDollars)
                return "This Excel column looks like a percent; the selected field expects a dollar amount.";
        }

        if (systemField.Equals("AR_Over_60_Pct", StringComparison.OrdinalIgnoreCase) &&
            (norm.Contains("artotal", StringComparison.Ordinal) || norm.Equals("artotal", StringComparison.Ordinal)))
            return "AR Total is a dollar column — leave unmapped or Ignore; past-due % is computed from aging buckets when possible.";

        return null;
    }
}
