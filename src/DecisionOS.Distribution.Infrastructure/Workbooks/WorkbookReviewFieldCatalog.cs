using DecisionOS.Distribution.Domain.Uploads;

namespace DecisionOS.Distribution.Infrastructure.Workbooks;

public static class WorkbookReviewFieldCatalog
{
    public static IReadOnlyList<string> ForKind(WorkbookSheetKind kind) => kind switch
    {
        WorkbookSheetKind.Sales => SystemFields.Sales,
        WorkbookSheetKind.Inventory => SystemFields.Inventory,
        WorkbookSheetKind.AccountsReceivable => SystemFields.AccountsReceivable,
        WorkbookSheetKind.AccountsPayable => SystemFields.AccountsPayable,
        WorkbookSheetKind.Customer => SystemFields.Customer,
        WorkbookSheetKind.Vendor => VendorFields,
        WorkbookSheetKind.Product => SystemFields.Product,
        WorkbookSheetKind.Purchasing => SystemFields.Purchasing,
        WorkbookSheetKind.WeeklyRollup => RollupFields,
        WorkbookSheetKind.Holdover => new[] { "Customer_ID", "Customer_Name", "Ignore" },
        WorkbookSheetKind.Skip or WorkbookSheetKind.Unknown => Array.Empty<string>(),
        WorkbookSheetKind.ExpectedKpiValidation or WorkbookSheetKind.ExpectedDecisionBoard
            or WorkbookSheetKind.CompanyProfile or WorkbookSheetKind.ValidationSummary
            or WorkbookSheetKind.OperationalIssues => Array.Empty<string>(),
        _ => SystemFields.Generic
    };

    /// <summary>Sheets that are documentation / validation only — not normalized into weekly KPI tables.</summary>
    public static bool IsReferenceOnly(WorkbookSheetKind kind) =>
        kind is WorkbookSheetKind.Skip
            or WorkbookSheetKind.Unknown
            or WorkbookSheetKind.ExpectedKpiValidation
            or WorkbookSheetKind.ExpectedDecisionBoard
            or WorkbookSheetKind.CompanyProfile
            or WorkbookSheetKind.ValidationSummary
            or WorkbookSheetKind.OperationalIssues;

    public static ReportType? ReportTypeForKind(WorkbookSheetKind kind) => kind switch
    {
        WorkbookSheetKind.Sales => ReportType.Sales,
        WorkbookSheetKind.Inventory => ReportType.Inventory,
        WorkbookSheetKind.AccountsReceivable => ReportType.AccountsReceivable,
        WorkbookSheetKind.AccountsPayable => ReportType.AccountsPayable,
        WorkbookSheetKind.Customer => ReportType.Customer,
        WorkbookSheetKind.Vendor => ReportType.Vendor,
        WorkbookSheetKind.Product => ReportType.Product,
        WorkbookSheetKind.Purchasing => ReportType.Purchasing,
        WorkbookSheetKind.WeeklyRollup => ReportType.FinancialStatement,
        _ => null
    };

    private static readonly IReadOnlyList<string> VendorFields =
        SystemFields.Vendor.Concat(["Fill_Rate_Pct", "Ignore"]).ToArray();

    private static readonly IReadOnlyList<string> RollupFields =
    [
        "Period_End_Date", "Net_Sales", "COGS", "Gross_Margin_Percent", "Gross_Profit",
        "AR_Over_60_Pct", "AP_Past_Due_Pct", "Fill_Rate_Pct", "Cash_Balance", "Inventory_Value",
        "Net_Income", "Net_Profit_Percent", "Operating_Profit", "Revenue", "Ignore"
    ];
}
