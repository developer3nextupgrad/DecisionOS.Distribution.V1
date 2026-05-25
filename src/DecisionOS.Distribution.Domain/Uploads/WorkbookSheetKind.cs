namespace DecisionOS.Distribution.Domain.Uploads;

/// <summary>Detected workbook sheet classification (extends <see cref="ReportType"/> for non-import sheets).</summary>
public enum WorkbookSheetKind
{
    Unknown = 0,
    Skip = 1,
    WeeklyRollup = 2,
    Sales = 3,
    Inventory = 4,
    AccountsReceivable = 5,
    AccountsPayable = 6,
    Customer = 7,
    Vendor = 8,
    Product = 9,
    Purchasing = 10,
    Holdover = 11,
    OperationalIssues = 12,
    CompanyProfile = 13,
    ExpectedKpiValidation = 14,
    ExpectedDecisionBoard = 15,
    ValidationSummary = 16
}
