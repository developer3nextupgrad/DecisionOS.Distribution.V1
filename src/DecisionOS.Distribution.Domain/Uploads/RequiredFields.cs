namespace DecisionOS.Distribution.Domain.Uploads;

public static class RequiredFields
{
    public static IReadOnlySet<string> Required(ReportType reportType) => reportType switch
    {
        ReportType.Sales => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Transaction_Date",
            "Quantity_Sold",
            "Net_Sales"
        },
        ReportType.Inventory => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Snapshot_Date",
            "SKU_ID",
            "Quantity_On_Hand"
        },
        ReportType.AccountsReceivable => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AR_Snapshot_Date",
            "Customer_ID",
            "Customer_Name",
            "Open_Balance"
        },
        ReportType.AccountsPayable => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AP_Snapshot_Date",
            "Vendor_ID",
            "Vendor_Name",
            "Open_Balance"
        },
        ReportType.Product => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SKU_ID",
            "Product_Description"
        },
        ReportType.Vendor => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Vendor_ID",
            "Vendor_Name"
        },
        ReportType.Location => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Location_ID",
            "Location_Name",
            "Location_Type"
        },
        _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    };

    public static IReadOnlySet<string> StronglyPreferred(ReportType reportType) => reportType switch
    {
        ReportType.Sales => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SKU_ID",
            "COGS",
            "Customer_ID",
            "Location_ID"
        },
        ReportType.Inventory => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Inventory_Value",
            "Average_Cost",
            "Last_Sale_Date",
            "Location_ID",
            "Vendor_ID"
        },
        ReportType.AccountsReceivable => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Due_Date",
            "Aging_Bucket"
        },
        ReportType.AccountsPayable => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Due_Date",
            "Aging_Bucket",
            "Payment_Terms"
        },
        _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    };

    public static IReadOnlyList<ReportType> MinimumPackageForV1 => new[]
    {
        ReportType.Sales,
        ReportType.Inventory,
        ReportType.AccountsReceivable,
        ReportType.AccountsPayable
    };
}

