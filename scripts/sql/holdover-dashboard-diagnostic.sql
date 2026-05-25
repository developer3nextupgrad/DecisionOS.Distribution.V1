-- Holdover Improvements / DriverValues diagnostic (run in pgAdmin on database: decisionos)
-- Replace :client_id and :period_end with values from your dashboard URL, or use section 1 first.

-- =============================================================================
-- 1) Tenants and whether ANY holdover rows exist
-- =============================================================================
SELECT t."ClientId", t."Name", COUNT(d."Id") AS driver_rows
FROM "Tenants" t
LEFT JOIN "DriverValues" d ON d."TenantId" = t."Id"
GROUP BY t."Id", t."ClientId", t."Name"
ORDER BY driver_rows DESC, t."ClientId";

-- =============================================================================
-- 2) Driver rows by tenant + week (holdovers only exist for weeks that were imported)
-- =============================================================================
SELECT t."ClientId",
       d."PeriodEnd",
       COUNT(*) AS rows,
       COUNT(*) FILTER (WHERE d."Dimension1" IS NULL OR TRIM(d."Dimension1") = '') AS no_buyer_key,
       COUNT(*) FILTER (WHERE d."Dimension1" IS NOT NULL AND TRIM(d."Dimension1") <> '') AS with_buyer_key,
       MIN(d."Rank") AS min_rank,
       MAX(d."Rank") AS max_rank
FROM "DriverValues" d
JOIN "Tenants" t ON t."Id" = d."TenantId"
GROUP BY t."ClientId", d."PeriodEnd"
ORDER BY t."ClientId", d."PeriodEnd" DESC;

-- =============================================================================
-- 3) Compare KPI weeks vs driver weeks (GAP: dashboard week has KPIs but no drivers)
-- =============================================================================
SELECT t."ClientId",
       k."PeriodEnd",
       k.kpi_snapshot_count,
       COALESCE(d.driver_count, 0) AS driver_count,
       CASE
           WHEN COALESCE(d.driver_count, 0) = 0 AND k.kpi_snapshot_count > 0 THEN 'GAP: KPIs exist, no holdover rows'
           WHEN COALESCE(d.driver_count, 0) > 0 THEN 'OK for holdover table'
           ELSE 'No KPIs either'
       END AS diagnosis
FROM (
    SELECT "TenantId", "PeriodEnd", COUNT(*) AS kpi_snapshot_count
    FROM "KpiSnapshots"
    GROUP BY "TenantId", "PeriodEnd"
) k
JOIN "Tenants" t ON t."Id" = k."TenantId"
LEFT JOIN (
    SELECT "TenantId", "PeriodEnd", COUNT(*) AS driver_count
    FROM "DriverValues"
    GROUP BY "TenantId", "PeriodEnd"
) d ON d."TenantId" = k."TenantId" AND d."PeriodEnd" = k."PeriodEnd"
ORDER BY t."ClientId", k."PeriodEnd" DESC;

-- =============================================================================
-- 4) Latest simplified import batch vs batch.PeriodEnd (holdovers written ONLY to latest period in batch)
-- =============================================================================
SELECT t."ClientId",
       b."Id" AS batch_id,
       b."Status",
       b."ImportMode",
       b."PeriodEnd" AS batch_latest_period,
       b."ReadinessStatus",
       LEFT(b."DetectionSummaryJson", 120) AS detection_preview,
       (SELECT COUNT(*) FROM "DriverValues" d
        WHERE d."TenantId" = b."TenantId" AND d."PeriodEnd" = b."PeriodEnd") AS drivers_on_batch_period
FROM "UploadBatches" b
JOIN "Tenants" t ON t."Id" = b."TenantId"
WHERE b."ImportMode" = 1  -- Simplified (UploadImportMode.Simplified)
   OR b."WorkbookStoredRelativePath" IS NOT NULL
ORDER BY b."CreatedAt" DESC
LIMIT 20;

-- If ImportMode is stored as string, use instead:
-- WHERE b."ImportMode"::text ILIKE '%Simplified%' OR b."WorkbookStoredRelativePath" IS NOT NULL

-- =============================================================================
-- 5) Sample holdover rows for a tenant (edit ClientId)
-- =============================================================================
SELECT d."PeriodEnd", d."Rank", d."PillarCode", d."DriverName",
       d."Dimension1", d."Owner", d."Status", d."FixProgressPercent"
FROM "DriverValues" d
JOIN "Tenants" t ON t."Id" = d."TenantId"
WHERE t."ClientId" = 'DIST-001'   -- change me
ORDER BY d."PeriodEnd" DESC, d."Rank"
LIMIT 30;

-- =============================================================================
-- 6) Buyer filter simulation (edit ClientId + PeriodEnd + CustomerId from dashboard URL)
-- =============================================================================
-- Dashboard with buyer CUST-0001 keeps rows where Dimension1 = buyer OR name in DriverName.
-- Rows with NULL Dimension1 are DROPPED today → empty table when buyer selected.
SELECT d.*
FROM "DriverValues" d
JOIN "Tenants" t ON t."Id" = d."TenantId"
WHERE t."ClientId" = 'DIST-001'
  AND d."PeriodEnd" = DATE '2026-04-28'   -- change to your reporting week
  AND (
        d."Dimension1" IS NULL OR TRIM(d."Dimension1") = ''
        OR d."Dimension1" ILIKE 'CUST-0001'
        OR d."DriverName" ILIKE '%CUST-0001%'
      );

-- Rows that WOULD show after fix (include tenant-wide holdovers with null Dimension1):
-- AND (d."Dimension1" IS NULL OR TRIM(d."Dimension1") = '' OR d."Dimension1" ILIKE 'CUST-0001' OR ...)

-- =============================================================================
-- 7) End customers in staging (buyer dropdown source — separate from holdovers)
-- =============================================================================
SELECT DISTINCT t."ClientId", s."CustomerId", s."CustomerName"
FROM "NormalizedSalesRows" s
JOIN "Tenants" t ON t."Id" = s."TenantId"
WHERE s."CustomerId" IS NOT NULL OR s."CustomerName" IS NOT NULL
ORDER BY t."ClientId", s."CustomerId"
LIMIT 50;
