Developer Question 1
“Can you explain the exact user flow step-by-step, like what should happen first and what comes next?”
Short answer
The V1 user flow should move from customer onboarding → data upload → mapping/validation → KPI scoring → driver/influencer diagnosis → priority ranking → management dashboard → action assignment → follow-up tracking.
The system should not just display reports. It should guide the user from raw data to a prioritized business decision.

---

Recommended Developer Answer
For Decision Priority OS / Decision OS–Distribution V1, the user flow should work in this order:
Step 1: Customer setup / onboarding
The internal team sets up the customer profile first.
This includes basic business information such as:
• Company name
• Business type/profile
• Distribution only, retail only, or hybrid distribution/retail
• Number of locations
• Data sources used
• Reporting cadence
• Starting operating Mode, if known
• Initial KPI library selection
• Any known data limitations
The purpose of this step is to tell the system what kind of business it is analyzing before any KPI scoring happens.

---

Step 2: Upload required data files
The user or internal team uploads the required reports.
For V1, this is a manual upload process, not a live integration.
Typical files may include:
• Sales report
• Inventory report
• Accounts receivable report
• Accounts payable report
• Vendor report
• Purchase/order data if available
• Customer/location/product detail if available
• Financial statements or accounting exports when needed
The system should allow uploads by report type, not just a generic file dump.
Example:
Upload Sales Report
Upload Inventory Report
Upload AR Aging Report
Upload AP Aging Report
Upload Vendor Report

---

Step 3: Data mapping
After upload, the system maps each uploaded file to the required system fields.
The system should identify:
• Which columns match required fields
• Which columns are missing
• Which columns need manual mapping
• Which files are usable
• Which files are incomplete
The user/internal operator should be able to confirm mappings before the system proceeds.
This is important because different customers may use different POS, accounting, ERP, or report formats.

---

Step 4: Data validation and completeness check
Before scoring, the system checks whether the uploaded data is usable.
The system should flag:
• Missing required fields
• Blank or incomplete files
• Invalid date ranges
• Duplicate records
• Unmapped SKUs, vendors, customers, or locations
• Missing cost, margin, inventory, AR, AP, or sales fields
• Data that is present but not reliable enough for full scoring
The output of this step should be a readiness status:
• Ready to Run
• Ready with Limitations
• Not Ready Yet
If the file package is incomplete, the system should not silently produce weak results. It should explain what is missing and what outputs will be limited.

---

Step 5: Run KPI calculations
Once the data is validated, the system calculates the active KPI set.
For the distribution version, the system should use the selected eligible KPI library and produce the active 7 KPI view.
Each KPI should calculate:
• Current value
• Target or threshold
• Red / Yellow / Green status
• Trend if available
• Data confidence
• Business impact where possible
The KPI layer is the first layer of the decision system, but it is not the final answer.

---

Step 6: Identify drivers under each KPI
After scoring the KPI, the system identifies which drivers are causing that KPI result.
Example:
If Gross Margin Health is yellow or red, the system should look at possible drivers such as:
• Gross margin %
• Net sales dollars
• COGS
• Discount rate
• Freight burden
• Product mix
• Vendor cost changes
The user should not only see:
Gross Margin is red.
They should see:
Gross Margin is red because discount rate increased and freight burden is reducing margin.

---

Step 7: Identify influencers beneath the drivers
The system then identifies the specific influencers that explain the driver movement.
Example:
KPI: Gross Margin Health
Driver: Discount Rate
Influencers:
• Certain customers receiving higher discounts
• Certain product categories being discounted more heavily
• Certain sales reps or channels using discounts more often
• Specific time period or promotion causing the increase
This level helps explain why the driver changed.

---

Step 8: Rank business priorities
After KPI, driver, and influencer analysis, the system should rank what matters most.
The system should not ask the user to look at seven KPIs and figure it out themselves.
It should determine:
• Which red issue matters most
• Which yellow issue needs monitoring
• Which issue has the biggest cash, profit, operational, or inventory impact
• Which action should be worked first
The priority logic should generally be:

1. Red items first
2. Highest business impact first
3. Cash/profit/inventory risk weighted heavily
4. Yellow items after red items
5. Green items last unless a negative trend warning exists
6. Gray items shown as insufficient data, not hidden

---

Step 9: Display the Management Layer dashboard
The Management Layer is the main user view.
It should show:
• The seven active KPI tiles
• Red / Yellow / Green / Gray status
• Top priority issue
• Plain-English explanation of what is wrong
• The likely cause
• Recommended action
• Owner
• Due date
• Follow-up status
• Holdover items from prior weeks
The Management Layer should be simple enough for an owner, CEO, president, or GM to understand quickly.
This is the “what matters now” screen.

---

Step 10: Allow drill-down into details
From the Management Layer, the user should be able to click into a KPI, driver, or priority issue.
The Drill-Down Layer should explain:
• What calculation produced the status
• Which drivers moved the KPI
• Which influencers are behind the driver
• What data was used
• What data confidence level applies
• What trend is visible
• Why the system made the recommendation
This layer is for validation, explanation, and trust.

---

Step 11: Recommend the action
For each priority issue, the system should recommend a specific action.
The recommendation should include:
• What is wrong
• Why it matters
• What action to take
• Who should own it
• When it should be done
• What result is expected
• When it should be reviewed again
The system should avoid generic advice like:
Review inventory.
Instead, it should say something closer to:
Reduce reorder exposure on slow-moving inventory group X. Purchasing should review these SKUs before the next buying cycle and pause or reduce replenishment until sell-through improves.

---

Step 12: Assign owner and due date
The user should be able to assign the action to a responsible person.
Each action should include:
• Owner
• Due date
• Status
• Priority level
• Follow-up date
• Notes
• Completion condition
Suggested statuses:
• Not Started
• In Progress
• At Risk
• Completed
• Deferred
• Blocked
This turns the system from a dashboard into an execution tool.

---

Step 13: Track holdover actions
If an action is not completed in the current week, it should carry forward as a holdover item.
The system should show:
• Original issue
• Assigned owner
• Original due date
• Current status
• Days open
• Progress notes
• Whether the issue is improving or getting worse
Completed items can remain visible briefly, then drop off after a short period.

---

Step 14: Next-cycle refresh
On the next upload/reporting cycle, the system should compare the new data to the prior cycle.
It should answer:
• Did the KPI improve?
• Did the driver improve?
• Did the action reduce the issue?
• Is the item still red/yellow?
• Should the action continue, change, escalate, or close?
• Are there new higher-priority issues?
This creates the weekly Decision Priority OS cycle:
Measure → Diagnose → Prioritize → Act → Track → Recheck

---

Plain-English version
The flow should feel like this:

1. Set up the customer.
2. Upload the customer’s reports.
3. Map the uploaded fields.
4. Check whether the data is complete enough.
5. Run the KPI calculations.
6. Identify which KPIs are red, yellow, green, or gray.
7. Find the drivers causing the KPI result.
8. Find the influencers causing the driver movement.
9. Rank the most important business issues.
10. Show the owner a simple Management Layer dashboard.
11. Let the user drill down for the details.
12. Recommend the action.
13. Assign an owner and due date.
14. Track the action until it is fixed.
15. Refresh next cycle and decide whether to close, continue, escalate, or adjust.

---

Important rule
The system should not be designed as a passive reporting dashboard.
It should be designed as a decision and action flow.
The user should move through this path:
Data → KPI status → Driver → Influencer → Priority → Action → Owner → Follow-up → Result
That is the core user flow.

Developer Question 2
“For the linkup/integration part, what exactly needs to be connected and how should it work?”
Short answer
For V1, the system does not need full live integration. It needs a structured manual upload / file import process that can later become automated.
The linkup/integration design should connect the customer’s core business systems to the Decision Priority OS data model:
Accounting + POS/ERP + Inventory + Purchasing/Vendor + AR/AP + Location/Customer/Product data
V1 should prove the logic works first. V2 can automate the connections.

---

Recommended Developer Answer
For Decision Priority OS / Decision OS–Distribution V1, the integration should be handled in two stages:
Stage 1: V1 manual upload linkup
In V1, “integration” means the customer or internal operator uploads reports exported from their existing systems.
The system should accept files from sources such as:
• POS system
• ERP system
• Accounting system
• Inventory system
• Purchasing system
• Vendor reports
• AR/AP reports
• Sales reports
• Customer/product/location reports
The system should not require direct API connections yet.
The V1 flow should be:
Customer system export → file upload → field mapping → validation → calculation engine → Management Layer output

---

Stage 2: Future automated integration
Later, in V1.5 or V2, those same reports/fields can be pulled automatically through API connections, scheduled exports, database connections, or system connectors.
The important thing is that the V1 data model should be built as if automation will come later.
That means the developers should not hard-code around one report format. They should build around required data fields and mapping rules.

---

What needs to be connected

1. Accounting system
   The accounting system is needed for the financial KPIs.
   Possible systems:
   • QuickBooks Online
   • QuickBooks Desktop
   • NetSuite
   • Sage
   • Xero
   • Other accounting platforms
   Data needed may include:
   • Profit and loss data
   • Balance sheet data
   • Cash balances
   • Accounts receivable
   • Accounts payable
   • Vendor payment terms
   • Customer payment behavior
   • COGS
   • Gross profit
   • Expenses
   • Net income or operating income
   This supports KPIs such as:
   • Cash Runway
   • AR Aging Risk
   • Gross Margin Health
   • Operating Margin / Net Profitability
   • AP pressure
   • cash availability and financial risk

---

2. POS or ERP system
   The POS/ERP system is usually the main operating data source.
   Possible systems:
   • Keystroke POS
   • AIMS
   • Lightspeed
   • Epicor
   • NetSuite
   • Shopify
   • Square
   • Clover
   • Custom ERP/POS systems
   Data needed may include:
   • Sales history
   • Invoice history
   • Customer sales
   • SKU/item sales
   • Category/department sales
   • Location-level sales
   • Quantity sold
   • Net sales
   • Gross sales if available
   • Discounts
   • Returns/credits
   • COGS/item cost
   • Gross margin
   • Inventory on hand
   • Inventory value
   • Transfers
   • Purchase orders
   • Receipts
   • Vendor/item relationships
   This supports:
   • Inventory Health
   • Gross Margin Health
   • Dead/slow-moving inventory
   • Sales trend
   • SKU/category/customer analysis
   • Vendor impact
   • Location-level analysis
   • drill-down diagnosis

---

3. Inventory system
   If inventory is managed inside the POS/ERP, this may be the same source. If inventory is separate, it needs to be connected or uploaded separately.
   Data needed may include:
   • SKU/item number
   • Description
   • Category/department/class
   • Vendor
   • Quantity on hand
   • Inventory value
   • Average cost
   • Last cost
   • Last sale date
   • Last receipt date
   • Sales velocity
   • Turns
   • Days on hand
   • Reorder settings if available
   • Min/Max if available
   • Location/node inventory if multi-location
   This supports:
   • Inventory Health
   • Dead/slow-moving inventory
   • overstock exposure
   • stockout risk
   • replenishment settings review
   • SKU classification
   • node/location inventory decisions

---

4. Purchasing and vendor data
   Purchasing and vendor data is needed to understand vendor behavior, inventory replenishment, and cash/inventory pressure.
   Data needed may include:
   • Vendor master list
   • Vendor terms
   • Lead time
   • Purchase orders
   • PO dates
   • Ordered quantity
   • Received quantity
   • Canceled quantity
   • Backordered quantity
   • Expected receipt date
   • Actual receipt date
   • Vendor cost
   • Freight if available
   • Fill rate if available
   • MOQ if available
   • Vendor-item relationship
   This supports:
   • Vendor Efficiency
   • inventory purchasing decisions
   • replenishment burden
   • cash tied up in inventory
   • PO timing
   • vendor performance
   • purchase planning

---

5. Accounts receivable data
   AR data is needed because Decision Priority OS includes financial and cash decision logic, not just operational reporting.
   Data needed may include:
   • Customer balance
   • Invoice date
   • Due date
   • Aging bucket
   • Amount current
   • Amount 30/60/90+ days
   • Payment history
   • Customer credit terms
   • Open invoices
   • Collections status if available
   This supports:
   • AR Aging Risk
   • Cash Runway
   • customer payment risk
   • collection priority
   • cash forecast pressure

---

6. Accounts payable data
   AP data is needed to understand near-term cash obligations.
   Data needed may include:
   • Vendor balance
   • Bill date
   • Due date
   • Aging bucket
   • Amount current
   • Amount 30/60/90+ days
   • Payment terms
   • Scheduled payments if available
   • Held invoices or disputed invoices if available
   This supports:
   • Cash Runway
   • AP pressure
   • vendor payment planning
   • cash timing
   • short-term financial risk

---

7. Customer data
   Customer data helps explain revenue quality, AR risk, margin problems, and customer concentration.
   Data needed may include:
   • Customer ID
   • Customer name
   • Customer type
   • Customer location or territory
   • Sales history
   • Gross margin by customer
   • Discount behavior
   • Payment terms
   • Payment history
   • Returns/credits
   • Channel if available
   • Assigned sales rep if available
   This supports:
   • AR Aging Risk
   • Gross Margin Health
   • customer concentration
   • customer profitability
   • revenue quality
   • drill-down explanations

---

8. Product / SKU data
   Product data is required for inventory, margin, purchasing, and vendor analysis.
   Data needed may include:
   • SKU/item number
   • Description
   • Category
   • Department
   • Class
   • Brand
   • Vendor
   • Cost
   • Price
   • Margin
   • Quantity sold
   • Quantity on hand
   • Inventory value
   • Sales velocity
   • Last sale date
   • Last receipt date
   • Reorder settings if available
   This supports:
   • Inventory Health
   • Gross Margin Health
   • Dead/slow-moving inventory
   • SKU classification
   • product/category profitability
   • replenishment settings
   • item-level drill-downs

---

9. Location / node data
   For distribution, retail, or hybrid customers, the system must understand locations and nodes.
   Data needed may include:
   • Location ID
   • Location name
   • Location type
   • DC, warehouse, store, ecommerce, outside sales, etc.
   • Parent/child relationship
   • Transfer path
   • Inventory by location
   • Sales by location
   • Receipts by location
   • Transfers between locations
   This supports:
   • multi-location reporting
   • DC vs. store logic
   • store-level KPI rollups
   • node-level inventory decisions
   • hybrid distribution/retail analysis
   Important: a DC that supplies stores should not be treated the same as a store. Store sales and DC replenishment burden are related but not identical.

---

How the linkup should work technically
Step 1: Customer selects source system or upload type
The system should ask what kind of source the customer is using:
• Accounting
• POS
• ERP
• Inventory
• Purchasing
• AR/AP
• Other
For V1, this can be done through upload categories.
Example:
Upload Accounting Export
Upload POS Sales Export
Upload Inventory Export
Upload AR Aging Export
Upload AP Aging Export
Upload Vendor/Purchasing Export

---

Step 2: Customer uploads files
Accepted file types should include:
• Excel
• CSV
• possibly PDF only for reference, not preferred for calculations
Best format for calculations should be Excel or CSV.
PDFs should not be the preferred data source because they are harder to map reliably.

---

Step 3: System identifies file type
The system should identify whether the file is:
• Sales
• Inventory
• AR
• AP
• Vendor
• Purchasing
• Customer
• Product/SKU
• Location
• Financial statement
• Unknown / needs manual classification
If the system cannot identify the file, the user should be able to manually label it.

---

Step 4: System maps columns to required fields
The system should map customer column names to system field names.
Example:
Customer file column:
Item No.
Product ID
SKU
Stock Number
All may map to:
SKU_ID
Customer file column:
Qty on Hand
QOH
Units Available
All may map to:
Quantity_On_Hand
The system needs a mapping layer because different customers and POS systems use different labels.

---

Step 5: System validates required fields
The system should check whether the required fields are present.
Examples:
For sales analysis:
• Date
• SKU/item
• Quantity sold
• Sales dollars
• Cost or gross margin
• Location if multi-location
For inventory analysis:
• SKU/item
• Quantity on hand
• Inventory value or cost
• Location if multi-location
• Vendor if available
• Last sale date or sales history if available
For AR:
• Customer
• Invoice amount
• Due date or aging bucket
• Open balance
For AP:
• Vendor
• Bill amount
• Due date or aging bucket
• Open balance

---

Step 6: System gives readiness status
After mapping and validation, each file and the full customer package should receive a readiness status:
• Ready to Run
• Ready with Limitations
• Not Ready Yet
Example:
Inventory file is Ready with Limitations because quantity on hand and item cost are present, but last sale date is missing. Dead stock scoring may be limited unless sales history is also uploaded.

---

Step 7: System normalizes data
The system should standardize the data before calculations.
Examples:
• Standard date format
• Standard SKU IDs
• Standard vendor IDs
• Standard customer IDs
• Standard location IDs
• Numeric fields cleaned
• Duplicate rows handled
• negative sales/returns separated if needed
• category/department naming standardized
This creates a common data model across different customer systems.

---

Step 8: System stores mapped source and original source
The system should keep both:

1. The original uploaded file/source reference
2. The normalized mapped data used for calculations
   This matters for auditability and troubleshooting.
   The user/internal team should be able to trace:
   This KPI result came from this source file, this date range, and these mapped fields.

---

Step 9: Calculation engine runs from normalized data
The KPI and decision engine should run from the normalized internal data model, not directly from raw customer files.
That makes the system more scalable.
The logic should be:
Raw customer file → mapped field layer → normalized data model → KPI/driver/influencer calculations → decision output

---

Step 10: Integration results feed the Management Layer
The final output should not simply say the upload succeeded.
It should feed:
• KPI results
• driver results
• influencer findings
• RYG status
• data confidence
• priority ranking
• recommended actions
• holdover tracking
• drill-down explanation

---

Important V1 vs. V2 distinction
V1 should do this:
• Manual uploads
• File classification
• Field mapping
• Validation
• Normalization
• KPI calculations
• dashboard output
• action tracking
V1 does not need to do this yet:
• Live API integrations
• automatic daily syncing
• writeback to customer systems
• real-time POS connection
• automatic payment updates
• full ERP integration
• automatic vendor communication
Those can come later.

---

Important design rule
The developers should build the linkup around a source-agnostic data model.
That means the system should not be built only for one POS, one accounting system, or one spreadsheet format.
Instead, it should work like this:
Any customer source → mapped to standard required fields → normalized into Decision Priority OS logic
That allows the same system to work with QuickBooks, Keystroke, AIMS, ERP exports, CSV files, and future API connections.

---

Plain-English answer
The system needs to connect to the customer’s business data, but for V1 that connection can happen through report uploads.
The main sources are:

1. Accounting system
2. POS or ERP system
3. Inventory data
4. Purchasing/vendor data
5. AR data
6. AP data
7. Customer data
8. Product/SKU data
9. Location/node data
   The flow should be:
   Upload reports → identify report type → map fields → validate data → normalize data → run calculations → produce dashboard/actions
   The goal is to make V1 upload-based but build it in a way that can later be automated through direct integrations.

---

Rule to lock in
For V1, integration should mean:
Manual-upload connection into a standardized Decision Priority OS data model.
For V2, integration can mean:
Automated API / connector sync into the same standardized data model.
The calculation engine should not care whether data came from a manual upload or automated connection.

Developer Question 3
“In the CSV/Excel import, what fields or mapping options should users be able to choose?”
Short answer
Users should be able to map uploaded CSV/Excel columns into a standard Decision Priority OS field structure.
The import should not require every customer to use the same column names. The system needs mapping options for:
Date fields, customer fields, SKU/product fields, sales fields, cost/margin fields, inventory fields, vendor fields, purchase/receiving fields, AR fields, AP fields, location fields, and optional classification fields.
The import should be flexible enough for different POS, ERP, accounting, and inventory systems.

---

Recommended Developer Answer
For the CSV/Excel import, users should be able to map their uploaded columns to standard system fields.
The system should present mapping options by report type.
The main import categories should be:

1. Sales
2. Inventory
3. Product/SKU master
4. Customer master
5. Vendor master
6. Purchase orders / purchasing
7. Receiving
8. Accounts receivable
9. Accounts payable
10. Financial statements / accounting summary
11. Location / store / warehouse structure
12. Optional adjustment / transfer / return data

---

1. Sales import mapping fields
   Sales data is used for revenue, margin, customer, product, inventory movement, trend, and drill-down analysis.
   Required or strongly preferred fields
   System Field What it Means Required Level
   Transaction_Date Invoice, order, or sale date Required
   Transaction_ID Invoice/order/receipt number Strongly preferred
   Customer_ID Customer account number or ID Required if customer analysis is used
   Customer_Name Customer name Strongly preferred
   SKU_ID Item/SKU/product number Required for SKU/product analysis
   Product_Description Item description Strongly preferred
   Quantity_Sold Units sold Required
   Gross_Sales Sales before discounts/returns Strongly preferred
   Discount_Amount Discount dollars Strongly preferred
   Net_Sales Sales after discounts/returns Required
   COGS Cost of goods sold Required for margin scoring
   Gross_Profit Net sales minus COGS Strongly preferred
   Gross_Margin_Percent Gross profit percentage Optional if system calculates it
   Location_ID Store/DC/location ID Required for multi-location
   Sales_Channel Inside sales, outside sales, ecommerce, retail, wholesale, etc. Optional but useful
   Sales_Rep Salesperson/rep Optional
   Return_Flag Indicates return/credit transaction Optional
   Credit_Memo_Amount Return or credit amount Optional
   Important mapping rule
   The system should allow customers to provide either:
   • Gross sales, discounts, net sales, and COGS, or
   • Net sales and gross profit, or
   • Net sales and gross margin percentage
   But the strongest version should capture enough information to calculate margin independently.

---

2. Inventory import mapping fields
   Inventory data is used for inventory health, slow/dead inventory, cash tied up in stock, SKU exposure, and replenishment analysis.
   Required or strongly preferred fields
   System Field What it Means Required Level
   Snapshot_Date Date inventory report was pulled Required
   SKU_ID Item/SKU/product number Required
   Product_Description Item description Strongly preferred
   Location_ID Store/DC/warehouse/location Required for multi-location
   Quantity_On_Hand Units currently on hand Required
   Quantity_Available Available units excluding committed/reserved Optional
   Inventory_Value Total inventory value Required or system calculated
   Average_Cost Average cost per unit Strongly preferred
   Last_Cost Most recent cost Optional
   Retail_Price Current selling price Optional
   Current_Margin_Percent Current margin percentage Optional
   Last_Sale_Date Last date sold Strongly preferred
   Last_Receipt_Date Last date received Strongly preferred
   Units_Sold_Period Units sold in selected period Strongly preferred
   Sales_Dollars_Period Sales dollars in selected period Strongly preferred
   Inventory_Turns Turns, if provided Optional
   Days_On_Hand Days on hand, if provided Optional
   Min_Level Current min setting Optional
   Max_Level Current max setting Optional
   Reorder_Point Current reorder point Optional
   Vendor_ID Primary vendor Strongly preferred
   Department Department/category grouping Strongly preferred
   Category Category grouping Strongly preferred
   Class Product class Optional
   Brand Product brand Optional
   Important mapping rule
   If the customer cannot provide historical inventory snapshots, the system should still accept the current inventory file, but confidence should be lower for trend-based inventory scoring.

---

3. Product / SKU master mapping fields
   The product master helps standardize item identity across sales, inventory, vendor, and purchasing files.
   System Field What it Means Required Level
   SKU_ID Item/SKU/product number Required
   Product_Description Item description Required
   UPC UPC/barcode Optional
   Vendor_ID Primary vendor Strongly preferred
   Vendor_Name Vendor name Strongly preferred
   Department Department Strongly preferred
   Category Category Strongly preferred
   Subcategory Subcategory Optional
   Class Product class Optional
   Brand Brand Optional
   Unit_Of_Measure Each, case, box, etc. Optional
   Pack_Size Units per pack/case Optional
   Current_Price Current sell price Strongly preferred
   Average_Cost Average cost Strongly preferred
   Last_Cost Last cost Optional
   MSRP Manufacturer suggested price Optional
   Active_Flag Active/inactive item Strongly preferred
   Stocking_Flag Stocked/non-stocked/special order Optional
   Created_Date Item creation date Optional
   Discontinued_Flag Discontinued item Optional

---

4. Customer master mapping fields
   Customer data supports AR risk, customer concentration, customer profitability, and sales/margin drill-downs.
   System Field What it Means Required Level
   Customer_ID Customer account number or ID Required
   Customer_Name Customer name Required
   Customer_Type Retail, wholesale, dealer, contractor, etc. Optional
   Customer_Status Active/inactive/hold Optional
   Billing_City City Optional
   Billing_State State Optional
   Territory Sales territory Optional
   Sales_Rep Assigned salesperson Optional
   Payment_Terms Net 15, Net 30, COD, etc. Strongly preferred
   Credit_Limit Customer credit limit Optional
   Account_Open_Date Start date Optional
   Channel Retail, ecommerce, wholesale, outside sales, etc. Optional

---

5. Vendor master mapping fields
   Vendor data supports vendor efficiency, purchasing logic, lead time, payment terms, and inventory decision analysis.
   System Field What it Means Required Level
   Vendor_ID Vendor number or ID Required
   Vendor_Name Vendor name Required
   Vendor_Status Active/inactive Optional
   Payment_Terms Vendor terms Strongly preferred
   Standard_Lead_Time_Days Vendor lead time Strongly preferred
   MOQ Minimum order quantity Optional
   Minimum_Order_Dollars Minimum order dollars Optional
   Freight_Terms Prepaid, collect, free freight threshold, etc. Optional
   Primary_Contact Vendor contact Optional
   Vendor_Category Vendor type/category Optional
   Strategic_Flag Strategic vendor flag Optional

---

6. Purchase order / purchasing mapping fields
   Purchasing data supports vendor efficiency, inventory pressure, PO analysis, cash planning, and replenishment decisions.
   System Field What it Means Required Level
   PO_ID Purchase order number Required
   PO_Date Date PO was created Required
   Vendor_ID Vendor ID Required
   Vendor_Name Vendor name Strongly preferred
   SKU_ID Item/SKU/product number Required for item-level PO analysis
   Product_Description Item description Strongly preferred
   Location_ID Receiving location Required for multi-location
   Ordered_Quantity Quantity ordered Required
   Ordered_Cost Unit cost on PO Strongly preferred
   Extended_PO_Cost Total cost of PO line Strongly preferred
   Expected_Receipt_Date Expected receipt date Strongly preferred
   PO_Status Open, closed, canceled, partial, etc. Strongly preferred
   Buyer Person who created/order owner Optional
   Cancel_Date Cancel date if applicable Optional
   Backorder_Flag Backorder indicator Optional

---

7. Receiving mapping fields
   Receiving data is important because PO data alone only shows what was ordered. Receiving shows what actually arrived.
   System Field What it Means Required Level
   Receipt_ID Receiver number Required
   Receipt_Date Date received Required
   PO_ID Related PO number Strongly preferred
   Vendor_ID Vendor ID Strongly preferred
   SKU_ID Item/SKU/product number Required
   Product_Description Item description Strongly preferred
   Location_ID Receiving location Required for multi-location
   Ordered_Quantity Quantity ordered Strongly preferred
   Received_Quantity Quantity physically received Required
   Backordered_Quantity Quantity not yet received Optional
   Canceled_Quantity Quantity canceled Optional
   Received_Cost Cost on received goods Strongly preferred
   Packing_Slip_Number Vendor packing slip number Optional
   Freight_Amount Freight cost if available Optional

---

8. Accounts receivable mapping fields
   AR data supports cash risk, collection priority, customer payment behavior, and Cash Runway.
   System Field What it Means Required Level
   AR_Snapshot_Date Date AR report was pulled Required
   Customer_ID Customer ID Required
   Customer_Name Customer name Required
   Invoice_ID Invoice number Strongly preferred
   Invoice_Date Invoice date Strongly preferred
   Due_Date Due date Strongly preferred
   Invoice_Amount Original invoice amount Strongly preferred
   Open_Balance Unpaid balance Required
   Aging_Bucket Current, 1–30, 31–60, 61–90, 90+ Required if due date unavailable
   Days_Past_Due Days past due Optional if aging bucket available
   Payment_Terms Customer payment terms Strongly preferred
   Collections_Status Current collection status Optional
   Important mapping rule
   The system should support either:
   • Due date and open balance, or
   • Aging bucket and open balance.
   Due date is better because the system can calculate the aging bucket itself.

---

9. Accounts payable mapping fields
   AP data supports cash pressure, payment timing, vendor obligations, and Cash Runway.
   System Field What it Means Required Level
   AP_Snapshot_Date Date AP report was pulled Required
   Vendor_ID Vendor ID Required
   Vendor_Name Vendor name Required
   Bill_ID Vendor bill/invoice number Strongly preferred
   Bill_Date Bill date Strongly preferred
   Due_Date Due date Strongly preferred
   Bill_Amount Original bill amount Strongly preferred
   Open_Balance Unpaid balance Required
   Aging_Bucket Current, 1–30, 31–60, 61–90, 90+ Required if due date unavailable
   Days_Past_Due Days past due Optional if aging bucket available
   Payment_Terms Vendor payment terms Strongly preferred
   Payment_Status Open, held, scheduled, paid, disputed Optional
   Hold_Flag Do not pay / held invoice indicator Optional

---

10. Financial statement / accounting summary mapping fields
    This supports higher-level financial KPIs.
    System Field What it Means Required Level
    Period_Start_Date Beginning of reporting period Required
    Period_End_Date End of reporting period Required
    Revenue Total revenue/net sales Required
    COGS Cost of goods sold Required
    Gross_Profit Revenue minus COGS Strongly preferred
    Operating_Expenses Operating expenses Strongly preferred
    Operating_Income Operating profit Strongly preferred
    Net_Income Net profit Strongly preferred
    Cash_Balance Current cash Required for Cash Runway
    AR_Balance Total accounts receivable Strongly preferred
    AP_Balance Total accounts payable Strongly preferred
    Inventory_Value Total inventory value Strongly preferred
    Debt_Payments_Due Required debt payments Optional
    Payroll_Due Payroll obligation if available Optional

---

11. Location / node mapping fields
    This is required for multi-location distribution, retail, warehouse, DC, or hybrid businesses.
    System Field What it Means Required Level
    Location_ID Location number or code Required
    Location_Name Location name Required
    Location_Type DC, warehouse, store, ecommerce, outside sales, etc. Required
    Parent_Location_ID Parent/feeder location if applicable Strongly preferred
    Active_Flag Active/inactive location Optional
    Region Region/group Optional
    Store_Group A/B/C store group or volume group Optional
    Opening_Date Store/location opening date Optional
    Startup_Mode_Flag New location/default mode flag Optional

---

12. Transfer mapping fields
    For hybrid distribution/retail businesses, transfers matter because DC-to-store transfers create replenishment burden on the DC.
    System Field What it Means Required Level
    Transfer_ID Transfer document number Required
    Transfer_Date Date of transfer Required
    From_Location_ID Source location Required
    To_Location_ID Destination location Required
    SKU_ID Item/SKU/product number Required
    Product_Description Item description Strongly preferred
    Transfer_Quantity Quantity transferred Required
    Transfer_Cost Cost basis if available Optional
    Transfer_Status Open, completed, canceled, partial Optional
    Important mapping rule
    Transfers should not be confused with outside customer sales.
    For DC logic, transfers may count as DC replenishment burden, but they are not the same as retail sales.

---

13. Return / credit mapping fields
    Returns and credits can distort sales, margin, inventory, and customer analysis if they are not separated.
    System Field What it Means Required Level
    Return_ID Return/credit memo number Strongly preferred
    Return_Date Date of return/credit Required
    Original_Transaction_ID Original invoice/order if available Optional
    Customer_ID Customer ID Strongly preferred
    SKU_ID Item/SKU/product number Strongly preferred
    Location_ID Location Required for multi-location
    Return_Quantity Units returned Strongly preferred
    Return_Amount Return/credit dollars Required
    Return_Cost Cost of returned goods Optional
    Reason_Code Reason for return Optional

---

14. Optional classification / setup mapping fields
    These fields are not always required, but they make the system stronger.
    System Field What it Means
    Business_Unit
    Department
    Category
    Subcategory
    Class
    Brand
    Vendor_Category
    Customer_Type
    Sales_Channel
    Territory
    Sales_Rep
    Buyer
    ABC_Class
    Stocking_Status
    Discontinued_Flag
    Strategic_Flag
    Seasonal_Flag
    Promotion_Flag
    New_Item_Flag
    Special_Order_Flag
    These fields improve drill-down, segmentation, and action recommendations.

---

Mapping options users should have
The import screen should let users do the following:

1. Choose report type
   The user should be able to label each uploaded file as:
   • Sales
   • Inventory
   • Product/SKU master
   • Customer master
   • Vendor master
   • Purchase order
   • Receiving
   • AR aging
   • AP aging
   • Financial statement
   • Location master
   • Transfer file
   • Return/credit file
   • Other

---

2. Choose header row
   Some Excel reports do not start on row 1.
   The user should be able to select:
   “My column headers are on row \_\_\_.”

---

3. Ignore extra rows
   The system should allow users to skip:
   • Report title rows
   • Blank rows
   • Totals rows
   • Subtotal rows
   • Notes at bottom of report

---

4. Map source column to system field
   Example:
   Customer Column Maps To
   Item # SKU_ID
   Qty OH Quantity_On_Hand
   Avg Cost Average_Cost
   Ext Cost Inventory_Value
   Dept Department

---

5. Mark unmapped fields as ignored
   Not every source column will matter.
   The user should be able to mark a column as:
   Ignore / Do Not Import

---

6. Apply saved mapping template
   Once a customer’s file format has been mapped, the system should save the mapping template.
   Example:
   Keystroke Inventory Export — Customer A
   QuickBooks AR Aging Export — Customer A
   AIMS Sales Export — Customer B
   Next time, the user should be able to reuse the mapping.

---

7. Identify field type
   The system should understand:
   • Date
   • Text
   • Number
   • Currency
   • Percentage
   • Boolean / yes-no
   • Category
   • ID field

---

8. Choose date range
   The user should confirm the report period:
   • Start date
   • End date
   • Snapshot date
   • Fiscal period if applicable
   This matters because some files are activity reports and some are point-in-time snapshots.

---

9. Choose location handling
   For multi-location customers, the user should be able to specify whether the file is:
   • One location only
   • All locations combined
   • All locations separated by location column
   • DC only
   • Store only
   • Warehouse only
   • Ecommerce only

---

10. Choose whether values are positive or negative
    Returns, credits, and adjustments vary by system.
    The system should allow rules such as:
    • Returns shown as negative sales
    • Returns shown as positive in a separate return column
    • Credits shown as separate credit memo rows
    • Discounts shown as negative dollars
    • Discounts shown as positive discount amount

---

11. Choose cost basis
    If multiple cost fields exist, the user/internal operator should choose which one to use:
    • Average cost
    • Last cost
    • Standard cost
    • Replacement cost
    • Landed cost
    • COGS from transaction
    • Vendor cost
    The strongest V1 default should usually be:
    Use transaction-level COGS when available for sales margin.
    Use average cost or current inventory cost for inventory value when transaction COGS is not available.

---

12. Flag required missing fields
    The system should immediately show:
    • Required fields mapped
    • Strongly preferred fields mapped
    • Optional fields mapped
    • Missing fields
    • Impact of missing fields
    Example:
    Missing COGS. Gross Margin Health can still show revenue trend, but margin scoring will be limited.

---

Required field logic by report type
Minimum to run a basic V1 distribution analysis
At minimum, the system should try to capture:
Sales
• Transaction date
• SKU or product identifier
• Quantity sold
• Net sales
• COGS or gross profit
• Customer if available
• Location if multi-location
Inventory
• Snapshot date
• SKU
• Quantity on hand
• Inventory value or cost
• Location if multi-location
• Last sale date or sales history if available
AR
• Customer
• Open balance
• Due date or aging bucket
AP
• Vendor
• Open balance
• Due date or aging bucket
Product/SKU
• SKU
• Description
• Category/department
• Vendor if available
Vendor
• Vendor ID/name
• Payment terms
• Lead time if available
Location
• Location ID/name/type if multi-location

---

Field importance levels
Developers should classify fields into three levels:
Required
Without these, a report type cannot be used properly.
Example:
• SKU_ID for item-level inventory
• Open_Balance for AR/AP
• Transaction_Date for sales
Strongly preferred
The system can run without them, but scoring quality or drill-down quality is reduced.
Example:
• Vendor_ID
• Department
• Gross_Profit
• Last_Sale_Date
• Sales_Channel
Optional
These fields improve explanation, segmentation, or future features.
Example:
• Brand
• Sales rep
• territory
• promotion flag
• reason code

---

Data-confidence rule
Missing fields should not always block the entire upload.
Instead, missing fields should affect data confidence.
The system should assign:
• High Confidence — required and strongly preferred fields are mostly present
• Medium Confidence — required fields are present, but some strong supporting fields are missing
• Low Confidence — required fields are missing or data quality is weak
If required fields are missing for a specific KPI, the KPI should be gray or limited instead of giving a false result.

---

Recommended import workflow
The CSV/Excel import should follow this sequence:

1. Upload file
2. Select report type
3. Select header row
4. Preview data
5. Map source columns to system fields
6. Identify ignored columns
7. Confirm date range or snapshot date
8. Confirm location handling
9. Validate required fields
10. Show missing-field impact
11. Save mapping template
12. Run import
13. Show readiness status
14. Feed normalized data into KPI/driver/influencer logic

---

Plain-English answer
The user should not have to rename their spreadsheet columns manually before uploading.
The system should let them upload a CSV or Excel file, choose what kind of file it is, then map each column to a standard Decision Priority OS field.
The most important mapping groups are:
• Date
• Customer
• SKU/product
• Sales
• Cost/margin
• Inventory
• Vendor
• Purchase order
• Receiving
• AR
• AP
• Location
• Returns/credits
• Transfers
• Department/category/classification
The system should save those mappings so the customer does not have to remap the same report every week.

---

Rule to lock in
For imports, the system should be built around this structure:
Customer column name → mapped system field → normalized data model → KPI/driver/influencer calculation → Management Layer/action output
The import tool should be flexible, reusable, and confidence-aware. It should support imperfect data, but it should clearly show what is missing and how missing fields affect the output.

Developer Question 4
“How should duplicate or invalid data be handled during the import process?”
Short answer
Duplicate or invalid data should not be silently imported. The import process should detect it, classify it, show the user/internal operator what was found, and either:

1. Auto-correct safe issues
2. Flag questionable issues for review
3. Block critical issues from import or scoring
   The system should protect the integrity of the KPI, driver, influencer, priority, and action logic.

---

Recommended Developer Answer
For Decision Priority OS / Decision OS–Distribution V1, duplicate and invalid data should be handled through a structured import validation and exception process.
The system should not assume that every uploaded CSV or Excel file is clean.
The import process should follow this flow:
Upload → Map → Validate → Detect duplicates/errors → Classify severity → Auto-fix safe issues → Hold review items → Block critical issues → Import clean records → Report exceptions → Adjust confidence

---

1. Duplicate data handling
   Duplicates should be detected based on the report type. The system should not use only one universal duplicate rule because duplicate logic is different for sales, inventory, AR, AP, purchasing, receiving, transfers, and returns.
   A. Exact duplicate rows
   An exact duplicate means every important field is identical.
   Example:
   Transaction_ID Date SKU Qty Net Sales
   INV-1001 4/15/2026 SKU-123 2 200
   INV-1001 4/15/2026 SKU-123 2 200
   Recommended handling
   The system should flag exact duplicates and allow the operator to choose:
   • Keep first record
   • Exclude duplicate record
   • Review manually
   For most cases, the default should be:
   Keep one record and exclude the exact duplicate from calculations.
   The excluded duplicate should remain visible in an exception log.

---

B. Duplicate transaction IDs with different detail
Sometimes the same invoice, PO, receipt, bill, or transfer appears on multiple lines because each line is a different SKU.
Example:
Invoice_ID SKU Qty Net Sales
INV-1001 SKU-123 2 200
INV-1001 SKU-456 1 125
Recommended handling
This is not a duplicate if the line-level detail is different.
The system should treat it as a valid multi-line transaction.
The duplicate check should use a line-level key, not just invoice number.
Suggested transaction-line duplicate key:
Transaction_ID + SKU_ID + Location_ID + Quantity + Net_Sales + COGS

---

C. Duplicate transaction IDs with same SKU but different quantities or dollars
Example:
Invoice_ID SKU Qty Net Sales
INV-1001 SKU-123 2 200
INV-1001 SKU-123 3 300
Recommended handling
This should be flagged as a possible duplicate or adjustment issue, not auto-deleted.
Possible causes:
• split shipment
• partial invoice
• correction
• duplicate export
• return/credit
• revised transaction
• same SKU sold twice on same invoice
Default handling:
Import only if there is a unique line ID or enough evidence that both rows are valid. Otherwise flag for review.

---

D. Re-uploaded file duplicates
If the same report is uploaded twice for the same customer, date range, and report type, the system should detect it.
Recommended handling
The system should warn:
This appears to be the same Sales Report for April 1–April 28, 2026 that was already imported.
The user/internal operator should choose:
• Replace prior import
• Append only new records
• Cancel import
• Import as separate version
For V1, the safest default is:
Replace prior import for the same customer/report type/date range unless the operator intentionally chooses append.
This prevents doubled sales, inventory, AR, AP, or PO data.

---

E. Snapshot duplicates
Inventory, AR, AP, and some financial reports are point-in-time snapshots.
For snapshots, duplicate handling is different from activity reports.
Example:
• Inventory snapshot as of April 29, 2026
• AR aging snapshot as of April 29, 2026
• AP aging snapshot as of April 29, 2026
If the same snapshot is uploaded twice, it should not be appended.
Recommended handling
For point-in-time reports, the system should use this logic:
Same customer + same report type + same snapshot date = replace/version, not append.
The user should choose:
• Replace existing snapshot
• Keep both as separate versions
• Cancel
Default should be:
Replace prior snapshot after confirmation.

---

2. Invalid data handling
   Invalid data should be classified by severity.
   The system should use three levels:
   Critical error — blocks import or scoring
   A critical error means the data cannot be trusted for that report type or KPI.
   Examples:
   • Missing required field
   • No transaction date in sales file
   • No SKU in inventory file
   • No open balance in AR/AP file
   • Non-numeric values in required dollar/quantity fields
   • Invalid or unparseable dates
   • Missing location field for required multi-location analysis
   • Inventory file has no quantity or value field
   • Sales file has no sales amount or quantity field
   • Required key fields are mostly blank
   Handling
   Critical errors should block either:
   • the entire file import, or
   • the specific records/KPI calculations affected
   The system should not produce confident results from critical invalid data.
   Output should be:
   Not Ready Yet or Ready with Limitations, depending on severity.

---

Warning — imports with limitations
A warning means the data may be usable, but scoring confidence or drill-down quality is reduced.
Examples:
• Missing vendor ID on some inventory items
• Missing customer ID but customer name exists
• Missing category/department
• Missing COGS but sales dollars exist
• Missing last sale date
• Missing sales rep or channel
• Some invalid rows but most data is usable
• Some SKU/customer/vendor IDs do not match master files
Handling
Warnings should not automatically block the full import.
The system should:
• Import valid records
• Flag invalid or incomplete records
• Lower confidence where appropriate
• Limit affected KPIs/drill-downs
• Show what analysis is weakened
Example:
Missing COGS. Gross Margin Health can calculate sales trend, but margin scoring will be limited or unavailable.

---

Informational issue — imports normally
An informational issue means the system noticed something but it does not materially affect calculations.
Examples:
• Optional field blank
• Unknown brand
• missing sales rep
• extra columns ignored
• formatting cleanup performed
• blank notes column
• row subtotal ignored
Handling
The system should import normally and log the issue.

---

3. Auto-correction rules
   The system may auto-correct safe formatting issues.
   Safe to auto-correct
   The system can safely clean:
   • extra spaces
   • inconsistent capitalization
   • date formatting when clearly parseable
   • currency symbols
   • commas in numbers
   • percent signs
   • blank rows
   • report title rows
   • subtotal rows if clearly marked
   • grand total rows if clearly marked
   • duplicate headers
   • trailing/leading spaces in IDs
   • simple yes/no values
   Examples:
   Source Value Standardized Value
   “ SKU-123 ” “SKU-123”
   “$1,250.00” 1250.00
   “45%” 0.45 or 45%, depending on system standard
   “yes” / “Y” / “TRUE” True
   Auto-corrections should be logged but should not require manual review unless the correction is uncertain.

---

Not safe to auto-correct
The system should not guess on business-critical values.
Do not auto-correct:
• unknown SKU to “closest match”
• unknown vendor to “closest match”
• unknown customer to “closest match”
• ambiguous date format when unclear
• negative sales that may be returns
• positive credits that may need reversal
• missing cost
• missing quantity
• missing open balance
• duplicate invoices with conflicting values
• different locations with same name but different IDs
• unit-of-measure conversions unless rules are configured
These should be flagged for review.

---

4. Record-level handling
   The system should not always reject an entire file because a few rows have problems.
   Use record-level validation where possible.
   Recommended approach
   Each row should receive a row status:
   • Valid
   • Corrected
   • Warning
   • Rejected
   • Needs Review
   Then the system imports only usable rows.
   Example:
   Row Status Handling
   Valid Import
   Corrected Import and log correction
   Warning Import but flag confidence issue
   Needs Review Hold from scoring until reviewed
   Rejected Do not import

---

5. File-level readiness status
   After validation, each file should receive a readiness status:
   Ready to Import
   Use when:
   • Required fields are mapped
   • Most required values are valid
   • Duplicate issues are resolved or immaterial
   • No critical blockers remain
   Ready with Limitations
   Use when:
   • Required minimum fields exist
   • Some supporting fields are missing
   • Some records were rejected
   • Some KPIs or drill-downs will be lower-confidence
   Not Ready Yet
   Use when:
   • Required fields are missing
   • too many rows are invalid
   • critical keys cannot be mapped
   • duplicate risk could materially distort results
   • the system cannot determine date range or snapshot date

---

6. Import exception log
   Every duplicate, invalid, corrected, or rejected item should be captured in an exception log.
   The log should include:
   • Customer
   • Upload batch ID
   • Report type
   • File name
   • Row number
   • Column name
   • Source value
   • Issue type
   • Severity
   • Recommended fix
   • Action taken
   • Reviewed by
   • Review date
   • Final status
   Example:
   Row Field Issue Severity Action
   247 SKU_ID Blank SKU Critical Row rejected
   518 COGS Missing COGS Warning Imported with margin limitation
   900 Net_Sales “$1,200” converted to 1200 Info Auto-corrected
   1200 Invoice_ID Exact duplicate row Warning Duplicate excluded

---

7. User-facing review screen
   The user/internal operator should see a validation summary before final import.
   The summary should show:
   • Total rows uploaded
   • Rows ready to import
   • Rows auto-corrected
   • Rows with warnings
   • Rows rejected
   • Duplicate records found
   • Critical errors
   • Missing required fields
   • KPIs affected
   • Data confidence impact
   Example:
   Sales Import Summary
   12,450 rows uploaded
   12,220 rows ready
   180 rows corrected
   35 warning rows
   15 rejected rows
   Gross Margin Health confidence: Medium because 18% of sales rows are missing COGS.

---

8. Duplicate handling by report type
   Sales
   Possible duplicate key:
   Transaction_ID + Transaction_Date + SKU_ID + Location_ID + Quantity + Net_Sales
   Do not treat repeated invoice numbers alone as duplicates.

---

Inventory
Possible duplicate key:
Snapshot_Date + SKU_ID + Location_ID
If the same SKU/location/snapshot appears twice with different quantities, flag for review.
Inventory snapshots should generally replace prior snapshots for the same date, not append.

---

Product/SKU master
Possible duplicate key:
SKU_ID
If duplicate SKU IDs have identical information, keep one.
If duplicate SKU IDs have conflicting descriptions, vendors, costs, or categories, flag for review.

---

Customer master
Possible duplicate key:
Customer_ID
If duplicate customer IDs have identical information, keep one.
If customer name exists with different customer IDs, flag as possible duplicate customer but do not merge automatically.

---

Vendor master
Possible duplicate key:
Vendor_ID
If same vendor name appears under multiple IDs, flag for possible duplicate vendor but do not merge automatically.

---

Purchase orders
Possible duplicate key:
PO_ID + SKU_ID + Location_ID + Ordered_Quantity + Ordered_Cost
Do not treat repeated PO_ID alone as duplicate because POs often have multiple lines.

---

Receiving
Possible duplicate key:
Receipt_ID + PO_ID + SKU_ID + Location_ID + Received_Quantity + Receipt_Date
Flag if same PO/SKU/location appears with conflicting received quantities.

---

AR
Possible duplicate key:
AR_Snapshot_Date + Customer_ID + Invoice_ID + Open_Balance
If the same AR snapshot is uploaded twice, replace/version instead of append.

---

AP
Possible duplicate key:
AP_Snapshot_Date + Vendor_ID + Bill_ID + Open_Balance
If the same AP snapshot is uploaded twice, replace/version instead of append.

---

Transfers
Possible duplicate key:
Transfer_ID + Transfer_Date + From_Location_ID + To_Location_ID + SKU_ID + Transfer_Quantity
Do not treat transfer ID alone as duplicate if the transfer has multiple SKU lines.

---

Returns / credits
Possible duplicate key:
Return_ID + Return_Date + Customer_ID + SKU_ID + Return_Amount
Returns should be separated from sales when possible.

---

9. How invalid data affects scoring
   Invalid data should affect the output in a controlled way.
   If critical data is missing
   The affected KPI or driver should become:
   Gray / Insufficient Data
   Example:
   If there is no COGS or gross profit data, Gross Margin Health should not produce a confident red/yellow/green margin result.

---

If partial data is missing
The KPI may still calculate, but confidence should be reduced.
Example:
If inventory quantity and value exist, but last sale date is missing, Inventory Health may still calculate inventory value exposure, but slow/dead inventory confidence is reduced.

---

If only optional data is missing
The KPI can calculate normally, but drill-down may be less detailed.
Example:
If sales rep is missing, the system can still analyze sales and margin, but cannot explain the issue by sales rep.

---

10. Recommended import decision logic
    The system should use this import decision structure:
    Import normally when:
    • Required fields exist
    • required values are valid
    • duplicates are resolved
    • only optional fields are missing
    Import with limitations when:
    • required fields exist
    • some supporting fields are missing
    • some records are invalid but not material
    • confidence can be adjusted
    Hold for review when:
    • duplicate records may materially distort results
    • key values conflict
    • data is ambiguous
    • matching is uncertain
    Reject record when:
    • required row-level fields are missing
    • required numeric fields are invalid
    • dates are invalid
    • key IDs are blank and cannot be resolved
    Reject/block file when:
    • required file-level fields are missing
    • wrong report type was uploaded
    • most rows are invalid
    • date range/snapshot cannot be determined
    • there is a high risk of duplicate import distortion

---

Plain-English answer
Duplicate and invalid data should be handled before the calculation engine runs.
The system should:

1. Detect duplicate rows and duplicate files.
2. Separate true duplicates from valid multi-line transactions.
3. Auto-fix safe formatting issues.
4. Flag questionable records for review.
5. Reject records that are clearly unusable.
6. Block files when critical required fields are missing.
7. Keep an exception log.
8. Lower confidence or gray out affected KPIs when the data is incomplete.
   The most important rule is:
   Do not silently import bad data and do not produce confident results from weak data.

---

Rule to lock in
For Decision Priority OS imports, duplicate and invalid data should use this rule:
Clean what is safe, flag what is questionable, reject what is unusable, and reduce confidence where data weakness affects the decision output.
The import process should protect the Management Layer, Drill-Down Layer, KPI scoring, driver/influencer logic, and recommended actions from being distorted by bad source data.
