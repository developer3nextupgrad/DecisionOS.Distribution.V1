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
    private readonly IWeeklyScoringService _weeklyScoring;

    public UploadBatchImportService(
        DecisionOsDbContext db,
        IWeeklyScoringService weeklyScoring)
    {
        _db = db;
        _weeklyScoring = weeklyScoring;
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

        var confidence = batch.ReadinessStatus switch
        {
            "ReadyToRun" => "High",
            "ReadyWithLimitations" => "Medium",
            _ => "Low"
        };

        var scoreResult = await _weeklyScoring.ScorePeriodAsync(new WeeklyScoringRequest
        {
            TenantId = tenant.Id,
            PeriodEnd = batch.PeriodEnd,
            UploadBatchId = batch.Id,
            DataConfidence = confidence
        }, ct);

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
            KpiRowsProcessed = scoreResult.SnapshotsWritten,
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

}

