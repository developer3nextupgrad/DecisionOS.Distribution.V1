using DecisionOS.Distribution.Domain.Uploads;

namespace DecisionOS.Distribution.Infrastructure.Workbooks;

/// <summary>Plain-language labels for sheet roles (operators / non-technical users).</summary>
public static class WorkbookSheetKindDisplay
{
    public static string Label(WorkbookSheetKind kind) => kind switch
    {
        WorkbookSheetKind.WeeklyRollup => "Weekly financials (one row per week)",
        WorkbookSheetKind.Sales => "Sales by product / customer",
        WorkbookSheetKind.AccountsReceivable => "Accounts receivable (invoices owed to you)",
        WorkbookSheetKind.AccountsPayable => "Accounts payable (bills you owe)",
        WorkbookSheetKind.Inventory => "Inventory on hand",
        WorkbookSheetKind.Customer => "Customer list (buyers)",
        WorkbookSheetKind.Vendor => "Vendor list (suppliers)",
        WorkbookSheetKind.Product => "Product / SKU list",
        WorkbookSheetKind.Purchasing => "Purchase orders",
        WorkbookSheetKind.Holdover => "Holdover / follow-up actions",
        WorkbookSheetKind.Skip => "Skip (do not export)",
        WorkbookSheetKind.Unknown => "Not sure yet — please choose a role",
        WorkbookSheetKind.CompanyProfile => "Company profile (reference only)",
        WorkbookSheetKind.ValidationSummary => "Validation notes (reference only)",
        WorkbookSheetKind.OperationalIssues => "Operational issues (reference only)",
        WorkbookSheetKind.ExpectedKpiValidation => "Expected KPI check (reference only)",
        WorkbookSheetKind.ExpectedDecisionBoard => "Expected decision board (reference only)",
        _ => kind.ToString()
    };

    public static string ShortLabel(WorkbookSheetKind kind) => kind switch
    {
        WorkbookSheetKind.WeeklyRollup => "Weekly financials",
        WorkbookSheetKind.Sales => "Sales detail",
        WorkbookSheetKind.AccountsReceivable => "Receivables (AR)",
        WorkbookSheetKind.AccountsPayable => "Payables (AP)",
        WorkbookSheetKind.Inventory => "Inventory",
        WorkbookSheetKind.Customer => "Customers",
        WorkbookSheetKind.Vendor => "Vendors",
        WorkbookSheetKind.Product => "Products",
        WorkbookSheetKind.Purchasing => "Purchasing",
        WorkbookSheetKind.Holdover => "Holdovers",
        WorkbookSheetKind.Skip => "Skip",
        WorkbookSheetKind.Unknown => "Unknown",
        _ => kind.ToString()
    };

    public static string Help(WorkbookSheetKind kind) => kind switch
    {
        WorkbookSheetKind.WeeklyRollup =>
            "Totals for each week ending date (sales, margin, AR/AP balances). Used for dashboard KPIs.",
        WorkbookSheetKind.Sales =>
            "Line-level sales. Needed for margin and customer drivers.",
        WorkbookSheetKind.AccountsReceivable =>
            "Open customer invoices. Used for past-due AR %.",
        WorkbookSheetKind.AccountsPayable =>
            "Open vendor bills. Used for past-due AP %.",
        WorkbookSheetKind.Inventory =>
            "Stock quantities and values. Used for days-on-hand.",
        WorkbookSheetKind.Skip =>
            "README, notes, or unused tabs. They will not appear in the downloaded file.",
        WorkbookSheetKind.Unknown =>
            "The system could not tell what this tab is. Choose the role that matches the columns, then Save.",
        _ => "Choose the role that best matches this Excel tab."
    };
}
