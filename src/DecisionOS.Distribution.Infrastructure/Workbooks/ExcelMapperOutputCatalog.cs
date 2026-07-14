using DecisionOS.Distribution.Domain.Uploads;

namespace DecisionOS.Distribution.Infrastructure.Workbooks;

/// <summary>Canonical output sheet names and column headers matching the simplified workbook template.</summary>
public static class ExcelMapperOutputCatalog
{
    public static string? SheetNameForKind(WorkbookSheetKind kind) => kind switch
    {
        WorkbookSheetKind.WeeklyRollup => "Weekly_Financials",
        WorkbookSheetKind.Sales => "Sales_By_SKU_Week",
        WorkbookSheetKind.AccountsReceivable => "Accounts_Receivable",
        WorkbookSheetKind.AccountsPayable => "Accounts_Payable",
        WorkbookSheetKind.Inventory => "Inventory_By_SKU",
        WorkbookSheetKind.Customer => "Customer_Master",
        WorkbookSheetKind.Vendor => "Vendor_Master",
        WorkbookSheetKind.Product => "SKU_Master",
        WorkbookSheetKind.Purchasing => "Purchase_Orders",
        WorkbookSheetKind.Holdover => "Holdover_Actions",
        _ => null
    };

    public static bool IsExportable(WorkbookSheetKind kind) =>
        SheetNameForKind(kind) is not null;

    public static IReadOnlyList<string> OutputHeadersForKind(WorkbookSheetKind kind) => kind switch
    {
        WorkbookSheetKind.WeeklyRollup =>
        [
            "Week_Number", "Week_End_Date", "Net_Sales", "COGS", "Gross_Profit", "Gross_Margin_%",
            "Orders", "Active_Customers", "Avg_Order_Value", "Inventory_Value_End",
            "Fill_Rate_%", "AR_Ending", "AR_Over_60_%", "AP_Ending", "AP_Past_Due_%",
            "Net_Profit_%", "Net_Income", "Cash_Ending", "Notes"
        ],
        WorkbookSheetKind.Sales =>
        [
            "Week_Number", "Week_End_Date", "SKU", "Category", "Customer_ID", "Units_Sold",
            "Gross_Sales", "Discount_Amount", "Net_Sales", "COGS", "Gross_Profit", "Gross_Margin_%",
            "Channel", "Order_Count"
        ],
        WorkbookSheetKind.AccountsReceivable =>
        [
            "Invoice_ID", "Customer_ID", "Customer_Name", "Invoice_Date", "Due_Date",
            "Original_Amount", "Open_Amount", "Days_Past_Due", "Aging_Bucket", "Collection_Status"
        ],
        WorkbookSheetKind.AccountsPayable =>
        [
            "Bill_ID", "Vendor_ID", "Vendor_Name", "Bill_Date", "Due_Date",
            "Original_Amount", "Open_Amount", "Days_Past_Due", "Aging_Bucket", "Payment_Status"
        ],
        WorkbookSheetKind.Inventory =>
        [
            "Snapshot_Date", "SKU", "Category", "On_Hand_Units", "Unit_Cost", "Inventory_Value",
            "Last_Sale_Date", "Quantity_On_Hand"
        ],
        WorkbookSheetKind.Customer =>
        [
            "Customer_ID", "Customer_Name", "Customer_Type", "Payment_Terms", "Credit_Limit", "Active_Flag"
        ],
        WorkbookSheetKind.Vendor =>
        [
            "Vendor_ID", "Vendor_Name", "Payment_Terms", "Lead_Time_Days", "Fill_Rate_%", "On_Time_%"
        ],
        WorkbookSheetKind.Product =>
        [
            "SKU", "Product_Description", "Category", "Vendor_ID", "Vendor_Name", "Unit_Of_Measure",
            "Average_Cost", "Current_Price", "Active_Flag"
        ],
        WorkbookSheetKind.Purchasing =>
        [
            "PO_ID", "PO_Date", "Vendor_ID", "Vendor_Name", "SKU", "Product_Description",
            "Ordered_Quantity", "Ordered_Cost", "Extended_PO_Cost", "Expected_Receipt_Date", "PO_Status"
        ],
        WorkbookSheetKind.Holdover =>
        [
            "Holdover_ID", "Customer_ID", "Customer_Name", "Area", "Action", "Owner", "Status", "Completion_%"
        ],
        _ => Array.Empty<string>()
    };

    /// <summary>Maps output column header → system field used during import.</summary>
    public static string? SystemFieldForOutputHeader(WorkbookSheetKind kind, string outputHeader) =>
        OutputToSystem.TryGetValue((kind, outputHeader), out var sys) ? sys : null;

    private static readonly Dictionary<(WorkbookSheetKind Kind, string Header), string> OutputToSystem =
        new()
        {
            // Weekly rollup
            [(WorkbookSheetKind.WeeklyRollup, "Week_End_Date")] = "Period_End_Date",
            [(WorkbookSheetKind.WeeklyRollup, "Net_Sales")] = "Net_Sales",
            [(WorkbookSheetKind.WeeklyRollup, "COGS")] = "COGS",
            [(WorkbookSheetKind.WeeklyRollup, "Gross_Profit")] = "Gross_Profit",
            [(WorkbookSheetKind.WeeklyRollup, "Gross_Margin_%")] = "Gross_Margin_Percent",
            [(WorkbookSheetKind.WeeklyRollup, "Inventory_Value_End")] = "Inventory_Value",
            [(WorkbookSheetKind.WeeklyRollup, "Fill_Rate_%")] = "Fill_Rate_Pct",
            [(WorkbookSheetKind.WeeklyRollup, "AR_Ending")] = "AR_Balance",
            [(WorkbookSheetKind.WeeklyRollup, "AR_Over_60_%")] = "AR_Over_60_Pct",
            [(WorkbookSheetKind.WeeklyRollup, "AP_Ending")] = "AP_Balance",
            [(WorkbookSheetKind.WeeklyRollup, "AP_Past_Due_%")] = "AP_Past_Due_Pct",
            [(WorkbookSheetKind.WeeklyRollup, "Net_Profit_%")] = "Net_Profit_Percent",
            [(WorkbookSheetKind.WeeklyRollup, "Net_Income")] = "Net_Income",
            [(WorkbookSheetKind.WeeklyRollup, "Cash_Ending")] = "Cash_Balance",

            // Sales
            [(WorkbookSheetKind.Sales, "Week_End_Date")] = "Transaction_Date",
            [(WorkbookSheetKind.Sales, "SKU")] = "SKU_ID",
            [(WorkbookSheetKind.Sales, "Category")] = "Category",
            [(WorkbookSheetKind.Sales, "Customer_ID")] = "Customer_ID",
            [(WorkbookSheetKind.Sales, "Units_Sold")] = "Quantity_Sold",
            [(WorkbookSheetKind.Sales, "Gross_Sales")] = "Gross_Sales",
            [(WorkbookSheetKind.Sales, "Discount_Amount")] = "Discount_Amount",
            [(WorkbookSheetKind.Sales, "Net_Sales")] = "Net_Sales",
            [(WorkbookSheetKind.Sales, "COGS")] = "COGS",
            [(WorkbookSheetKind.Sales, "Gross_Profit")] = "Gross_Profit",
            [(WorkbookSheetKind.Sales, "Gross_Margin_%")] = "Gross_Margin_Percent",
            [(WorkbookSheetKind.Sales, "Channel")] = "Sales_Channel",

            // AR
            [(WorkbookSheetKind.AccountsReceivable, "Invoice_ID")] = "Invoice_ID",
            [(WorkbookSheetKind.AccountsReceivable, "Customer_ID")] = "Customer_ID",
            [(WorkbookSheetKind.AccountsReceivable, "Customer_Name")] = "Customer_Name",
            [(WorkbookSheetKind.AccountsReceivable, "Invoice_Date")] = "Invoice_Date",
            [(WorkbookSheetKind.AccountsReceivable, "Due_Date")] = "Due_Date",
            [(WorkbookSheetKind.AccountsReceivable, "Original_Amount")] = "Invoice_Amount",
            [(WorkbookSheetKind.AccountsReceivable, "Open_Amount")] = "Open_Balance",
            [(WorkbookSheetKind.AccountsReceivable, "Days_Past_Due")] = "Days_Past_Due",
            [(WorkbookSheetKind.AccountsReceivable, "Aging_Bucket")] = "Aging_Bucket",
            [(WorkbookSheetKind.AccountsReceivable, "Collection_Status")] = "Collections_Status",

            // AP
            [(WorkbookSheetKind.AccountsPayable, "Bill_ID")] = "Bill_ID",
            [(WorkbookSheetKind.AccountsPayable, "Vendor_ID")] = "Vendor_ID",
            [(WorkbookSheetKind.AccountsPayable, "Vendor_Name")] = "Vendor_Name",
            [(WorkbookSheetKind.AccountsPayable, "Bill_Date")] = "Bill_Date",
            [(WorkbookSheetKind.AccountsPayable, "Due_Date")] = "Due_Date",
            [(WorkbookSheetKind.AccountsPayable, "Original_Amount")] = "Bill_Amount",
            [(WorkbookSheetKind.AccountsPayable, "Open_Amount")] = "Open_Balance",
            [(WorkbookSheetKind.AccountsPayable, "Days_Past_Due")] = "Days_Past_Due",
            [(WorkbookSheetKind.AccountsPayable, "Aging_Bucket")] = "Aging_Bucket",
            [(WorkbookSheetKind.AccountsPayable, "Payment_Status")] = "Payment_Status",

            // Inventory
            [(WorkbookSheetKind.Inventory, "Snapshot_Date")] = "Snapshot_Date",
            [(WorkbookSheetKind.Inventory, "SKU")] = "SKU_ID",
            [(WorkbookSheetKind.Inventory, "Category")] = "Category",
            [(WorkbookSheetKind.Inventory, "On_Hand_Units")] = "Quantity_On_Hand",
            [(WorkbookSheetKind.Inventory, "Unit_Cost")] = "Average_Cost",
            [(WorkbookSheetKind.Inventory, "Inventory_Value")] = "Inventory_Value",
            [(WorkbookSheetKind.Inventory, "Last_Sale_Date")] = "Last_Sale_Date",
            [(WorkbookSheetKind.Inventory, "Quantity_On_Hand")] = "Quantity_On_Hand",

            // Customer
            [(WorkbookSheetKind.Customer, "Customer_ID")] = "Customer_ID",
            [(WorkbookSheetKind.Customer, "Customer_Name")] = "Customer_Name",
            [(WorkbookSheetKind.Customer, "Customer_Type")] = "Customer_Type",
            [(WorkbookSheetKind.Customer, "Payment_Terms")] = "Payment_Terms",
            [(WorkbookSheetKind.Customer, "Credit_Limit")] = "Credit_Limit",
            [(WorkbookSheetKind.Customer, "Active_Flag")] = "Customer_Status",

            // Vendor
            [(WorkbookSheetKind.Vendor, "Vendor_ID")] = "Vendor_ID",
            [(WorkbookSheetKind.Vendor, "Vendor_Name")] = "Vendor_Name",
            [(WorkbookSheetKind.Vendor, "Payment_Terms")] = "Payment_Terms",
            [(WorkbookSheetKind.Vendor, "Lead_Time_Days")] = "Standard_Lead_Time_Days",
            [(WorkbookSheetKind.Vendor, "Fill_Rate_%")] = "Fill_Rate_Pct",

            // Product
            [(WorkbookSheetKind.Product, "SKU")] = "SKU_ID",
            [(WorkbookSheetKind.Product, "Product_Description")] = "Product_Description",
            [(WorkbookSheetKind.Product, "Category")] = "Category",
            [(WorkbookSheetKind.Product, "Vendor_ID")] = "Vendor_ID",
            [(WorkbookSheetKind.Product, "Vendor_Name")] = "Vendor_Name",
            [(WorkbookSheetKind.Product, "Unit_Of_Measure")] = "Unit_Of_Measure",
            [(WorkbookSheetKind.Product, "Average_Cost")] = "Average_Cost",
            [(WorkbookSheetKind.Product, "Current_Price")] = "Current_Price",
            [(WorkbookSheetKind.Product, "Active_Flag")] = "Active_Flag",

            // Purchasing
            [(WorkbookSheetKind.Purchasing, "PO_ID")] = "PO_ID",
            [(WorkbookSheetKind.Purchasing, "PO_Date")] = "PO_Date",
            [(WorkbookSheetKind.Purchasing, "Vendor_ID")] = "Vendor_ID",
            [(WorkbookSheetKind.Purchasing, "Vendor_Name")] = "Vendor_Name",
            [(WorkbookSheetKind.Purchasing, "SKU")] = "SKU_ID",
            [(WorkbookSheetKind.Purchasing, "Product_Description")] = "Product_Description",
            [(WorkbookSheetKind.Purchasing, "Ordered_Quantity")] = "Ordered_Quantity",
            [(WorkbookSheetKind.Purchasing, "Ordered_Cost")] = "Ordered_Cost",
            [(WorkbookSheetKind.Purchasing, "Extended_PO_Cost")] = "Extended_PO_Cost",
            [(WorkbookSheetKind.Purchasing, "Expected_Receipt_Date")] = "Expected_Receipt_Date",
            [(WorkbookSheetKind.Purchasing, "PO_Status")] = "PO_Status",

            // Holdover
            [(WorkbookSheetKind.Holdover, "Customer_ID")] = "Customer_ID",
            [(WorkbookSheetKind.Holdover, "Customer_Name")] = "Customer_Name",
        };
}
