namespace DecisionOS.Distribution.Domain.Uploads;

public static class SystemFields
{
    public static IReadOnlyList<string> For(ReportType reportType) => reportType switch
    {
        ReportType.Sales => Sales,
        ReportType.Inventory => Inventory,
        ReportType.AccountsReceivable => AccountsReceivable,
        ReportType.AccountsPayable => AccountsPayable,
        ReportType.Product => Product,
        ReportType.Customer => Customer,
        ReportType.Vendor => Vendor,
        ReportType.Purchasing => Purchasing,
        ReportType.Receiving => Receiving,
        ReportType.Location => Location,
        ReportType.Transfer => Transfer,
        ReportType.ReturnCredit => ReturnCredit,
        ReportType.FinancialStatement => FinancialStatement,
        _ => Generic
    };

    public static readonly IReadOnlyList<string> Generic =
        new[] { "Ignore" };

    public static readonly IReadOnlyList<string> Sales = new[]
    {
        "Transaction_Date",
        "Transaction_ID",
        "Customer_ID",
        "Customer_Name",
        "SKU_ID",
        "Product_Description",
        "Quantity_Sold",
        "Gross_Sales",
        "Discount_Amount",
        "Net_Sales",
        "COGS",
        "Gross_Profit",
        "Gross_Margin_Percent",
        "Location_ID",
        "Sales_Channel",
        "Sales_Rep",
        "Return_Flag",
        "Credit_Memo_Amount"
    };

    public static readonly IReadOnlyList<string> Inventory = new[]
    {
        "Snapshot_Date",
        "SKU_ID",
        "Product_Description",
        "Location_ID",
        "Quantity_On_Hand",
        "Quantity_Available",
        "Inventory_Value",
        "Average_Cost",
        "Last_Cost",
        "Retail_Price",
        "Current_Margin_Percent",
        "Last_Sale_Date",
        "Last_Receipt_Date",
        "Units_Sold_Period",
        "Sales_Dollars_Period",
        "Inventory_Turns",
        "Days_On_Hand",
        "Min_Level",
        "Max_Level",
        "Reorder_Point",
        "Vendor_ID",
        "Department",
        "Category",
        "Class",
        "Brand"
    };

    public static readonly IReadOnlyList<string> Product = new[]
    {
        "SKU_ID",
        "Product_Description",
        "UPC",
        "Vendor_ID",
        "Vendor_Name",
        "Department",
        "Category",
        "Subcategory",
        "Class",
        "Brand",
        "Unit_Of_Measure",
        "Pack_Size",
        "Current_Price",
        "Average_Cost",
        "Last_Cost",
        "MSRP",
        "Active_Flag",
        "Stocking_Flag",
        "Created_Date",
        "Discontinued_Flag"
    };

    public static readonly IReadOnlyList<string> Customer = new[]
    {
        "Customer_ID",
        "Customer_Name",
        "Customer_Type",
        "Customer_Status",
        "Billing_City",
        "Billing_State",
        "Territory",
        "Sales_Rep",
        "Payment_Terms",
        "Credit_Limit",
        "Account_Open_Date",
        "Channel"
    };

    public static readonly IReadOnlyList<string> Vendor = new[]
    {
        "Vendor_ID",
        "Vendor_Name",
        "Vendor_Status",
        "Payment_Terms",
        "Standard_Lead_Time_Days",
        "MOQ",
        "Minimum_Order_Dollars",
        "Freight_Terms",
        "Primary_Contact",
        "Vendor_Category",
        "Strategic_Flag"
    };

    public static readonly IReadOnlyList<string> Purchasing = new[]
    {
        "PO_ID",
        "PO_Date",
        "Vendor_ID",
        "Vendor_Name",
        "SKU_ID",
        "Product_Description",
        "Location_ID",
        "Ordered_Quantity",
        "Ordered_Cost",
        "Extended_PO_Cost",
        "Expected_Receipt_Date",
        "PO_Status",
        "Buyer",
        "Cancel_Date",
        "Backorder_Flag"
    };

    public static readonly IReadOnlyList<string> Receiving = new[]
    {
        "Receipt_ID",
        "Receipt_Date",
        "PO_ID",
        "Vendor_ID",
        "SKU_ID",
        "Product_Description",
        "Location_ID",
        "Ordered_Quantity",
        "Received_Quantity",
        "Backordered_Quantity",
        "Canceled_Quantity",
        "Received_Cost",
        "Packing_Slip_Number",
        "Freight_Amount"
    };

    public static readonly IReadOnlyList<string> AccountsReceivable = new[]
    {
        "AR_Snapshot_Date",
        "Customer_ID",
        "Customer_Name",
        "Invoice_ID",
        "Invoice_Date",
        "Due_Date",
        "Invoice_Amount",
        "Open_Balance",
        "Aging_Bucket",
        "Days_Past_Due",
        "Payment_Terms",
        "Collections_Status"
    };

    public static readonly IReadOnlyList<string> AccountsPayable = new[]
    {
        "AP_Snapshot_Date",
        "Vendor_ID",
        "Vendor_Name",
        "Bill_ID",
        "Bill_Date",
        "Due_Date",
        "Bill_Amount",
        "Open_Balance",
        "Aging_Bucket",
        "Days_Past_Due",
        "Payment_Terms",
        "Payment_Status",
        "Hold_Flag"
    };

    public static readonly IReadOnlyList<string> FinancialStatement = new[]
    {
        "Period_Start_Date",
        "Period_End_Date",
        "Revenue",
        "COGS",
        "Gross_Profit",
        "Operating_Expenses",
        "Operating_Income",
        "Net_Income",
        "Cash_Balance",
        "AR_Balance",
        "AP_Balance",
        "Inventory_Value",
        "Debt_Payments_Due",
        "Payroll_Due"
    };

    public static readonly IReadOnlyList<string> Location = new[]
    {
        "Location_ID",
        "Location_Name",
        "Location_Type",
        "Parent_Location_ID",
        "Active_Flag",
        "Region",
        "Store_Group",
        "Opening_Date",
        "Startup_Mode_Flag"
    };

    public static readonly IReadOnlyList<string> Transfer = new[]
    {
        "Transfer_ID",
        "Transfer_Date",
        "From_Location_ID",
        "To_Location_ID",
        "SKU_ID",
        "Product_Description",
        "Transfer_Quantity",
        "Transfer_Cost",
        "Transfer_Status"
    };

    public static readonly IReadOnlyList<string> ReturnCredit = new[]
    {
        "Return_ID",
        "Return_Date",
        "Original_Transaction_ID",
        "Customer_ID",
        "SKU_ID",
        "Location_ID",
        "Return_Quantity",
        "Return_Amount",
        "Return_Cost",
        "Reason_Code"
    };
}

