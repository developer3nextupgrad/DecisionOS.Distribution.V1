using System.Globalization;
using System.Text.Json;
using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Normalized;
using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure.Workbooks;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Infrastructure;

public sealed class SimplifiedWorkbookImportService : ISimplifiedWorkbookImportService
{
    private readonly DecisionOsDbContext _db;
    private readonly IWorkbookAnalyzer _analyzer;
    private readonly IWeeklyScoringService _scoring;
    public SimplifiedWorkbookImportService(
        DecisionOsDbContext db,
        IWorkbookAnalyzer analyzer,
        IWeeklyScoringService scoring)
    {
        _db = db;
        _analyzer = analyzer;
        _scoring = scoring;
    }

    public async Task<WorkbookDetectionResult> DetectAndPersistAsync(
        long batchId,
        byte[] workbookBytes,
        string originalFileName,
        string contentRootPath,
        CancellationToken ct = default)
    {
        var batch = await _db.UploadBatches.Include(b => b.Tenant)
            .FirstOrDefaultAsync(b => b.Id == batchId, ct)
            ?? throw new InvalidOperationException("Batch not found.");

        var cadence = batch.Cadence ?? UploadCadence.Weekly;
        var detection = _analyzer.Analyze(workbookBytes, cadence, batch.AnchorPeriodEnd);
        ApplyDetectionToBatch(batch, detection);

        var sha = UploadedFile.ComputeSha256Hex(workbookBytes);
        var folder = Path.Combine(contentRootPath, "App_Data", "uploads", batch.Tenant.ClientId,
            "simplified", batch.Id.ToString(CultureInfo.InvariantCulture));
        Directory.CreateDirectory(folder);

        var storedName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}_{sha[..10]}.xlsx";
        var fullPath = Path.Combine(folder, storedName);
        await File.WriteAllBytesAsync(fullPath, workbookBytes, ct);
        var rel = Path.GetRelativePath(contentRootPath, fullPath).Replace('\\', '/');

        batch.WorkbookStoredRelativePath = rel;

        _db.UploadedFiles.RemoveRange(_db.UploadedFiles.Where(f => f.UploadBatchId == batch.Id));
        foreach (var sheet in detection.Sheets.Where(s => s.Kind != WorkbookSheetKind.Skip && s.Kind != WorkbookSheetKind.Unknown))
        {
            _db.UploadedFiles.Add(new UploadedFile
            {
                UploadBatchId = batch.Id,
                ReportType = sheet.ReportType ?? ReportType.Other,
                OriginalFileName = $"{originalFileName}::{sheet.SheetName}",
                StoredFileName = storedName,
                StoredRelativePath = rel,
                Sha256Hex = sha,
                HeaderRowNumber = sheet.HeaderRowNumber > 0 ? sheet.HeaderRowNumber : 1,
                UploadedAt = DateTimeOffset.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
        return detection;
    }

    public async Task<WorkbookDetectionResult> ReanalyzeStoredWorkbookAsync(
        long batchId,
        string contentRootPath,
        DateOnly? anchorPeriodEnd,
        UploadCadence? cadence,
        CancellationToken ct = default)
    {
        var batch = await _db.UploadBatches.Include(b => b.Tenant)
            .FirstOrDefaultAsync(b => b.Id == batchId, ct)
            ?? throw new InvalidOperationException("Batch not found.");

        if (string.IsNullOrWhiteSpace(batch.WorkbookStoredRelativePath))
            throw new InvalidOperationException("No workbook stored for this batch.");

        if (anchorPeriodEnd is not null)
            batch.AnchorPeriodEnd = anchorPeriodEnd;
        if (cadence is not null)
            batch.Cadence = cadence;

        var fullPath = Path.Combine(contentRootPath,
            batch.WorkbookStoredRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var bytes = await File.ReadAllBytesAsync(fullPath, ct);
        var detection = _analyzer.Analyze(bytes, batch.Cadence ?? UploadCadence.Weekly, batch.AnchorPeriodEnd);
        ApplyDetectionToBatch(batch, detection);
        batch.DetectionSummaryJson = WorkbookAnalyzer.Serialize(detection);

        await _db.SaveChangesAsync(ct);
        return detection;
    }

    private static void ApplyDetectionToBatch(UploadBatch batch, WorkbookDetectionResult detection)
    {
        batch.WorkbookFingerprint = detection.WorkbookFingerprint;
        batch.DetectionSummaryJson = WorkbookAnalyzer.Serialize(detection);
        batch.ReadinessStatus = detection.FilteredPeriodEnds.Count > 0 ? "ReadyWithLimitations" : "NotReadyYet";
        batch.Status = UploadBatchStatuses.MappingInProgress;

        if (detection.AnchorAutoAdjusted && detection.EffectiveAnchorPeriodEnd is not null)
            batch.AnchorPeriodEnd = detection.EffectiveAnchorPeriodEnd;

        if (detection.FilteredPeriodEnds.Count > 0)
            batch.PeriodEnd = detection.FilteredPeriodEnds.Max();
    }

    public async Task ValidateSimplifiedAsync(long batchId, CancellationToken ct = default)
    {
        var batch = await _db.UploadBatches.FirstOrDefaultAsync(b => b.Id == batchId, ct);
        if (batch is null) return;

        var detection = WorkbookAnalyzer.Deserialize(batch.DetectionSummaryJson);
        var issues = new List<UploadBatchIssue>();

        if (detection is null)
        {
            issues.Add(new UploadBatchIssue
            {
                UploadBatchId = batchId,
                Severity = UploadIssueSeverity.Critical,
                Category = "Detection",
                Message = "Workbook has not been analyzed yet.",
                Field = "workbook"
            });
        }
        else
        {
            if (detection.FilteredPeriodEnds.Count == 0)
            {
                issues.Add(new UploadBatchIssue
                {
                    UploadBatchId = batchId,
                    Severity = UploadIssueSeverity.Critical,
                    Category = "Periods",
                    Message = "No periods found at or after anchor date.",
                    Field = "anchor"
                });
            }

            if (!detection.Sheets.Any(s => s.Kind == WorkbookSheetKind.WeeklyRollup) &&
                !detection.Sheets.Any(s => s.Kind == WorkbookSheetKind.Sales))
            {
                issues.Add(new UploadBatchIssue
                {
                    UploadBatchId = batchId,
                    Severity = UploadIssueSeverity.Critical,
                    Category = "Package",
                    Message = "Need weekly rollup or sales detail sheet.",
                    Field = "sheets"
                });
            }

            foreach (var w in detection.Warnings)
            {
                issues.Add(new UploadBatchIssue
                {
                    UploadBatchId = batchId,
                    Severity = UploadIssueSeverity.Warning,
                    Category = "Detection",
                    Message = w,
                    Field = "workbook"
                });
            }

            ValidateExpectedKpis(detection, issues, batchId);
        }

        _db.UploadBatchIssues.RemoveRange(_db.UploadBatchIssues.Where(i => i.UploadBatchId == batchId));
        _db.UploadBatchIssues.AddRange(issues);

        var hasCritical = issues.Any(i => i.Severity == UploadIssueSeverity.Critical);
        var hasWarnings = issues.Any(i => i.Severity == UploadIssueSeverity.Warning);
        batch.ReadinessStatus = hasCritical ? "NotReadyYet" : hasWarnings ? "ReadyWithLimitations" : "ReadyToRun";
        batch.Status = UploadBatchStatuses.Validated;
        batch.ValidationSummary =
            $"Simplified; Readiness={batch.ReadinessStatus}; Periods={detection?.FilteredPeriodEnds.Count ?? 0}; Critical={issues.Count(i => i.Severity == UploadIssueSeverity.Critical)}; Warning={issues.Count(i => i.Severity == UploadIssueSeverity.Warning)}";

        await _db.SaveChangesAsync(ct);
    }

    public async Task RunSimplifiedImportAsync(long batchId, string contentRootPath, CancellationToken ct = default)
    {
        await ValidateSimplifiedAsync(batchId, ct);
        var batch = await _db.UploadBatches.Include(b => b.Tenant).FirstOrDefaultAsync(b => b.Id == batchId, ct)
            ?? throw new InvalidOperationException("Batch not found.");

        if (string.Equals(batch.ReadinessStatus, "NotReadyYet", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Batch is NotReadyYet. {batch.ValidationSummary}");

        if (string.IsNullOrWhiteSpace(batch.WorkbookStoredRelativePath))
            throw new InvalidOperationException("No workbook stored for simplified batch.");

        var detection = WorkbookAnalyzer.Deserialize(batch.DetectionSummaryJson)
            ?? throw new InvalidOperationException("Missing detection summary.");

        var fullPath = Path.Combine(contentRootPath,
            batch.WorkbookStoredRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var wb = WorkbookParseHelper.ParseFile(fullPath);
        var sheetByName = wb.Sheets.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

        var tenant = batch.Tenant;
        var periods = detection.FilteredPeriodEnds
            .Where(WorkbookDateRules.IsPlausiblePeriod)
            .Distinct()
            .OrderBy(p => p)
            .ToList();
        if (periods.Count == 0) return;

        await WorkbookKpiDefinitionEnsurer.EnsureAsync(_db, detection, ct);

        var confidence = batch.ReadinessStatus switch
        {
            "ReadyToRun" => "High",
            "ReadyWithLimitations" => "Medium",
            _ => "Low"
        };

        ClearNormalizedForBatch(batch.Id, ct);

        var rollupSheets = detection.Sheets.Where(s => s.Kind == WorkbookSheetKind.WeeklyRollup).ToList();

        var salesSheet = detection.Sheets.FirstOrDefault(s => s.Kind == WorkbookSheetKind.Sales);
        var invSheet = detection.Sheets.FirstOrDefault(s => s.Kind == WorkbookSheetKind.Inventory);
        var arSheet = detection.Sheets.FirstOrDefault(s => s.Kind == WorkbookSheetKind.AccountsReceivable);
        var apSheet = detection.Sheets.FirstOrDefault(s => s.Kind == WorkbookSheetKind.AccountsPayable);
        var customerSheet = detection.Sheets.FirstOrDefault(s => s.Kind == WorkbookSheetKind.Customer);
        var vendorSheet = detection.Sheets.FirstOrDefault(s => s.Kind == WorkbookSheetKind.Vendor);

        var latestPeriod = ResolveLatestOperationalPeriod(detection, sheetByName, periods);
        var totalDrivers = 0;
        var totalSnapshots = 0;
        var traceFileId = await _db.UploadedFiles
            .Where(f => f.UploadBatchId == batch.Id)
            .Select(f => f.Id)
            .FirstOrDefaultAsync(ct);

        foreach (var period in periods)
        {
            var directKpis = ExtractDirectKpisFromAllRollups(rollupSheets, sheetByName, period);
            MergeVendorFillRate(directKpis, vendorSheet, sheetByName, period);

            if (salesSheet is not null && sheetByName.TryGetValue(salesSheet.SheetName, out var sp))
                ImportSales(sp, salesSheet.ColumnMappings, tenant.Id, batch.Id, traceFileId, period, latestPeriod);

            ImportRollupInventoryForPeriod(rollupSheets, sheetByName, tenant.Id, batch.Id, traceFileId, period);
            ImportRollupSalesCogsForPeriod(rollupSheets, sheetByName, tenant.Id, batch.Id, traceFileId, period);
            ImportRollupArApTotalsForPeriod(rollupSheets, sheetByName, tenant.Id, batch.Id, traceFileId, period);

            if (period == latestPeriod)
            {
                if (invSheet is not null && sheetByName.TryGetValue(invSheet.SheetName, out var ip))
                    ImportInventory(ip, invSheet.ColumnMappings, tenant.Id, batch.Id, traceFileId, period);
                if (arSheet is not null && sheetByName.TryGetValue(arSheet.SheetName, out var arp))
                    ImportAr(arp, arSheet.ColumnMappings, tenant.Id, batch.Id, traceFileId, period);
                if (apSheet is not null && sheetByName.TryGetValue(apSheet.SheetName, out var app))
                    ImportAp(app, apSheet.ColumnMappings, tenant.Id, batch.Id, traceFileId, period);
            }

            await _db.SaveChangesAsync(ct);

            var scoreResult = await _scoring.ScorePeriodAsync(new WeeklyScoringRequest
            {
                TenantId = tenant.Id,
                PeriodEnd = period,
                UploadBatchId = batch.Id,
                DataConfidence = confidence,
                DirectKpiValues = directKpis
            }, ct);

            totalSnapshots += scoreResult.SnapshotsWritten;

            var run = new ImportRun
            {
                TenantId = tenant.Id,
                PeriodEnd = period,
                StartedAt = DateTimeOffset.UtcNow,
                CompletedAt = DateTimeOffset.UtcNow,
                Status = "Completed",
                SourceFingerprint = batch.WorkbookFingerprint ?? "",
                ValidationSummary = batch.ValidationSummary,
                ReadinessStatus = batch.ReadinessStatus,
                KpiRowsProcessed = scoreResult.SnapshotsWritten,
                DriverRowsProcessed = 0
            };
            _db.ImportRuns.Add(run);
        }

        totalDrivers += await ImportHoldoversForAllPeriodsAsync(
            detection, sheetByName, tenant.Id, periods, ct);
        totalDrivers += await ImportOperationalIssuesAsync(
            detection, sheetByName, tenant, latestPeriod, ct);

        if (customerSheet is not null && sheetByName.TryGetValue(customerSheet.SheetName, out var cp))
            ImportCustomerMaster(cp, customerSheet.ColumnMappings, tenant.Id, batch.Id, traceFileId, latestPeriod);

        await _db.SaveChangesAsync(ct);

        batch.PeriodEnd = latestPeriod;
        batch.Status = UploadBatchStatuses.Imported;
        batch.ValidationSummary = $"{batch.ValidationSummary}; ImportedPeriods={periods.Count}; Drivers={totalDrivers}";

        var lastRun = await _db.ImportRuns
            .Where(r => r.TenantId == tenant.Id && periods.Contains(r.PeriodEnd))
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync(ct);
        if (lastRun is not null) batch.ImportRunId = lastRun.Id;

        await _db.SaveChangesAsync(ct);
    }

    private void ClearNormalizedForBatch(long batchId, CancellationToken ct)
    {
        _db.NormalizedSalesRows.RemoveRange(_db.NormalizedSalesRows.Where(r => r.UploadBatchId == batchId));
        _db.NormalizedInventoryRows.RemoveRange(_db.NormalizedInventoryRows.Where(r => r.UploadBatchId == batchId));
        _db.NormalizedArRows.RemoveRange(_db.NormalizedArRows.Where(r => r.UploadBatchId == batchId));
        _db.NormalizedApRows.RemoveRange(_db.NormalizedApRows.Where(r => r.UploadBatchId == batchId));
    }

    private static Dictionary<string, decimal?> ExtractDirectKpisFromAllRollups(
        IReadOnlyList<DetectedSheet> rollupSheets,
        Dictionary<string, ParsedSheet> sheetByName,
        DateOnly period)
    {
        var result = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);
        foreach (var det in rollupSheets)
        {
            if (!sheetByName.TryGetValue(det.SheetName, out var parsed)) continue;

            if (WorkbookRollupKpiExtractor.SheetHasVendorRows(parsed.Headers))
            {
                var vendorRows = parsed.Rows
                    .Where(r => ColumnSynonymMatcher.ResolveRowPeriod(r, det.ColumnMappings) == period)
                    .ToList();
                var apPct = WorkbookRollupKpiExtractor.AggregateApPastDuePercent(vendorRows);
                if (apPct is not null)
                    result["AP_PastDue31p%"] = apPct;
                continue;
            }

            foreach (var kvp in ExtractDirectKpis(parsed, det.ColumnMappings, period))
            {
                if (kvp.Value is not null)
                    result[kvp.Key] = kvp.Value;
            }
        }
        return result;
    }

    private static void MergeVendorFillRate(
        Dictionary<string, decimal?> directKpis,
        DetectedSheet? vendorSheet,
        Dictionary<string, ParsedSheet> sheetByName,
        DateOnly period)
    {
        if (directKpis.ContainsKey("PerfectOrderRate")) return;
        if (vendorSheet is null || !sheetByName.TryGetValue(vendorSheet.SheetName, out var sheet)) return;

        var fillHeader = vendorSheet.ColumnMappings
            .FirstOrDefault(m => m.Value.Contains("Fill", StringComparison.OrdinalIgnoreCase)).Key;
        if (fillHeader is null)
        {
            fillHeader = sheet.Headers.FirstOrDefault(h =>
                WorkbookParseHelper.NormalizeHeader(h).Contains("fillrate", StringComparison.Ordinal));
        }
        if (fillHeader is null) return;

        var values = sheet.Rows
            .Select(r => WorkbookParseHelper.ParseDecimal(r.TryGetValue(fillHeader, out var v) ? v : null))
            .Where(v => v is not null)
            .Select(v => v!.Value)
            .ToList();
        if (values.Count == 0) return;

        var avg = values.Average();
        if (avg > 1m) avg /= 100m;
        directKpis["PerfectOrderRate"] = avg;
    }

    private void ImportRollupInventoryForPeriod(
        IReadOnlyList<DetectedSheet> rollupSheets,
        Dictionary<string, ParsedSheet> sheetByName,
        Guid tenantId,
        long batchId,
        long uploadedFileId,
        DateOnly period)
    {
        foreach (var det in rollupSheets)
        {
            if (!sheetByName.TryGetValue(det.SheetName, out var sheet)) continue;
            if (!det.ColumnMappings.Values.Any(v =>
                    string.Equals(v, "Inventory_Value", StringComparison.OrdinalIgnoreCase)))
                continue;

            foreach (var row in sheet.Rows)
            {
                var rowDate = WorkbookDateRules.TryParsePeriodDate(
                    ColumnSynonymMatcher.GetMapped(row, det.ColumnMappings, "Period_End_Date"));
                if (rowDate != period) continue;

                var invVal = WorkbookParseHelper.ParseDecimal(
                    ColumnSynonymMatcher.GetMapped(row, det.ColumnMappings, "Inventory_Value"));
                if (invVal is null or <= 0) continue;

                _db.NormalizedInventoryRows.Add(new NormalizedInventoryRow
                {
                    TenantId = tenantId,
                    PeriodEnd = period,
                    UploadBatchId = batchId,
                    UploadedFileId = uploadedFileId,
                    SourceRowNumber = 0,
                    Status = RowStatus.Valid,
                    RawJson = "{}",
                    SnapshotDate = period,
                    InventoryValue = invVal
                });
                return;
            }
        }
    }

    private void ImportRollupSalesCogsForPeriod(
        IReadOnlyList<DetectedSheet> rollupSheets,
        Dictionary<string, ParsedSheet> sheetByName,
        Guid tenantId,
        long batchId,
        long uploadedFileId,
        DateOnly period)
    {
        foreach (var det in rollupSheets)
        {
            if (WorkbookRollupKpiExtractor.SheetHasVendorRows(det.Headers)) continue;
            if (!sheetByName.TryGetValue(det.SheetName, out var sheet)) continue;

            foreach (var row in sheet.Rows)
            {
                var rowDate = WorkbookDateRules.TryParsePeriodDate(
                    ColumnSynonymMatcher.GetMapped(row, det.ColumnMappings, "Period_End_Date"));
                if (rowDate != period) continue;

                var net = WorkbookParseHelper.ParseDecimal(ColumnSynonymMatcher.GetMapped(row, det.ColumnMappings, "Net_Sales"));
                var cogs = WorkbookParseHelper.ParseDecimal(ColumnSynonymMatcher.GetMapped(row, det.ColumnMappings, "COGS"));
                if (net is null && cogs is null) continue;

                _db.NormalizedSalesRows.Add(new NormalizedSalesRow
                {
                    TenantId = tenantId,
                    PeriodEnd = period,
                    UploadBatchId = batchId,
                    UploadedFileId = uploadedFileId,
                    SourceRowNumber = 0,
                    Status = RowStatus.Valid,
                    RawJson = "{}",
                    TransactionDate = period,
                    NetSales = net,
                    Cogs = cogs
                });
                return;
            }
        }
    }

    private void ImportRollupArApTotalsForPeriod(
        IReadOnlyList<DetectedSheet> rollupSheets,
        Dictionary<string, ParsedSheet> sheetByName,
        Guid tenantId,
        long batchId,
        long uploadedFileId,
        DateOnly period)
    {
        foreach (var det in rollupSheets)
        {
            if (WorkbookRollupKpiExtractor.SheetHasVendorRows(det.Headers)) continue;
            if (!sheetByName.TryGetValue(det.SheetName, out var sheet)) continue;

            foreach (var row in sheet.Rows)
            {
                var rowDate = WorkbookDateRules.TryParsePeriodDate(
                    ColumnSynonymMatcher.GetMapped(row, det.ColumnMappings, "Period_End_Date"));
                if (rowDate != period) continue;

                var arTotal = ParseByHeader(row, "artotal");
                if (arTotal is > 0)
                {
                    _db.NormalizedArRows.Add(new NormalizedArRow
                    {
                        TenantId = tenantId,
                        PeriodEnd = period,
                        UploadBatchId = batchId,
                        UploadedFileId = uploadedFileId,
                        SourceRowNumber = 0,
                        Status = RowStatus.Valid,
                        RawJson = "{}",
                        SnapshotDate = period,
                        OpenBalance = arTotal
                    });
                }

                var apTotal = ParseByHeader(row, "aptotal");
                if (apTotal is > 0)
                {
                    _db.NormalizedApRows.Add(new NormalizedApRow
                    {
                        TenantId = tenantId,
                        PeriodEnd = period,
                        UploadBatchId = batchId,
                        UploadedFileId = uploadedFileId,
                        SourceRowNumber = 0,
                        Status = RowStatus.Valid,
                        RawJson = "{}",
                        SnapshotDate = period,
                        OpenBalance = apTotal
                    });
                }

                return;
            }
        }
    }

    private static Dictionary<string, decimal?> ExtractDirectKpis(
        ParsedSheet? sheet,
        IReadOnlyDictionary<string, string>? colMap,
        DateOnly period)
    {
        var result = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);
        if (sheet is null || colMap is null) return result;

        foreach (var row in sheet.Rows)
        {
            var rowDate = WorkbookDateRules.TryParsePeriodDate(ColumnSynonymMatcher.GetMapped(row, colMap, "Period_End_Date"));
            if (rowDate != period) continue;

            void SetRatio(string code, string field)
            {
                var v = ColumnSynonymMatcher.GetMapped(row, colMap, field);
                var d = WorkbookParseHelper.ParseDecimal(v);
                if (d is null) return;
                var ratio = WorkbookRollupKpiExtractor.NormalizeRatio(d.Value);
                if (ratio is not null)
                    result[code] = ratio;
            }

            SetRatio("GrossMargin%", "Gross_Margin_Percent");
            SetRatio("AR_PastDue31p%", "AR_Over_60_Pct");
            SetRatio("AP_PastDue31p%", "AP_Past_Due_Pct");
            SetRatio("PerfectOrderRate", "Fill_Rate_Pct");
            SetRatio("NetProfit%", "Net_Profit_Percent");

            var net = WorkbookParseHelper.ParseDecimal(ColumnSynonymMatcher.GetMapped(row, colMap, "Net_Sales"));
            var cogs = WorkbookParseHelper.ParseDecimal(ColumnSynonymMatcher.GetMapped(row, colMap, "COGS"));
            if (net is > 0 && cogs is > 0 && !result.ContainsKey("GrossMargin%"))
                result["GrossMargin%"] = (net - cogs) / net;

            var inv = WorkbookParseHelper.ParseDecimal(ColumnSynonymMatcher.GetMapped(row, colMap, "Inventory_Value"));
            var doh = WorkbookRollupKpiExtractor.TryComputeDoh(inv, cogs);
            if (doh is not null)
                result["DOH"] = doh;

            if (!result.ContainsKey("NetProfit%"))
            {
                var opProfit = ParseByHeader(row, "operatingprofit", "operating_profit");
                var np = WorkbookRollupKpiExtractor.TryComputeNetProfitPercent(net, opProfit);
                if (np is not null)
                    result["NetProfit%"] = np;
            }

            if (!result.ContainsKey("AR_PastDue31p%"))
            {
                var arPct = WorkbookRollupKpiExtractor.TryComputeArPastDuePercent(row);
                if (arPct is not null)
                    result["AR_PastDue31p%"] = arPct;
            }
        }

        return result;
    }

    private static decimal? ParseByHeader(IReadOnlyDictionary<string, string?> row, params string[] tokens)
    {
        foreach (var kvp in row)
        {
            var norm = WorkbookParseHelper.NormalizeHeader(kvp.Key);
            if (!tokens.Any(t => norm.Contains(t, StringComparison.Ordinal))) continue;
            return WorkbookParseHelper.ParseDecimal(kvp.Value);
        }
        return null;
    }

    private void ImportSales(
        ParsedSheet sheet,
        IReadOnlyDictionary<string, string> colMap,
        Guid tenantId,
        long batchId,
        long uploadedFileId,
        DateOnly period,
        DateOnly latestPeriod)
    {
        var hasPeriodColumn = ColumnSynonymMatcher.MapsAnyPeriodField(colMap);
        var rowNum = 1;
        foreach (var row in sheet.Rows)
        {
            rowNum++;
            DateOnly txDate;
            if (hasPeriodColumn)
            {
                var resolved = ColumnSynonymMatcher.ResolveRowPeriod(row, colMap);
                if (resolved is null || resolved != period) continue;
                txDate = resolved.Value;
            }
            else
            {
                if (period != latestPeriod) continue;
                txDate = period;
            }

            var (customerId, customerName) = CustomerKeyResolver.Resolve(
                ColumnSynonymMatcher.GetMapped(row, colMap, "Customer_ID"),
                ColumnSynonymMatcher.GetMapped(row, colMap, "Customer_Name"));

            _db.NormalizedSalesRows.Add(new NormalizedSalesRow
            {
                TenantId = tenantId,
                PeriodEnd = period,
                UploadBatchId = batchId,
                UploadedFileId = uploadedFileId,
                SourceRowNumber = rowNum,
                Status = RowStatus.Valid,
                RawJson = JsonSerializer.Serialize(row),
                TransactionDate = txDate,
                QuantitySold = WorkbookParseHelper.ParseDecimal(ColumnSynonymMatcher.GetMapped(row, colMap, "Quantity_Sold")),
                NetSales = WorkbookParseHelper.ParseDecimal(ColumnSynonymMatcher.GetMapped(row, colMap, "Net_Sales")),
                Cogs = WorkbookParseHelper.ParseDecimal(ColumnSynonymMatcher.GetMapped(row, colMap, "COGS")),
                CustomerId = customerId,
                CustomerName = customerName,
                SkuId = ColumnSynonymMatcher.GetMapped(row, colMap, "SKU_ID")
            });
        }
    }

    private void ImportCustomerMaster(
        ParsedSheet sheet,
        IReadOnlyDictionary<string, string> colMap,
        Guid tenantId,
        long batchId,
        long uploadedFileId,
        DateOnly period)
    {
        var rowNum = 1;
        foreach (var row in sheet.Rows)
        {
            rowNum++;
            var (customerId, customerName) = CustomerKeyResolver.Resolve(
                ColumnSynonymMatcher.GetMapped(row, colMap, "Customer_ID"),
                ColumnSynonymMatcher.GetMapped(row, colMap, "Customer_Name"));
            if (customerId is null) continue;

            _db.NormalizedArRows.Add(new NormalizedArRow
            {
                TenantId = tenantId,
                PeriodEnd = period,
                UploadBatchId = batchId,
                UploadedFileId = uploadedFileId,
                SourceRowNumber = rowNum,
                Status = RowStatus.Valid,
                RawJson = JsonSerializer.Serialize(row),
                SnapshotDate = period,
                CustomerId = customerId,
                CustomerName = customerName,
                OpenBalance = 0m,
                DaysPastDue = 0
            });
        }
    }

    private void ImportInventory(
        ParsedSheet sheet,
        IReadOnlyDictionary<string, string> colMap,
        Guid tenantId,
        long batchId,
        long uploadedFileId,
        DateOnly period)
    {
        var rowNum = 1;
        foreach (var row in sheet.Rows)
        {
            rowNum++;
            _db.NormalizedInventoryRows.Add(new NormalizedInventoryRow
            {
                TenantId = tenantId,
                PeriodEnd = period,
                UploadBatchId = batchId,
                UploadedFileId = uploadedFileId,
                SourceRowNumber = rowNum,
                Status = RowStatus.Valid,
                RawJson = JsonSerializer.Serialize(row),
                SnapshotDate = period,
                QuantityOnHand = WorkbookParseHelper.ParseDecimal(ColumnSynonymMatcher.GetMapped(row, colMap, "Quantity_On_Hand")),
                InventoryValue = WorkbookParseHelper.ParseDecimal(ColumnSynonymMatcher.GetMapped(row, colMap, "Inventory_Value"))
            });
        }
    }

    private void ImportAr(
        ParsedSheet sheet,
        IReadOnlyDictionary<string, string> colMap,
        Guid tenantId,
        long batchId,
        long uploadedFileId,
        DateOnly period)
    {
        var rowNum = 1;
        foreach (var row in sheet.Rows)
        {
            rowNum++;
            var (customerId, customerName) = CustomerKeyResolver.Resolve(
                ColumnSynonymMatcher.GetMapped(row, colMap, "Customer_ID"),
                ColumnSynonymMatcher.GetMapped(row, colMap, "Customer_Name"));

            _db.NormalizedArRows.Add(new NormalizedArRow
            {
                TenantId = tenantId,
                PeriodEnd = period,
                UploadBatchId = batchId,
                UploadedFileId = uploadedFileId,
                SourceRowNumber = rowNum,
                Status = RowStatus.Valid,
                RawJson = JsonSerializer.Serialize(row),
                SnapshotDate = period,
                DaysPastDue = WorkbookParseHelper.ParseInt(ColumnSynonymMatcher.GetMapped(row, colMap, "Days_Past_Due")),
                AgingBucket = ColumnSynonymMatcher.GetMapped(row, colMap, "Aging_Bucket"),
                OpenBalance = WorkbookParseHelper.ParseDecimal(ColumnSynonymMatcher.GetMapped(row, colMap, "Open_Balance")),
                CustomerId = customerId,
                CustomerName = customerName
            });
        }
    }

    private void ImportAp(
        ParsedSheet sheet,
        IReadOnlyDictionary<string, string> colMap,
        Guid tenantId,
        long batchId,
        long uploadedFileId,
        DateOnly period)
    {
        var rowNum = 1;
        foreach (var row in sheet.Rows)
        {
            rowNum++;
            _db.NormalizedApRows.Add(new NormalizedApRow
            {
                TenantId = tenantId,
                PeriodEnd = period,
                UploadBatchId = batchId,
                UploadedFileId = uploadedFileId,
                SourceRowNumber = rowNum,
                Status = RowStatus.Valid,
                RawJson = JsonSerializer.Serialize(row),
                SnapshotDate = period,
                DaysPastDue = WorkbookParseHelper.ParseInt(ColumnSynonymMatcher.GetMapped(row, colMap, "Days_Past_Due")),
                AgingBucket = ColumnSynonymMatcher.GetMapped(row, colMap, "Aging_Bucket"),
                OpenBalance = WorkbookParseHelper.ParseDecimal(ColumnSynonymMatcher.GetMapped(row, colMap, "Open_Balance"))
            });
        }
    }

    /// <summary>
    /// Open holdover actions apply to every imported reporting week (carry-forward), not only the batch anchor week.
    /// </summary>
    private async Task<int> ImportHoldoversForAllPeriodsAsync(
        WorkbookDetectionResult detection,
        Dictionary<string, ParsedSheet> sheetByName,
        Guid tenantId,
        IReadOnlyList<DateOnly> periods,
        CancellationToken ct)
    {
        if (periods.Count == 0) return 0;

        var templates = BuildHoldoverTemplates(detection, sheetByName);
        if (templates.Count == 0) return 0;

        foreach (var period in periods)
        {
            _db.DriverValues.RemoveRange(
                _db.DriverValues.Where(d => d.TenantId == tenantId && d.PeriodEnd == period));

            foreach (var template in templates)
            {
                _db.DriverValues.Add(new DriverValue
                {
                    TenantId = tenantId,
                    PeriodEnd = period,
                    PillarCode = template.PillarCode,
                    DriverName = template.DriverName,
                    Rank = template.Rank,
                    Status = template.Status,
                    WhyItMatters = template.WhyItMatters,
                    Owner = template.Owner,
                    AssignedSummary = template.AssignedSummary,
                    TargetSummary = template.TargetSummary,
                    CurrentSummary = template.CurrentSummary,
                    FixProgressPercent = template.FixProgressPercent,
                    Dimension1 = template.Dimension1,
                    Dimension2 = template.Dimension2
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        return templates.Count * periods.Count;
    }

    private static List<HoldoverTemplate> BuildHoldoverTemplates(
        WorkbookDetectionResult detection,
        Dictionary<string, ParsedSheet> sheetByName)
    {
        var holdSheet = detection.Sheets.FirstOrDefault(s => s.Kind == WorkbookSheetKind.Holdover);
        if (holdSheet is null || !sheetByName.TryGetValue(holdSheet.SheetName, out var sheet))
            return [];

        var colMap = holdSheet.ColumnMappings;
        var templates = new List<HoldoverTemplate>();
        var rank = 1;
        foreach (var row in sheet.Rows)
        {
            var area = row.FirstOrDefault(kvp => kvp.Key.Contains("Area", StringComparison.OrdinalIgnoreCase)).Value
                ?? "General";
            var completion = WorkbookParseHelper.ParseDecimal(
                row.FirstOrDefault(kvp => kvp.Key.Contains("Completion", StringComparison.OrdinalIgnoreCase)).Value);
            var pct = completion is not null ? (int)Math.Round(completion > 1 ? completion.Value * 100 : completion.Value) : (int?)null;
            var (buyerId, buyerName) = ResolveHoldoverBuyer(row, colMap);

            templates.Add(new HoldoverTemplate(
                MapAreaToPillar(area),
                row.FirstOrDefault(kvp => kvp.Key.Contains("Action", StringComparison.OrdinalIgnoreCase)).Value ?? "Holdover action",
                rank++,
                MapHoldoverStatus(row),
                row.FirstOrDefault(kvp => kvp.Key.Contains("Expected", StringComparison.OrdinalIgnoreCase)).Value ?? "",
                row.FirstOrDefault(kvp => kvp.Key.Contains("Owner", StringComparison.OrdinalIgnoreCase)).Value,
                row.FirstOrDefault(kvp => kvp.Key.Contains("Owner", StringComparison.OrdinalIgnoreCase)).Value,
                row.FirstOrDefault(kvp => kvp.Key.Contains("Expected", StringComparison.OrdinalIgnoreCase)).Value,
                row.FirstOrDefault(kvp => kvp.Key.Contains("Current", StringComparison.OrdinalIgnoreCase)).Value,
                pct is >= 0 and <= 100 ? pct : null,
                buyerId,
                buyerName));
        }

        return templates;
    }

    private sealed record HoldoverTemplate(
        string PillarCode,
        string DriverName,
        int Rank,
        string Status,
        string WhyItMatters,
        string? Owner,
        string? AssignedSummary,
        string? TargetSummary,
        string? CurrentSummary,
        int? FixProgressPercent,
        string? Dimension1,
        string? Dimension2);

    private async Task<int> ImportOperationalIssuesAsync(
        WorkbookDetectionResult detection,
        Dictionary<string, ParsedSheet> sheetByName,
        Tenant tenant,
        DateOnly period,
        CancellationToken ct)
    {
        var issueSheet = detection.Sheets.FirstOrDefault(s => s.Kind == WorkbookSheetKind.OperationalIssues);
        if (issueSheet is null || !sheetByName.TryGetValue(issueSheet.SheetName, out var sheet))
            return 0;

        _db.ActionItems.RemoveRange(_db.ActionItems.Where(a => a.TenantId == tenant.Id && a.PeriodEnd == period));

        foreach (var row in sheet.Rows)
        {
            var status = row.FirstOrDefault(kvp => kvp.Key.Contains("Open", StringComparison.OrdinalIgnoreCase)).Value ?? "";
            if (status.Contains("Closed", StringComparison.OrdinalIgnoreCase)) continue;

            var title = row.FirstOrDefault(kvp => kvp.Key.Contains("Description", StringComparison.OrdinalIgnoreCase)).Value
                ?? row.FirstOrDefault(kvp => kvp.Key.Contains("Issue_Type", StringComparison.OrdinalIgnoreCase)).Value
                ?? "Operational issue";

            _db.ActionItems.Add(new ActionItem
            {
                TenantId = tenant.Id,
                PeriodEnd = period,
                Title = title.Length > 200 ? title[..200] : title,
                Owner = row.FirstOrDefault(kvp => kvp.Key.Contains("Owner", StringComparison.OrdinalIgnoreCase)).Value ?? "Unassigned",
                Status = ActionStatuses.InProgress,
                Priority = 50,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
        return sheet.Rows.Count;
    }

    private static void ValidateExpectedKpis(
        WorkbookDetectionResult detection,
        List<UploadBatchIssue> issues,
        long batchId)
    {
        var expected = detection.Sheets.FirstOrDefault(s => s.Kind == WorkbookSheetKind.ExpectedKpiValidation);
        if (expected is null) return;

        issues.Add(new UploadBatchIssue
        {
            UploadBatchId = batchId,
            Severity = UploadIssueSeverity.Info,
            Category = "ExpectedKpi",
            Message = "Workbook contains expected KPI outcomes for post-import comparison.",
            Field = expected.SheetName
        });
    }

    private static string MapAreaToPillar(string area)
    {
        var a = area.ToLowerInvariant();
        if (a.Contains("inventory")) return "DOH";
        if (a.Contains("ar") || a.Contains("receivable")) return "AR_PastDue31p%";
        if (a.Contains("margin")) return "GrossMargin%";
        if (a.Contains("vendor")) return "PerfectOrderRate";
        if (a.Contains("ap") || a.Contains("payable")) return "AP_PastDue31p%";
        return "CCC";
    }

    /// <summary>Last week that has sales/rollup in the workbook — not max of spurious invoice-derived dates.</summary>
    internal static DateOnly ResolveLatestOperationalPeriod(
        WorkbookDetectionResult detection,
        Dictionary<string, ParsedSheet> sheetByName,
        IReadOnlyList<DateOnly> periods)
    {
        var fromSales = new HashSet<DateOnly>();
        var salesSheet = detection.Sheets.FirstOrDefault(s => s.Kind == WorkbookSheetKind.Sales);
        if (salesSheet is not null && sheetByName.TryGetValue(salesSheet.SheetName, out var sp))
        {
            foreach (var row in sp.Rows)
            {
                var d = ColumnSynonymMatcher.ResolveRowPeriod(row, salesSheet.ColumnMappings);
                if (d is not null) fromSales.Add(d.Value);
            }
        }

        if (fromSales.Count > 0)
            return fromSales.Max();

        var rollupSheet = detection.Sheets.FirstOrDefault(s => s.Kind == WorkbookSheetKind.WeeklyRollup);
        if (rollupSheet is not null && sheetByName.TryGetValue(rollupSheet.SheetName, out var rp))
        {
            var fromRollup = new HashSet<DateOnly>();
            foreach (var row in rp.Rows)
            {
                var d = WorkbookDateRules.TryParsePeriodDate(
                    ColumnSynonymMatcher.GetMapped(row, rollupSheet.ColumnMappings, "Period_End_Date"));
                if (d is not null) fromRollup.Add(d.Value);
            }
            if (fromRollup.Count > 0)
                return fromRollup.Max();
        }

        return periods.Max();
    }

    private static (string? Id, string? Name) ResolveHoldoverBuyer(
        IReadOnlyDictionary<string, string?> row,
        IReadOnlyDictionary<string, string> colMap)
    {
        var customerId = ColumnSynonymMatcher.GetMapped(row, colMap, "Customer_ID")
            ?? FindCellByHeaderToken(row, "customerid", "custid", "accountid", "buyerid");
        var customerName = ColumnSynonymMatcher.GetMapped(row, colMap, "Customer_Name")
            ?? FindCellByHeaderToken(row, "customername", "custname", "accountname", "buyername");
        return CustomerKeyResolver.Resolve(customerId, customerName);
    }

    private static string? FindCellByHeaderToken(
        IReadOnlyDictionary<string, string?> row,
        params string[] tokens)
    {
        foreach (var kvp in row)
        {
            var norm = WorkbookParseHelper.NormalizeHeader(kvp.Key);
            if (tokens.Any(t => norm.Contains(t, StringComparison.Ordinal)))
                return kvp.Value;
        }
        return null;
    }

    private static string MapHoldoverStatus(IReadOnlyDictionary<string, string?> row)
    {
        var s = row.FirstOrDefault(kvp => kvp.Key.Contains("Status", StringComparison.OrdinalIgnoreCase)).Value ?? "";
        return s.Contains("Behind", StringComparison.OrdinalIgnoreCase) ? "RED"
            : s.Contains("Complete", StringComparison.OrdinalIgnoreCase) ? "GREEN"
            : "YELLOW";
    }
}
