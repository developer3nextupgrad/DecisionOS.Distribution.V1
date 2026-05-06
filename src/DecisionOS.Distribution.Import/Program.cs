using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Import;
using DecisionOS.Distribution.Infrastructure;
using ExcelDataReader;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var rawArgs = args.ToList();
var forceReimport = rawArgs.RemoveAll(x => string.Equals(x, "--force", StringComparison.OrdinalIgnoreCase)) > 0;

if (rawArgs.Count >= 2 && string.Equals(rawArgs[0], "inspect", StringComparison.OrdinalIgnoreCase))
{
    var path = rawArgs[1];
    var rows = 10;
    var sheetIndex = 0;
    for (var i = 2; i < rawArgs.Count; i++)
    {
        if (string.Equals(rawArgs[i], "--rows", StringComparison.OrdinalIgnoreCase) && i + 1 < rawArgs.Count &&
            int.TryParse(rawArgs[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var r))
        {
            rows = Math.Clamp(r, 1, 200);
            i++;
            continue;
        }
        if (string.Equals(rawArgs[i], "--sheet", StringComparison.OrdinalIgnoreCase) && i + 1 < rawArgs.Count &&
            int.TryParse(rawArgs[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var s))
        {
            sheetIndex = Math.Max(0, s);
            i++;
        }
    }

    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    DumpTabularPreview(path, sheetIndex, rows);
    return;
}

if (rawArgs.Count < 3)
{
    Console.WriteLine("Usage: DecisionOS.Distribution.Import <client_id> <period_end:YYYY-MM-DD> <kpi_path(.csv|.xlsx|.xls)> [drivers_path(.csv|.xlsx|.xls)] [--force]");
    Console.WriteLine("       DecisionOS.Distribution.Import inspect <path(.csv|.xlsx|.xls)> [--sheet N] [--rows N]");
    return;
}

var clientId = rawArgs[0];
if (!DateOnly.TryParse(rawArgs[1], out var periodEnd))
{
    Console.WriteLine("Invalid period_end. Expected YYYY-MM-DD.");
    return;
}

var kpiPath = rawArgs[2];
var driversPath = rawArgs.Count > 3 ? rawArgs[3] : string.Empty;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var tempFilesToDelete = new List<string>();
try
{
    kpiPath = NormalizeToCsvPath(kpiPath, tempFilesToDelete);
    driversPath = string.IsNullOrWhiteSpace(driversPath) ? string.Empty : NormalizeToCsvPath(driversPath, tempFilesToDelete);

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DecisionOs")
    ?? "Host=localhost;Port=5432;Database=decisionos;Username=decisionos;Password=decisionos";

var optionsBuilder = new DbContextOptionsBuilder<DecisionOsDbContext>();
optionsBuilder.UseNpgsql(connectionString);

await using var db = new DecisionOsDbContext(optionsBuilder.Options);
await db.Database.MigrateAsync();

await SeedVerticalLibrariesIfNeeded(db);
await SeedBusinessProfilesIfNeeded(db);

var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.ClientId == clientId);
if (tenant is null)
{
    var defaultProfileId = await db.BusinessProfiles
        .Where(p => p.Code == "DISTRIBUTION_DEFAULT")
        .Select(p => (Guid?)p.Id)
        .FirstOrDefaultAsync();

    tenant = new Tenant
    {
        Id = Guid.NewGuid(),
        ClientId = clientId,
        Name = clientId,
        BusinessProfileId = defaultProfileId
    };
    db.Tenants.Add(tenant);
    await db.SaveChangesAsync();
}
await SeedKpiDefinitionsIfNeeded(db);
await SyncKpiDefinitionPriorities(db);
await SeedDriverDefinitionsIfNeeded(db);

var fingerprint = ComputeImportFingerprint(clientId, periodEnd, kpiPath, driversPath);

if (!forceReimport && await db.ImportRuns.AsNoTracking().AnyAsync(r =>
        r.TenantId == tenant.Id &&
        r.PeriodEnd == periodEnd &&
        r.Status == "Completed" &&
        r.SourceFingerprint == fingerprint))
{
    Console.WriteLine("Idempotent skip: identical sources were already imported successfully. Pass --force to re-run.");
    return;
}

var importRun = new ImportRun
{
    TenantId = tenant.Id,
    PeriodEnd = periodEnd,
    StartedAt = DateTimeOffset.UtcNow,
    Status = "Running",
    KpiRowsProcessed = 0,
    DriverRowsProcessed = 0,
    SourceFingerprint = fingerprint
};
db.ImportRuns.Add(importRun);
await db.SaveChangesAsync();

var kpiStatusService = new KpiStatusService();
var alertService = new AlertService();
var weeklyFocusService = new WeeklyFocusService();
var definitionResolver = new DefinitionResolver(db);

try
{
    ValidateKpiCsvHeaders(kpiPath);
    if (!string.IsNullOrWhiteSpace(driversPath))
    {
        var requireDriverCode = await RequiresDriverCodeForTenantAsync(db, tenant);
        await ValidateDriverCsvHeadersAsync(driversPath, requireDriverCode);
    }

    var validations = new List<(string Label, ImportValidationResult Result)>();

    var kpiValidation = new ImportValidationResult();
    validations.Add(("KPI", kpiValidation));
    importRun.KpiRowsProcessed = await ImportKpisAsync(db, tenant, periodEnd, kpiPath, kpiStatusService, kpiValidation, definitionResolver);
    if (!kpiValidation.IsValid)
        throw new InvalidOperationException(kpiValidation.FormatMessages());

    if (!string.IsNullOrWhiteSpace(driversPath))
    {
        var driverValidation = new ImportValidationResult();
        validations.Add(("Driver", driverValidation));
        importRun.DriverRowsProcessed = await ImportDriversAsync(db, tenant, periodEnd, driversPath, driverValidation, definitionResolver);
        if (!driverValidation.IsValid)
            throw new InvalidOperationException(driverValidation.FormatMessages());
    }

    PersistValidation(importRun, validations);
    await db.SaveChangesAsync();
    await PersistValidationIssuesAsync(db, importRun, validations.Select(v => v.Result));

    var previousPeriodEnd = periodEnd.AddDays(-7);
    var currentSnapshots = await db.KpiSnapshots
        .Where(s => s.TenantId == tenant.Id && s.PeriodEnd == periodEnd)
        .ToListAsync();
    var previousSnapshots = await db.KpiSnapshots
        .Where(s => s.TenantId == tenant.Id && s.PeriodEnd == previousPeriodEnd)
        .ToDictionaryAsync(s => s.KpiDefinitionId);

    foreach (var snapshot in currentSnapshots)
    {
        if (previousSnapshots.TryGetValue(snapshot.KpiDefinitionId, out var prev))
            snapshot.WeekOverWeekDelta = snapshot.Value - prev.Value;
    }

    var confidence = importRun.ReadinessStatus switch
    {
        "ReadyToRun" => "High",
        "ReadyWithLimitations" => "Medium",
        _ => "Low"
    };
    foreach (var snapshot in currentSnapshots)
        snapshot.DataConfidence = confidence;

    await db.SaveChangesAsync();

    var snapshots = await db.KpiSnapshots
        .Include(s => s.KpiDefinition)
        .Where(s => s.TenantId == tenant.Id && s.PeriodEnd == periodEnd)
        .ToListAsync();
    var definitions = await db.KpiDefinitions.ToListAsync();

    var topAlert = alertService.SelectTopAlert(tenant.Id, periodEnd, snapshots, definitions);
    if (topAlert is not null)
    {
        var existingAlerts = db.Alerts.Where(a => a.TenantId == tenant.Id && a.PeriodEnd == periodEnd);
        db.Alerts.RemoveRange(existingAlerts);
        db.Alerts.Add(topAlert);
    }

    var weeklyFocus = weeklyFocusService.GenerateWeeklyFocus(tenant.Id, periodEnd, topAlert, definitions);
    if (weeklyFocus is not null)
    {
        var existingFocuses = db.WeeklyFocuses.Where(f => f.TenantId == tenant.Id && f.PeriodEnd == periodEnd);
        db.WeeklyFocuses.RemoveRange(existingFocuses);
        db.WeeklyFocuses.Add(weeklyFocus);
    }

    await db.SaveChangesAsync();

    // Ensure at least one actionable execution item exists for the week (do not overwrite operator edits).
    if (topAlert is not null)
    {
        var existingAction = await db.ActionItems.FirstOrDefaultAsync(a =>
            a.TenantId == tenant.Id &&
            a.PeriodEnd == periodEnd &&
            a.KpiDefinitionId == topAlert.KpiDefinitionId);

        if (existingAction is null)
        {
            var def = definitions.FirstOrDefault(d => d.Id == topAlert.KpiDefinitionId);
            db.ActionItems.Add(new ActionItem
            {
                TenantId = tenant.Id,
                PeriodEnd = periodEnd,
                KpiDefinitionId = topAlert.KpiDefinitionId,
                Title = def is null ? "Weekly priority action" : $"Fix {def.Name}",
                Description = weeklyFocus?.RecommendedAction,
                Owner = weeklyFocus?.Owner ?? "Operations",
                DueDate = periodEnd.AddDays(7),
                Status = ActionStatuses.NotStarted,
                Priority = def?.AlertPriority ?? 50,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
    }

    await db.SaveChangesAsync();

    importRun.Status = "Completed";
    await db.SaveChangesAsync();
    Console.WriteLine("Import completed.");
}
catch (Exception ex)
{
    importRun.Status = "Failed";
    importRun.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
    Console.WriteLine($"Import failed: {ex.Message}");
    throw;
}
finally
{
    importRun.CompletedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync();
}
}
finally
{
    foreach (var p in tempFilesToDelete)
    {
        try { File.Delete(p); } catch { /* ignore */ }
    }
}

static void PersistValidation(ImportRun run, IReadOnlyList<(string Label, ImportValidationResult Result)> results)
{
    var hasCritical = results.Any(r => r.Result.HasCritical);
    var hasWarnings = results.Any(r => r.Result.HasWarnings);

    var readiness = hasCritical
        ? "NotReadyYet"
        : hasWarnings
            ? "ReadyWithLimitations"
            : "ReadyToRun";

    run.ReadinessStatus = readiness;

    var allIssues = results.SelectMany(r => r.Result.Issues).ToList();
    var criticalCount = allIssues.Count(i => i.Severity == ImportValidationSeverity.Critical);
    var warningCount = allIssues.Count(i => i.Severity == ImportValidationSeverity.Warning);
    var infoCount = allIssues.Count(i => i.Severity == ImportValidationSeverity.Info);

    var header = $"Readiness={readiness}; Critical={criticalCount}; Warning={warningCount}; Info={infoCount}";

    var sections = new List<string> { header };
    foreach (var (label, r) in results)
    {
        if (r.Issues.Count == 0) continue;
        sections.Add($"--- {label} issues ({r.Issues.Count}) ---");
        sections.Add(r.FormatMessages(maxLines: 200));
    }

    if (sections.Count == 1)
        sections.Add("No validation issues.");

    run.ValidationSummary = string.Join(Environment.NewLine, sections);
}

static async Task PersistValidationIssuesAsync(DecisionOsDbContext db, ImportRun run, IEnumerable<ImportValidationResult> results)
{
    // Replace prior exceptions for the run to keep the latest attempt canonical.
    var existing = db.ImportRunIssues.Where(x => x.ImportRunId == run.Id);
    db.ImportRunIssues.RemoveRange(existing);

    var issues = results.SelectMany(r => r.Issues).Select(i => new ImportRunIssue
    {
        ImportRunId = run.Id,
        Category = i.Category,
        Severity = i.Severity,
        Message = i.Message,
        RowNumber = i.RowNumber,
        Field = i.Field
    }).ToList();

    if (issues.Count > 0)
    {
        db.ImportRunIssues.AddRange(issues);
        await db.SaveChangesAsync();
    }
}

static string ComputeImportFingerprint(string clientId, DateOnly periodEnd, string kpiPath, string? driversPath)
{
    static string HashFile(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    var sb = new StringBuilder(256);
    sb.Append(clientId.Trim());
    sb.Append('|');
    sb.Append(periodEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    sb.Append('|');
    sb.Append(HashFile(kpiPath));
    if (!string.IsNullOrWhiteSpace(driversPath) && File.Exists(driversPath))
    {
        sb.Append('|');
        sb.Append(HashFile(driversPath));
    }

    return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()))).ToLowerInvariant();
}

static string NormalizeToCsvPath(string path, List<string> tempFilesToDelete)
{
    if (string.IsNullOrWhiteSpace(path))
        return path;

    if (!File.Exists(path))
        throw new FileNotFoundException($"File not found: {path}", path);

    var ext = Path.GetExtension(path);
    if (string.Equals(ext, ".csv", StringComparison.OrdinalIgnoreCase))
        return path;

    var isExcel = string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(ext, ".xls", StringComparison.OrdinalIgnoreCase);
    if (!isExcel)
        throw new InvalidOperationException($"Unsupported file type '{ext}'. Only .csv and .xlsx are supported.");

    var tmp = Path.Combine(Path.GetTempPath(), $"decisionos_import_{Guid.NewGuid():N}.csv");
    ConvertFirstWorksheetXlsxToCsv(path, tmp);
    tempFilesToDelete.Add(tmp);
    return tmp;
}

static void ConvertFirstWorksheetXlsxToCsv(string xlsxPath, string csvPath)
{
    using var stream = File.Open(xlsxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    using var reader = ExcelReaderFactory.CreateReader(stream);
    var data = reader.AsDataSet();
    if (data.Tables.Count == 0)
        throw new InvalidOperationException("Excel file has no worksheets.");

    var table = data.Tables[0];

    using var writer = new StreamWriter(csvPath, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = false,
        NewLine = Environment.NewLine
    });

    foreach (System.Data.DataRow row in table.Rows)
    {
        foreach (var cell in row.ItemArray)
            csv.WriteField(cell?.ToString());
        csv.NextRecord();
    }
}

static void DumpTabularPreview(string path, int sheetIndex, int maxRows)
{
    if (!File.Exists(path))
        throw new FileNotFoundException($"File not found: {path}", path);

    var ext = Path.GetExtension(path);
    if (string.Equals(ext, ".csv", StringComparison.OrdinalIgnoreCase))
    {
        var lines = File.ReadLines(path).Take(maxRows).ToList();
        foreach (var line in lines)
            Console.WriteLine(line);
        return;
    }

    var isExcel = string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(ext, ".xls", StringComparison.OrdinalIgnoreCase);
    if (!isExcel)
        throw new InvalidOperationException($"Unsupported file type '{ext}'. Only .csv, .xlsx, .xls are supported.");

    using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    using var reader = ExcelReaderFactory.CreateReader(stream);
    var data = reader.AsDataSet();
    if (data.Tables.Count == 0)
        throw new InvalidOperationException("Excel file has no worksheets.");

    if (sheetIndex >= data.Tables.Count)
        throw new InvalidOperationException($"Sheet index {sheetIndex} is out of range. Workbook has {data.Tables.Count} sheets.");

    var table = data.Tables[sheetIndex];

    using var csv = new CsvWriter(Console.Out, new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = false,
        NewLine = Environment.NewLine
    });

    var rows = Math.Min(maxRows, table.Rows.Count);
    for (var r = 0; r < rows; r++)
    {
        var row = table.Rows[r];
        foreach (var cell in row.ItemArray)
            csv.WriteField(cell?.ToString());
        csv.NextRecord();
    }
}

static async Task SeedDriverDefinitionsIfNeeded(DecisionOsDbContext db)
{
    if (await db.DriverDefinitions.AnyAsync()) return;

    IEnumerable<DriverDefinition> defs = new[]
    {
        new DriverDefinition { PillarCode = "AR_PastDue31p%", DriverCode = "ar_acme", DisplayName = "Acme Corp", SortOrder = 10 },
        new DriverDefinition { PillarCode = "AR_PastDue31p%", DriverCode = "ar_beta", DisplayName = "Beta Industries", SortOrder = 20 },
        new DriverDefinition { PillarCode = "AR_PastDue31p%", DriverCode = "ar_gamma", DisplayName = "Gamma LLC", SortOrder = 30 },
        new DriverDefinition { PillarCode = "AR_PastDue31p%", DriverCode = "ar_delta", DisplayName = "Delta Supply", SortOrder = 40 },
        new DriverDefinition { PillarCode = "AR_PastDue31p%", DriverCode = "ar_epsilon", DisplayName = "Epsilon Co", SortOrder = 50 },
        new DriverDefinition { PillarCode = "CCC", DriverCode = "ccc_dso", DisplayName = "Days Sales Outstanding", SortOrder = 10 },
        new DriverDefinition { PillarCode = "CCC", DriverCode = "ccc_dio", DisplayName = "Days Inventory Outstanding", SortOrder = 20 },
        new DriverDefinition { PillarCode = "CCC", DriverCode = "ccc_dpo", DisplayName = "Days Payable Outstanding", SortOrder = 30 },
        new DriverDefinition { PillarCode = "GrossMargin%", DriverCode = "gm_mix", DisplayName = "Product Mix", SortOrder = 10 },
        new DriverDefinition { PillarCode = "GrossMargin%", DriverCode = "gm_freight", DisplayName = "Freight Costs", SortOrder = 20 },
        new DriverDefinition { PillarCode = "GrossMargin%", DriverCode = "gm_discount", DisplayName = "Discounting", SortOrder = 30 },
        new DriverDefinition { PillarCode = "PerfectOrderRate", DriverCode = "por_late", DisplayName = "Late Shipments", SortOrder = 10 },
        new DriverDefinition { PillarCode = "DOH", DriverCode = "doh_slow", DisplayName = "Industrial slow movers", SortOrder = 10 }
    };

    db.DriverDefinitions.AddRange(defs);
    await db.SaveChangesAsync();
}

static async Task SeedBusinessProfilesIfNeeded(DecisionOsDbContext db)
{
    if (await db.BusinessProfiles.AnyAsync()) return;

    var distributionId = await db.VerticalLibraries
        .Where(v => v.Code == "DISTRIBUTION")
        .Select(v => v.Id)
        .FirstOrDefaultAsync();
    if (distributionId == Guid.Empty)
        distributionId = Guid.NewGuid();
    var defaultProfileId = Guid.NewGuid();
    db.BusinessProfiles.Add(new BusinessProfile
    {
        Id = defaultProfileId,
        VerticalLibraryId = distributionId,
        Code = "DISTRIBUTION_DEFAULT",
        Name = "Distribution (Default)",
        Description = "Default pilot KPI/driver standards for distribution-style businesses.",
        ActiveKpiProfileCode = "PILOT_7",
        LocationStructure = "single-location",
        ChannelStructure = "internal-external",
        ThresholdProfileCode = "PILOT_DEFAULT"
    });

    await db.SaveChangesAsync();
}

static async Task SeedVerticalLibrariesIfNeeded(DecisionOsDbContext db)
{
    if (await db.VerticalLibraries.AnyAsync()) return;
    db.VerticalLibraries.Add(new VerticalLibrary
    {
        Id = Guid.NewGuid(),
        Code = "DISTRIBUTION",
        Name = "Distribution",
        Description = "Distribution vertical library (broad business family)."
    });
    await db.SaveChangesAsync();
}

static async Task<bool> RequiresDriverCodeForTenantAsync(DecisionOsDbContext db, Tenant tenant)
{
    var profileId = tenant.BusinessProfileId;
    if (profileId is null)
        return await db.DriverDefinitions.AnyAsync(d => d.IsActive && d.BusinessProfileId == null);

    var hasProfileCatalog = await db.DriverDefinitions.AnyAsync(d => d.IsActive && d.BusinessProfileId == profileId);
    if (hasProfileCatalog) return true;

    return await db.DriverDefinitions.AnyAsync(d => d.IsActive && d.BusinessProfileId == null);
}

static async Task SeedKpiDefinitionsIfNeeded(DecisionOsDbContext db)
{
    if (await db.KpiDefinitions.AnyAsync()) return;

    var defs = new[]
    {
        new KpiDefinition
        {
            Code = "CCC",
            Name = "Cash Conversion Cycle (CCC)",
            Unit = "days",
            Direction = KpiDirection.LowerIsBetter,
            Target = 45m,
            AmberThreshold = 55m,
            RedThreshold = 70m,
            AlertPriority = 10,
            RecommendedAction = "Tighten cash cycle: accelerate collections, reduce slow inventory, and protect payables terms.",
            DiagnosticChecks = "Check DSO, DIO, DPO; focus on the biggest mover vs last week."
        },
        new KpiDefinition
        {
            Code = "GrossMargin%",
            Name = "Gross Margin %",
            Unit = "pct",
            Direction = KpiDirection.HigherIsBetter,
            Target = 0.28m,
            AmberThreshold = 0.265m,
            RedThreshold = 0.25m,
            AlertPriority = 40,
            RecommendedAction = "Protect margin: adjust pricing/discounts, attack freight-in/COGS leakage, and fix low-margin customers/SKUs.",
            DiagnosticChecks = "Review margin by customer, SKU, channel; look for discount creep and supplier cost changes."
        },
        new KpiDefinition
        {
            Code = "NetProfit%",
            Name = "Net Profit %",
            Unit = "pct",
            Direction = KpiDirection.HigherIsBetter,
            Target = 0.06m,
            AmberThreshold = 0.045m,
            RedThreshold = 0.03m,
            AlertPriority = 30,
            RecommendedAction = "Stabilize profit: control Opex, fix margin leakage, and prioritize high-contribution work.",
            DiagnosticChecks = "Decompose: GM$ and Opex; identify the top 2 expense overruns and top margin leaks."
        },
        new KpiDefinition
        {
            Code = "AR_PastDue31p%",
            Name = "A/R Health",
            Unit = "pct",
            Direction = KpiDirection.LowerIsBetter,
            Target = 0.12m,
            AmberThreshold = 0.15m,
            RedThreshold = 0.20m,
            AlertPriority = 20,
            RecommendedAction = "Stop overdue growth: implement collections cadence, tighten credit holds, and resolve dispute queues.",
            DiagnosticChecks = "Look at 31–60 and 90+ buckets; top 10 past-due accounts; dispute/short-pay reasons."
        },
        new KpiDefinition
        {
            Code = "DOH",
            Name = "Inventory Health (DOH)",
            Unit = "days",
            Direction = KpiDirection.LowerIsBetter,
            Target = 45m,
            AmberThreshold = 55m,
            RedThreshold = 70m,
            AlertPriority = 50,
            RecommendedAction = "Balance inventory: reduce excess (slow movers) and eliminate stockouts on top sellers.",
            DiagnosticChecks = "Check DOH by category; stockouts list; excess value; inbound lead-time variability."
        },
        new KpiDefinition
        {
            Code = "AP_PastDue31p%",
            Name = "A/P & Purchasing Efficiency",
            Unit = "pct",
            Direction = KpiDirection.LowerIsBetter,
            Target = 0.10m,
            AmberThreshold = 0.12m,
            RedThreshold = 0.18m,
            AlertPriority = 60,
            RecommendedAction = "Protect vendor relationships and cash: schedule payments, negotiate terms, and prevent 60+ drift.",
            DiagnosticChecks = "Check AP 31+ %, 60+ $, missed discounts, vendor holds, and upcoming large invoices."
        },
        new KpiDefinition
        {
            Code = "PerfectOrderRate",
            Name = "Service / Fulfillment (Perfect Order)",
            Unit = "pct",
            Direction = KpiDirection.HigherIsBetter,
            Target = 0.93m,
            AmberThreshold = 0.91m,
            RedThreshold = 0.89m,
            AlertPriority = 70,
            RecommendedAction = "Run service recovery: fix the biggest service driver (late, short, or damaged) and stabilize throughput.",
            DiagnosticChecks = "Decompose: On-time %, Fill rate %, Damage rate; identify top WH/carrier/SKU exceptions."
        }
    };

    db.KpiDefinitions.AddRange(defs);
    await db.SaveChangesAsync();
}

static async Task SyncKpiDefinitionPriorities(DecisionOsDbContext db)
{
    var priorityByCode = new Dictionary<string, int>
    {
        ["CCC"] = 10,
        ["AR_PastDue31p%"] = 20,
        ["NetProfit%"] = 30,
        ["GrossMargin%"] = 40,
        ["DOH"] = 50,
        ["AP_PastDue31p%"] = 60,
        ["PerfectOrderRate"] = 70
    };

    var defs = await db.KpiDefinitions.Where(d => priorityByCode.Keys.Contains(d.Code)).ToListAsync();
    foreach (var d in defs)
        d.AlertPriority = priorityByCode[d.Code];

    if (defs.Count > 0)
        await db.SaveChangesAsync();
}

static void ValidateKpiCsvHeaders(string path)
{
    var line = File.ReadLines(path).FirstOrDefault();
    if (string.IsNullOrWhiteSpace(line))
        throw new InvalidOperationException("KPI CSV has no header row.");

    var headers = line.Split(',').Select(s => s.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
    if (!headers.Contains("kpi_code") || !headers.Contains("value"))
        throw new InvalidOperationException("KPI CSV must include columns: kpi_code, value.");
}

static Task ValidateDriverCsvHeadersAsync(string path, bool requireDriverCode)
{
    var line = File.ReadLines(path).FirstOrDefault();
    if (string.IsNullOrWhiteSpace(line))
        throw new InvalidOperationException("Driver CSV has no header row.");

    var headers = line.Split(',').Select(s => s.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
    foreach (var req in new[] { "pillar_code", "driver_name", "current", "rank", "status", "why_it_matters" })
    {
        if (!headers.Contains(req))
            throw new InvalidOperationException($"Driver CSV must include column: {req}.");
    }

    if (requireDriverCode && !headers.Contains("driver_code"))
        throw new InvalidOperationException("Driver CSV must include driver_code when an active driver catalog exists.");

    return Task.CompletedTask;
}

static string? GetOptionalCsvField(IDictionary<string, object?> dict, string canonicalName)
{
    var key = dict.Keys.FirstOrDefault(k => string.Equals(k, canonicalName, StringComparison.OrdinalIgnoreCase));
    if (key is null) return null;
    var raw = dict[key]?.ToString()?.Trim();
    return string.IsNullOrEmpty(raw) ? null : raw;
}

// Definition resolution moved to Infrastructure.DefinitionResolver (unit-testable).

static async Task<int> ImportKpisAsync(
    DecisionOsDbContext db,
    Tenant tenant,
    DateOnly periodEnd,
    string cdfPath,
    IKpiStatusService statusService,
    ImportValidationResult validation,
    DefinitionResolver resolver)
{
    using var reader = new StreamReader(cdfPath);
    var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
    using var csv = new CsvReader(reader, config);

    var records = csv.GetRecords<dynamic>().ToList();
    var definitions = await resolver.ResolveKpiDefinitionsAsync(tenant);

    var row = 2;
    var pending = new List<(int DefId, decimal Value, string? L1, string? L2)>();
    foreach (var record in records)
    {
        var dict = (IDictionary<string, object?>)record;
        if (!dict.TryGetValue("kpi_code", out var codeObj) || codeObj is null || string.IsNullOrWhiteSpace(codeObj.ToString()))
        {
            row++;
            continue;
        }

        if (!dict.TryGetValue("value", out var valueObj) || valueObj is null)
        {
            validation.Add("KPI", "value is required when kpi_code is set.", row, "value", ImportValidationSeverity.Critical);
            row++;
            continue;
        }

        var code = codeObj.ToString()!.Trim();
        if (!definitions.TryGetValue(code, out var definition))
        {
            validation.Add("KPI", $"Unknown kpi_code '{code}'.", row, "kpi_code", ImportValidationSeverity.Critical);
            row++;
            continue;
        }

        if (!decimal.TryParse(valueObj.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            validation.Add("KPI", $"Cannot parse value for '{code}'.", row, "value", ImportValidationSeverity.Critical);
            row++;
            continue;
        }

        var issuesBefore = validation.Issues.Count;
        ImportRowValidator.ValidateKpiValue(definition, value, validation, row);
        if (validation.Issues.Count > issuesBefore)
        {
            row++;
            continue;
        }

        pending.Add((
            definition.Id,
            value,
            GetOptionalCsvField(dict, "card_detail_line1"),
            GetOptionalCsvField(dict, "card_detail_line2")));
        row++;
    }

    if (!validation.IsValid)
        return 0;

    foreach (var (defId, value, l1, l2) in pending)
    {
        var definition = definitions.Values.First(d => d.Id == defId);
        var status = statusService.ComputeStatus(definition, value);

        var existing = await db.KpiSnapshots
            .FirstOrDefaultAsync(s =>
                s.TenantId == tenant.Id &&
                s.PeriodEnd == periodEnd &&
                s.KpiDefinitionId == defId);

        if (existing is null)
        {
            existing = new KpiSnapshot
            {
                TenantId = tenant.Id,
                PeriodEnd = periodEnd,
                KpiDefinitionId = defId
            };
            db.KpiSnapshots.Add(existing);
        }

        existing!.Value = value;
        existing.Status = status;
        existing.CardDetailLine1 = l1;
        existing.CardDetailLine2 = l2;
    }

    await db.SaveChangesAsync();
    return pending.Count;
}

static async Task<int> ImportDriversAsync(
    DecisionOsDbContext db,
    Tenant tenant,
    DateOnly periodEnd,
    string csvPath,
    ImportValidationResult validation,
    DefinitionResolver resolver)
{
    using var reader = new StreamReader(csvPath);
    var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
    using var csv = new CsvReader(reader, config);

    var records = csv.GetRecords<dynamic>().ToList();

    var existing = db.DriverValues.Where(d => d.TenantId == tenant.Id && d.PeriodEnd == periodEnd);
    db.DriverValues.RemoveRange(existing);

    var activeDefs = await resolver.ResolveDriverDefinitionsAsync(tenant);
    var byPillar = activeDefs.Where(d => d.IsActive).ToLookup(d => d.PillarCode, StringComparer.OrdinalIgnoreCase);

    var pillarCodes = await db.KpiDefinitions
        .Select(k => k.Code)
        .ToListAsync();
    var pillarSet = pillarCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

    var list = new List<DriverValue>();
    var row = 2;
    foreach (var record in records)
    {
        var dict = (IDictionary<string, object?>)record;

        string Get(string name) =>
            dict.TryGetValue(name, out var v) && v is not null ? v.ToString() ?? string.Empty : string.Empty;

        var pillar = Get("pillar_code");
        var driverName = Get("driver_name");
        if (string.IsNullOrWhiteSpace(pillar) || string.IsNullOrWhiteSpace(driverName))
        {
            row++;
            continue;
        }

        decimal TryDecimal(string name)
            => dict.TryGetValue(name, out var v) && v is not null &&
               decimal.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
                ? d
                : 0m;

        int TryInt(string name)
            => dict.TryGetValue(name, out var v) && v is not null &&
               int.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
                ? d
                : 0;

        int? TryNullableInt(string name)
        {
            if (!dict.TryGetValue(name, out var v) || v is null) return null;
            var s = v.ToString();
            if (string.IsNullOrWhiteSpace(s)) return null;
            return int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
        }

        var wowKey = dict.Keys.FirstOrDefault(k => string.Equals(k, "wow", StringComparison.OrdinalIgnoreCase));
        decimal? wowVal = null;
        if (wowKey is not null && dict[wowKey] is { } wowRaw &&
            decimal.TryParse(wowRaw.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var wDec))
            wowVal = wDec;

        var statusRaw = Get("status");
        var rank = TryInt("rank");
        var driverCode = GetOptionalCsvField(dict, "driver_code");
        var fixProg = TryNullableInt("fix_progress");

        if (!pillarSet.Contains(pillar))
        {
            validation.Add("Driver", $"pillar_code '{pillar}' does not match a KPI definition.", row, "pillar_code", ImportValidationSeverity.Critical);
            row++;
            continue;
        }

        var issuesBefore = validation.Issues.Count;
        ImportRowValidator.ValidateDriverRow(
            pillar,
            driverName,
            driverCode,
            TryDecimal("current"),
            rank,
            statusRaw,
            fixProg,
            byPillar,
            validation,
            row);

        if (validation.Issues.Count > issuesBefore)
        {
            row++;
            continue;
        }

        var driver = new DriverValue
        {
            TenantId = tenant.Id,
            PeriodEnd = periodEnd,
            PillarCode = pillar,
            DriverCode = driverCode,
            DriverName = driverName,
            Dimension1 = Get("dimension1"),
            Dimension2 = Get("dimension2"),
            Current = TryDecimal("current"),
            WeekOverWeekDelta = wowVal,
            Context = Get("context"),
            Rank = rank,
            Status = string.IsNullOrWhiteSpace(statusRaw) ? "YELLOW" : statusRaw.Trim().ToUpperInvariant(),
            WhyItMatters = Get("why_it_matters"),
            Owner = GetOptionalCsvField(dict, "owner"),
            AssignedSummary = GetOptionalCsvField(dict, "assigned_summary"),
            TargetSummary = GetOptionalCsvField(dict, "target_summary"),
            CurrentSummary = GetOptionalCsvField(dict, "current_summary"),
            FixProgressPercent = fixProg
        };

        list.Add(driver);
        row++;
    }

    if (!validation.IsValid)
        return 0;

    db.DriverValues.AddRange(list);
    await db.SaveChangesAsync();
    return list.Count;
}
