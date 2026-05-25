-- Clear imported/scored data for one tenant so you can re-test Simplified import from scratch.
-- Default: DIST-001. Review counts, then COMMIT (or leave ROLLBACK for dry-run).

BEGIN;

-- pgAdmin: change 'DIST-001' if you use another tenant ClientId.

SELECT t."ClientId", t."Id" AS tenant_id
FROM "Tenants" t WHERE t."ClientId" = 'DIST-001';

-- Preview counts
SELECT 'KpiSnapshots' AS entity, COUNT(*) FROM "KpiSnapshots" s
JOIN "Tenants" t ON t."Id" = s."TenantId" WHERE t."ClientId" = 'DIST-001'
UNION ALL SELECT 'DriverValues', COUNT(*) FROM "DriverValues" d
JOIN "Tenants" t ON t."Id" = d."TenantId" WHERE t."ClientId" = 'DIST-001'
UNION ALL SELECT 'NormalizedSalesRows', COUNT(*) FROM "NormalizedSalesRows" n
JOIN "Tenants" t ON t."Id" = n."TenantId" WHERE t."ClientId" = 'DIST-001'
UNION ALL SELECT 'UploadBatches', COUNT(*) FROM "UploadBatches" b
JOIN "Tenants" t ON t."Id" = b."TenantId" WHERE t."ClientId" = 'DIST-001';

-- Delete child → parent order
DELETE FROM "KpiSnapshots" s USING "Tenants" t
WHERE t."Id" = s."TenantId" AND t."ClientId" = 'DIST-001';

DELETE FROM "DriverValues" d USING "Tenants" t
WHERE t."Id" = d."TenantId" AND t."ClientId" = 'DIST-001';

DELETE FROM "Alerts" a USING "Tenants" t
WHERE t."Id" = a."TenantId" AND t."ClientId" = 'DIST-001';

DELETE FROM "WeeklyFocuses" w USING "Tenants" t
WHERE t."Id" = w."TenantId" AND t."ClientId" = 'DIST-001';

DELETE FROM "ActionItems" a USING "Tenants" t
WHERE t."Id" = a."TenantId" AND t."ClientId" = 'DIST-001';

DELETE FROM "NormalizedSalesRows" n USING "Tenants" t
WHERE t."Id" = n."TenantId" AND t."ClientId" = 'DIST-001';

DELETE FROM "NormalizedInventoryRows" n USING "Tenants" t
WHERE t."Id" = n."TenantId" AND t."ClientId" = 'DIST-001';

DELETE FROM "NormalizedArRows" n USING "Tenants" t
WHERE t."Id" = n."TenantId" AND t."ClientId" = 'DIST-001';

DELETE FROM "NormalizedApRows" n USING "Tenants" t
WHERE t."Id" = n."TenantId" AND t."ClientId" = 'DIST-001';

DELETE FROM "UploadBatchIssues" i USING "UploadBatches" b, "Tenants" t
WHERE b."Id" = i."UploadBatchId" AND t."Id" = b."TenantId" AND t."ClientId" = 'DIST-001';

DELETE FROM "UploadedFileColumnMaps" m USING "UploadedFiles" f, "UploadBatches" b, "Tenants" t
WHERE f."Id" = m."UploadedFileId" AND b."Id" = f."UploadBatchId"
  AND t."Id" = b."TenantId" AND t."ClientId" = 'DIST-001';

DELETE FROM "UploadedFiles" f USING "UploadBatches" b, "Tenants" t
WHERE b."Id" = f."UploadBatchId" AND t."Id" = b."TenantId" AND t."ClientId" = 'DIST-001';

DELETE FROM "UploadBatches" b USING "Tenants" t
WHERE t."Id" = b."TenantId" AND t."ClientId" = 'DIST-001';

DELETE FROM "ImportRunIssues" i USING "ImportRuns" r, "Tenants" t
WHERE r."Id" = i."ImportRunId" AND t."Id" = r."TenantId" AND t."ClientId" = 'DIST-001';

DELETE FROM "ImportRuns" r USING "Tenants" t
WHERE t."Id" = r."TenantId" AND t."ClientId" = 'DIST-001';

-- Tenants, users, KPI definitions are kept.

COMMIT;
-- ROLLBACK;
