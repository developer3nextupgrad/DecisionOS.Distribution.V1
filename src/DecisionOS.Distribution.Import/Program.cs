using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Import;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var rawArgs = args.ToList();
var forceReimport = rawArgs.RemoveAll(x => string.Equals(x, "--force", StringComparison.OrdinalIgnoreCase)) > 0;

if (rawArgs.Count < 3)
{
    Console.WriteLine("Usage: DecisionOS.Distribution.Import <client_id> <period_end:YYYY-MM-DD> <kpi_csv_path> [drivers_csv_path] [--force]");
    return;
}

var clientId = rawArgs[0];
if (!DateOnly.TryParse(rawArgs[1], out var periodEnd))
{
    Console.WriteLine("Invalid period_end. Expected YYYY-MM-DD.");
    return;
}

var kpiCsvPath = rawArgs[2];
var driversCsvPath = rawArgs.Count > 3 ? rawArgs[3] : string.Empty;

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

var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.ClientId == clientId);
if (tenant is null)
{
    tenant = new Tenant
    {
        Id = Guid.NewGuid(),
        ClientId = clientId,
        Name = clientId
    };
    db.Tenants.Add(tenant);
    await db.SaveChangesAsync();
}

await SeedKpiDefinitionsIfNeeded(db);
await SyncKpiDefinitionPriorities(db);
await SeedDriverDefinitionsIfNeeded(db);

var fingerprint = ComputeImportFingerprint(clientId, periodEnd, kpiCsvPath, driversCsvPath);

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

try
{
    ValidateKpiCsvHeaders(kpiCsvPath);
    if (!string.IsNullOrWhiteSpace(driversCsvPath))
    {
        var requireDriverCode = await db.DriverDefinitions.AnyAsync(d => d.IsActive);
        await ValidateDriverCsvHeadersAsync(driversCsvPath, requireDriverCode);
    }

    var kpiValidation = new ImportValidationResult();
    importRun.KpiRowsProcessed = await ImportKpisAsync(db, tenant, periodEnd, kpiCsvPath, kpiStatusService, kpiValidation);
    if (!kpiValidation.IsValid)
        throw new InvalidOperationException(kpiValidation.FormatMessages());

    if (!string.IsNullOrWhiteSpace(driversCsvPath))
    {
        var driverValidation = new ImportValidationResult();
        importRun.DriverRowsProcessed = await ImportDriversAsync(db, tenant, periodEnd, driversCsvPath, driverValidation);
        if (!driverValidation.IsValid)
            throw new InvalidOperationException(driverValidation.FormatMessages());
    }

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

    importRun.Status = "Completed";
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

static async Task<int> ImportKpisAsync(
    DecisionOsDbContext db,
    Tenant tenant,
    DateOnly periodEnd,
    string cdfPath,
    IKpiStatusService statusService,
    ImportValidationResult validation)
{
    using var reader = new StreamReader(cdfPath);
    var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
    using var csv = new CsvReader(reader, config);

    var records = csv.GetRecords<dynamic>().ToList();
    var definitions = (await db.KpiDefinitions.ToListAsync()).ToDictionary(d => d.Code, StringComparer.Ordinal);

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
            validation.Add("KPI", "value is required when kpi_code is set.", row, "value");
            row++;
            continue;
        }

        var code = codeObj.ToString()!.Trim();
        if (!definitions.TryGetValue(code, out var definition))
        {
            validation.Add("KPI", $"Unknown kpi_code '{code}'.", row, "kpi_code");
            row++;
            continue;
        }

        if (!decimal.TryParse(valueObj.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            validation.Add("KPI", $"Cannot parse value for '{code}'.", row, "value");
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
    ImportValidationResult validation)
{
    using var reader = new StreamReader(csvPath);
    var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
    using var csv = new CsvReader(reader, config);

    var records = csv.GetRecords<dynamic>().ToList();

    var existing = db.DriverValues.Where(d => d.TenantId == tenant.Id && d.PeriodEnd == periodEnd);
    db.DriverValues.RemoveRange(existing);

    var activeDefs = await db.DriverDefinitions.Where(d => d.IsActive).ToListAsync();
    var byPillar = activeDefs.ToLookup(d => d.PillarCode, StringComparer.OrdinalIgnoreCase);

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
            validation.Add("Driver", $"pillar_code '{pillar}' does not match a KPI definition.", row, "pillar_code");
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
