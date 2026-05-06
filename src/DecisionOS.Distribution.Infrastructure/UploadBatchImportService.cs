using System.Globalization;
using System.Text.Json;
using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Normalized;
using DecisionOS.Distribution.Domain.Uploads;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Infrastructure;

public sealed class UploadBatchImportService
{
    private readonly DecisionOsDbContext _db;
    private readonly IKpiStatusService _kpiStatusService;
    private readonly IAlertService _alertService;
    private readonly IWeeklyFocusService _weeklyFocusService;

    public UploadBatchImportService(
        DecisionOsDbContext db,
        IKpiStatusService kpiStatusService,
        IAlertService alertService,
        IWeeklyFocusService weeklyFocusService)
    {
        _db = db;
        _kpiStatusService = kpiStatusService;
        _alertService = alertService;
        _weeklyFocusService = weeklyFocusService;
    }

    public async Task ValidateAsync(long batchId, string contentRootPath, CancellationToken ct = default)
    {
        var batch = await _db.UploadBatches.FirstOrDefaultAsync(b => b.Id == batchId, ct);
        if (batch is null) return;

        var files = await _db.UploadedFiles.AsNoTracking().Where(f => f.UploadBatchId == batch.Id).ToListAsync(ct);
        var fileIds = files.Select(f => f.Id).ToList();
        var maps = await _db.UploadedFileColumnMaps.AsNoTracking().Where(m => fileIds.Contains(m.UploadedFileId)).ToListAsync(ct);

        var issues = new List<UploadBatchIssue>();

        foreach (var f in files)
        {
            var header = await ReadCsvHeaderAsync(contentRootPath, f, ct);
            if (header.Count == 0)
            {
                issues.Add(new UploadBatchIssue
                {
                    UploadBatchId = batch.Id,
                    UploadedFileId = f.Id,
                    Severity = UploadIssueSeverity.Critical,
                    Category = "File",
                    Message = "File has no readable header row.",
                    Field = "header"
                });
                continue;
            }

            var fileMaps = maps.Where(m => m.UploadedFileId == f.Id).ToList();
            if (fileMaps.Count == 0)
            {
                issues.Add(new UploadBatchIssue
                {
                    UploadBatchId = batch.Id,
                    UploadedFileId = f.Id,
                    Severity = UploadIssueSeverity.Warning,
                    Category = "Mapping",
                    Message = "No mappings saved for this file yet.",
                    Field = "mapping"
                });
            }

            var mappedFields = fileMaps
                .Where(m => !m.Ignore && !string.IsNullOrWhiteSpace(m.SystemField))
                .Select(m => m.SystemField!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var req in RequiredFields.Required(f.ReportType))
            {
                if (!mappedFields.Contains(req))
                {
                    issues.Add(new UploadBatchIssue
                    {
                        UploadBatchId = batch.Id,
                        UploadedFileId = f.Id,
                        Severity = UploadIssueSeverity.Critical,
                        Category = "RequiredField",
                        Message = $"Missing required field mapping: {req}.",
                        Field = req
                    });
                }
            }

            foreach (var pref in RequiredFields.StronglyPreferred(f.ReportType))
            {
                if (!mappedFields.Contains(pref))
                {
                    issues.Add(new UploadBatchIssue
                    {
                        UploadBatchId = batch.Id,
                        UploadedFileId = f.Id,
                        Severity = UploadIssueSeverity.Warning,
                        Category = "PreferredField",
                        Message = $"Missing strongly preferred field mapping: {pref}.",
                        Field = pref
                    });
                }
            }
        }

        foreach (var rt in RequiredFields.MinimumPackageForV1)
        {
            if (!files.Any(f => f.ReportType == rt))
            {
                issues.Add(new UploadBatchIssue
                {
                    UploadBatchId = batch.Id,
                    Severity = UploadIssueSeverity.Warning,
                    Category = "Package",
                    Message = $"Missing report type for V1 minimum package: {rt}. Scoring will run with limitations.",
                    Field = rt.ToString()
                });
            }
        }

        _db.UploadBatchIssues.RemoveRange(_db.UploadBatchIssues.Where(i => i.UploadBatchId == batch.Id));
        _db.UploadBatchIssues.AddRange(issues);

        var hasCritical = issues.Any(i => i.Severity == UploadIssueSeverity.Critical);
        var hasWarnings = issues.Any(i => i.Severity == UploadIssueSeverity.Warning);
        batch.ReadinessStatus = hasCritical ? "NotReadyYet" : hasWarnings ? "ReadyWithLimitations" : "ReadyToRun";
        batch.Status = UploadBatchStatuses.Validated;
        batch.ValidationSummary =
            $"Readiness={batch.ReadinessStatus}; Critical={issues.Count(i => i.Severity == UploadIssueSeverity.Critical)}; Warning={issues.Count(i => i.Severity == UploadIssueSeverity.Warning)}; Info={issues.Count(i => i.Severity == UploadIssueSeverity.Info)}";

        await _db.SaveChangesAsync(ct);
    }

    public async Task RunImportAsync(long batchId, string contentRootPath, CancellationToken ct = default)
    {
        var batch = await _db.UploadBatches.FirstOrDefaultAsync(b => b.Id == batchId, ct);
        if (batch is null) return;

        // Be defensive: some callers may trigger RunImport without validating first (or with stale readiness).
        // We validate again so "Run import" either runs or fails with a deterministic validation summary.
        await ValidateAsync(batchId, contentRootPath, ct);
        batch = await _db.UploadBatches.FirstOrDefaultAsync(b => b.Id == batchId, ct);
        if (batch is null) return;

        if (string.Equals(batch.ReadinessStatus, "NotReadyYet", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Batch is NotReadyYet. {batch.ValidationSummary}");

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == batch.TenantId, ct);
        if (tenant is null) return;

        var files = await _db.UploadedFiles.AsNoTracking().Where(f => f.UploadBatchId == batch.Id).ToListAsync(ct);
        var fileIds = files.Select(f => f.Id).ToList();
        var maps = await _db.UploadedFileColumnMaps.AsNoTracking().Where(m => fileIds.Contains(m.UploadedFileId)).ToListAsync(ct);

        _db.NormalizedSalesRows.RemoveRange(_db.NormalizedSalesRows.Where(r => r.UploadBatchId == batch.Id));
        _db.NormalizedInventoryRows.RemoveRange(_db.NormalizedInventoryRows.Where(r => r.UploadBatchId == batch.Id));
        _db.NormalizedArRows.RemoveRange(_db.NormalizedArRows.Where(r => r.UploadBatchId == batch.Id));
        _db.NormalizedApRows.RemoveRange(_db.NormalizedApRows.Where(r => r.UploadBatchId == batch.Id));
        await _db.SaveChangesAsync(ct);

        foreach (var f in files)
        {
            var header = await ReadCsvHeaderAsync(contentRootPath, f, ct);
            if (header.Count == 0) continue;

            var mapByCol = maps
                .Where(m => m.UploadedFileId == f.Id)
                .ToDictionary(m => m.SourceColumn, m => m, StringComparer.OrdinalIgnoreCase);

            var fullPath = Path.Combine(contentRootPath, f.StoredRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath)) continue;

            var lines = await File.ReadAllLinesAsync(fullPath, ct);
            var headerIdx = Math.Clamp(f.HeaderRowNumber - 1, 0, Math.Max(0, lines.Length - 1));
            for (var i = headerIdx + 1; i < lines.Length; i++)
            {
                var rowNumber = i + 1;
                var values = SplitCsv(lines[i]);
                if (values.Count == 1 && string.IsNullOrWhiteSpace(values[0])) continue;

                var raw = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                for (var c = 0; c < header.Count; c++)
                    raw[header[c]] = c < values.Count ? values[c] : null;

                string? GetField(string sys)
                {
                    var source = mapByCol
                        .Where(kvp => !kvp.Value.Ignore && string.Equals(kvp.Value.SystemField, sys, StringComparison.OrdinalIgnoreCase))
                        .Select(kvp => kvp.Key)
                        .FirstOrDefault();
                    if (source is null) return null;
                    return raw.TryGetValue(source, out var v) ? v : null;
                }

                var rawJson = JsonSerializer.Serialize(raw);

                switch (f.ReportType)
                {
                    case ReportType.Sales:
                        _db.NormalizedSalesRows.Add(new NormalizedSalesRow
                        {
                            TenantId = tenant.Id,
                            PeriodEnd = batch.PeriodEnd,
                            UploadBatchId = batch.Id,
                            UploadedFileId = f.Id,
                            SourceRowNumber = rowNumber,
                            Status = RowStatus.Valid,
                            RawJson = rawJson,
                            TransactionDate = TryDate(GetField("Transaction_Date")) ?? batch.PeriodEnd,
                            QuantitySold = TryDecimal(GetField("Quantity_Sold")),
                            NetSales = TryDecimal(GetField("Net_Sales")),
                            Cogs = TryDecimal(GetField("COGS"))
                        });
                        break;
                    case ReportType.Inventory:
                        _db.NormalizedInventoryRows.Add(new NormalizedInventoryRow
                        {
                            TenantId = tenant.Id,
                            PeriodEnd = batch.PeriodEnd,
                            UploadBatchId = batch.Id,
                            UploadedFileId = f.Id,
                            SourceRowNumber = rowNumber,
                            Status = RowStatus.Valid,
                            RawJson = rawJson,
                            SnapshotDate = TryDate(GetField("Snapshot_Date")),
                            QuantityOnHand = TryDecimal(GetField("Quantity_On_Hand")),
                            InventoryValue = TryDecimal(GetField("Inventory_Value"))
                        });
                        break;
                    case ReportType.AccountsReceivable:
                        _db.NormalizedArRows.Add(new NormalizedArRow
                        {
                            TenantId = tenant.Id,
                            PeriodEnd = batch.PeriodEnd,
                            UploadBatchId = batch.Id,
                            UploadedFileId = f.Id,
                            SourceRowNumber = rowNumber,
                            Status = RowStatus.Valid,
                            RawJson = rawJson,
                            SnapshotDate = TryDate(GetField("AR_Snapshot_Date")),
                            DaysPastDue = TryInt(GetField("Days_Past_Due")),
                            AgingBucket = TrimOrNull(GetField("Aging_Bucket")),
                            OpenBalance = TryDecimal(GetField("Open_Balance"))
                        });
                        break;
                    case ReportType.AccountsPayable:
                        _db.NormalizedApRows.Add(new NormalizedApRow
                        {
                            TenantId = tenant.Id,
                            PeriodEnd = batch.PeriodEnd,
                            UploadBatchId = batch.Id,
                            UploadedFileId = f.Id,
                            SourceRowNumber = rowNumber,
                            Status = RowStatus.Valid,
                            RawJson = rawJson,
                            SnapshotDate = TryDate(GetField("AP_Snapshot_Date")),
                            DaysPastDue = TryInt(GetField("Days_Past_Due")),
                            AgingBucket = TrimOrNull(GetField("Aging_Bucket")),
                            OpenBalance = TryDecimal(GetField("Open_Balance"))
                        });
                        break;
                }
            }
        }

        await _db.SaveChangesAsync(ct);

        var kpiDefs = await _db.KpiDefinitions.AsNoTracking().ToListAsync(ct);
        var defsByCode = kpiDefs.ToDictionary(d => d.Code, StringComparer.OrdinalIgnoreCase);

        var confidence = batch.ReadinessStatus switch
        {
            "ReadyToRun" => "High",
            "ReadyWithLimitations" => "Medium",
            _ => "Low"
        };

        async Task<decimal?> GrossMarginPct()
        {
            var rows = await _db.NormalizedSalesRows.AsNoTracking().Where(r => r.UploadBatchId == batch.Id).ToListAsync(ct);
            var net = rows.Where(r => r.NetSales is not null).Sum(r => r.NetSales!.Value);
            var cogs = rows.Where(r => r.Cogs is not null).Sum(r => r.Cogs!.Value);
            if (net <= 0) return null;
            if (cogs <= 0) return null;
            return (net - cogs) / net;
        }

        async Task<decimal?> ArPastDue31Pct()
        {
            var rows = await _db.NormalizedArRows.AsNoTracking().Where(r => r.UploadBatchId == batch.Id).ToListAsync(ct);
            var total = rows.Where(r => r.OpenBalance is not null).Sum(r => r.OpenBalance!.Value);
            if (total <= 0) return null;
            var past = rows.Where(r => IsPastDue31(r.DaysPastDue, r.AgingBucket) && r.OpenBalance is not null).Sum(r => r.OpenBalance!.Value);
            return past / total;
        }

        async Task<decimal?> ApPastDue31Pct()
        {
            var rows = await _db.NormalizedApRows.AsNoTracking().Where(r => r.UploadBatchId == batch.Id).ToListAsync(ct);
            var total = rows.Where(r => r.OpenBalance is not null).Sum(r => r.OpenBalance!.Value);
            if (total <= 0) return null;
            var past = rows.Where(r => IsPastDue31(r.DaysPastDue, r.AgingBucket) && r.OpenBalance is not null).Sum(r => r.OpenBalance!.Value);
            return past / total;
        }

        async Task<decimal?> DoH()
        {
            var invRows = await _db.NormalizedInventoryRows.AsNoTracking().Where(r => r.UploadBatchId == batch.Id).ToListAsync(ct);
            var inv = invRows.Where(r => r.InventoryValue is not null).Sum(r => r.InventoryValue!.Value);
            if (inv <= 0) return null;
            var salesRows = await _db.NormalizedSalesRows.AsNoTracking().Where(r => r.UploadBatchId == batch.Id).ToListAsync(ct);
            var salesCogs = salesRows.Where(r => r.Cogs is not null).Sum(r => r.Cogs!.Value);
            if (salesCogs <= 0) return null;
            var perDay = salesCogs / 7m;
            if (perDay <= 0) return null;
            return inv / perDay;
        }

        async Task<decimal?> CCC()
        {
            var salesRows = await _db.NormalizedSalesRows.AsNoTracking().Where(r => r.UploadBatchId == batch.Id).ToListAsync(ct);
            var netSales = salesRows.Where(r => r.NetSales is not null).Sum(r => r.NetSales!.Value);
            var cogs = salesRows.Where(r => r.Cogs is not null).Sum(r => r.Cogs!.Value);
            if (netSales <= 0 || cogs <= 0) return null;
            var arRows = await _db.NormalizedArRows.AsNoTracking().Where(r => r.UploadBatchId == batch.Id).ToListAsync(ct);
            var apRows = await _db.NormalizedApRows.AsNoTracking().Where(r => r.UploadBatchId == batch.Id).ToListAsync(ct);
            var ar = arRows.Where(r => r.OpenBalance is not null).Sum(r => r.OpenBalance!.Value);
            var ap = apRows.Where(r => r.OpenBalance is not null).Sum(r => r.OpenBalance!.Value);
            var dso = ar / (netSales / 7m);
            var dio = await DoH();
            var dpo = ap / (cogs / 7m);
            if (dio is null) return null;
            return dso + dio.Value - dpo;
        }

        var computed = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase)
        {
            ["GrossMargin%"] = await GrossMarginPct(),
            ["AR_PastDue31p%"] = await ArPastDue31Pct(),
            ["AP_PastDue31p%"] = await ApPastDue31Pct(),
            ["DOH"] = await DoH(),
            ["CCC"] = await CCC(),
            ["NetProfit%"] = null,
            ["PerfectOrderRate"] = null
        };

        _db.KpiSnapshots.RemoveRange(_db.KpiSnapshots.Where(s => s.TenantId == tenant.Id && s.PeriodEnd == batch.PeriodEnd));

        foreach (var kvp in computed)
        {
            if (!defsByCode.TryGetValue(kvp.Key, out var def)) continue;
            if (kvp.Value is null)
            {
                _db.KpiSnapshots.Add(new KpiSnapshot
                {
                    TenantId = tenant.Id,
                    PeriodEnd = batch.PeriodEnd,
                    KpiDefinitionId = def.Id,
                    Value = 0m,
                    Status = "GRAY",
                    DataConfidence = "Low",
                    CardDetailLine1 = "Insufficient data from uploaded package.",
                    CardDetailLine2 = "Add missing fields/files to enable scoring."
                });
            }
            else
            {
                var value = kvp.Value.Value;
                _db.KpiSnapshots.Add(new KpiSnapshot
                {
                    TenantId = tenant.Id,
                    PeriodEnd = batch.PeriodEnd,
                    KpiDefinitionId = def.Id,
                    Value = value,
                    Status = _kpiStatusService.ComputeStatus(def, value),
                    DataConfidence = confidence
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        var snapshots = await _db.KpiSnapshots.Include(s => s.KpiDefinition)
            .Where(s => s.TenantId == tenant.Id && s.PeriodEnd == batch.PeriodEnd)
            .ToListAsync(ct);

        var topAlert = _alertService.SelectTopAlert(tenant.Id, batch.PeriodEnd, snapshots, kpiDefs);
        _db.Alerts.RemoveRange(_db.Alerts.Where(a => a.TenantId == tenant.Id && a.PeriodEnd == batch.PeriodEnd));
        if (topAlert is not null) _db.Alerts.Add(topAlert);

        _db.WeeklyFocuses.RemoveRange(_db.WeeklyFocuses.Where(f => f.TenantId == tenant.Id && f.PeriodEnd == batch.PeriodEnd));
        var focus = _weeklyFocusService.GenerateWeeklyFocus(tenant.Id, batch.PeriodEnd, topAlert, kpiDefs);
        if (focus is not null) _db.WeeklyFocuses.Add(focus);

        await _db.SaveChangesAsync(ct);

        var fp = UploadedFile.ComputeSha256Hex(string.Join("|",
            files.OrderBy(f => f.ReportType).Select(f => $"{f.ReportType}:{f.Sha256Hex}:{f.HeaderRowNumber}")));
        var run = new ImportRun
        {
            TenantId = tenant.Id,
            PeriodEnd = batch.PeriodEnd,
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
            Status = "Completed",
            SourceFingerprint = fp,
            ValidationSummary = batch.ValidationSummary,
            ReadinessStatus = batch.ReadinessStatus,
            KpiRowsProcessed = snapshots.Count,
            DriverRowsProcessed = 0
        };
        _db.ImportRuns.Add(run);
        await _db.SaveChangesAsync(ct);

        batch.ImportRunId = run.Id;
        batch.Status = UploadBatchStatuses.Imported;
        await _db.SaveChangesAsync(ct);
    }

    private static async Task<IReadOnlyList<string>> ReadCsvHeaderAsync(string contentRootPath, UploadedFile file, CancellationToken ct)
    {
        var fullPath = Path.Combine(contentRootPath, file.StoredRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath)) return Array.Empty<string>();
        var lines = await File.ReadAllLinesAsync(fullPath, ct);
        var idx = Math.Clamp(file.HeaderRowNumber - 1, 0, Math.Max(0, lines.Length - 1));
        var headerLine = lines.Length == 0 ? "" : lines[idx];
        return SplitCsv(headerLine).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
    }

    private static List<string> SplitCsv(string line)
    {
        var res = new List<string>();
        var cur = "";
        var inQ = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                inQ = !inQ;
                continue;
            }
            if (ch == ',' && !inQ)
            {
                res.Add(cur.Trim());
                cur = "";
                continue;
            }
            cur += ch;
        }
        res.Add(cur.Trim());
        return res;
    }

    private static DateOnly? TryDate(string? raw)
        => DateOnly.TryParse(raw?.Trim(), out var d) ? d : null;

    private static decimal? TryDecimal(string? raw)
        => decimal.TryParse(raw?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static int? TryInt(string? raw)
        => int.TryParse(raw?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static string? TrimOrNull(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static bool IsPastDue31(int? daysPastDue, string? bucket)
    {
        if (daysPastDue is >= 31) return true;
        if (string.IsNullOrWhiteSpace(bucket)) return false;
        var b = bucket.Trim().ToLowerInvariant();
        return b.Contains("31") || b.Contains("60") || b.Contains("90") || b.Contains("past due") || b.Contains("over");
    }
}

