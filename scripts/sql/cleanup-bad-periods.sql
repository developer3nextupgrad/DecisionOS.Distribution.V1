-- Optional: remove bogus period rows from bad-date imports (review counts before COMMIT)
-- Run in pgAdmin on database decisionos. Adjust ClientId if needed.

-- Preview rows to delete (implausible years from bad date detection)
SELECT 'KpiSnapshots' AS tbl, COUNT(*) AS row_count
FROM "KpiSnapshots" s
JOIN "Tenants" t ON t."Id" = s."TenantId"
WHERE t."ClientId" = 'DIST-001'
  AND (EXTRACT(YEAR FROM s."PeriodEnd") < 2015 OR EXTRACT(YEAR FROM s."PeriodEnd") > 2040);

-- Then run DELETE statements one at a time after review, e.g.:
-- DELETE FROM "KpiSnapshots" s USING "Tenants" t WHERE t."Id" = s."TenantId"
--   AND t."ClientId" = 'DIST-001' AND (EXTRACT(YEAR FROM s."PeriodEnd") < 2015 OR EXTRACT(YEAR FROM s."PeriodEnd") > 2040);
