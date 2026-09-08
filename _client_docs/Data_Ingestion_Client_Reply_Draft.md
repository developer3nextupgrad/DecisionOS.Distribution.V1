# Draft — email to Terry (promised next morning)

> Internal: From Emran, after Rizwan review. Attach `DecisionOS_Financials_Upload_Template.xlsx` from `_client_docs/`. Terry’s live AR sample is **not** in the repo — if it arrived by email only, review it before sending the AR paragraph unchanged.

---

Subject: Decision OS — financial upload template, AR aging, and how to load it

Hi Terry,

Thank you for the call. As promised, here is the Excel template and the upload steps. Please wait to load live data until you have this file in front of you.

**Attachment:** `DecisionOS_Financials_Upload_Template.xlsx`

**What the file contains**

| Tab | What to put there |
|---|---|
| README_Import_Map | Instructions (do not delete) |
| Weekly_Financials | Recommended one-tab summary: sales, COGS, profit, cash, AR/AP totals, inventory — one row per month |
| P_and_L | Profit and loss totals (same months as rows) |
| Weekly_Balance_Sheet | Cash, AR total, inventory, AP total |
| Weekly_Cash_Flow | Net income and cash beginning/end |
| Accounts_Receivable | **Aged invoices** (one row per open invoice) |

May–July 2026 sample numbers are from the SCBS files you already reformatted. Replace them with live figures; keep the **header names in row 1**.

**Layout we confirmed on the call**

- Headers in **row 1**.
- Date on the left (`Week_End_Date`).
- Each month is a **new row** underneath (not a new tab, and not native QuickBooks with accounts down the left).
- P&L, balance sheet, and cash flow all use this same layout. You can keep them as **separate tabs in this one workbook**.
- Next month: add a row (for example `2026-08-31`) and upload the same file again. Prior months are updated, not duplicated.

Native QuickBooks Desktop exports (title rows, outline accounts, period in the top-right) will not import.

**Accounts receivable / aging**

Yes — Decision OS **does support AR aging**. The dashboard KPI is past-due 31+ as a percent of total AR.

That cannot come from QuickBooks General Ledger (the AR dollar total on the balance sheet). It needs an **aged receivables report**: customer, invoice, open amount, and either days past due or an aging bucket (Current, 1–30, 31–60, 61–90, 90+).

The `Accounts_Receivable` tab in the attached file is the format. If you already sent a live AR export, we will match your column names to these fields and reply if anything is missing. You can remove personal names if needed; keep invoice IDs and amounts.

**How to upload (in Decision OS)**

1. Sign in.
2. Go to **Operations → Uploads**.
3. Click **New Batch**.
4. Choose **Simplified — single workbook**.
5. Select the tenant (your distributor).
6. Set **Cadence** to **Monthly**.
7. Set **Anchor date** to the first month you want included (for the sample file: 2026-05-31).
8. Click **Continue**.
9. On **Upload workbook**, choose this `.xlsx` (or download it again from that screen: “Download financials template”).
10. Click **Analyze workbook**.
11. On **Verify**, check that three months were detected and that P&L / balance sheet / cash flow / AR tabs look correct.
12. Click **Validate package**, then **Import all periods**.
13. Open **Dashboard**, pick the tenant and the latest month.

You can also download the template later from **Operations → Uploads** without waiting for email.

If you also have sales and inventory files, use the larger **full 7-KPI workbook template** on the same screens so all seven tiles can score. Financials + AR alone will still load; some operational KPIs will stay gray until those files are included.

Please use this template going forward. If anything on Analyze looks wrong, screenshot the Verify page and send it to us before importing.

Thanks,  
Emran  
NextUpgrad
