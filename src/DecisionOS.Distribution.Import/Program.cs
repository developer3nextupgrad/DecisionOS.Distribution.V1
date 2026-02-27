using CsvHelper;
using CsvHelper.Configuration;
using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Globalization;

if (args.Length < 3)
{
    Console.WriteLine("Usage: DecisionOS.Distribution.Import <client_id> <period_end:YYYY-MM-DD> <kpi_csv_path> <drivers_csv_path>");
    return;
}

var clientId = args[0];
if (!DateOnly.TryParse(args[1], out var periodEnd))
{
    Console.WriteLine("Invalid period_end. Expected YYYY-MM-DD.");
    return;
}

var kpiCsvPath = args[2];
var driversCsvPath = args.Length > 3 ? args[3] : string.Empty;

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

var kpiStatusService = new KpiStatusService();
var alertService = new AlertService();
var weeklyFocusService = new WeeklyFocusService();
var driverRankingService = new DriverRankingService();

await ImportKpisAsync(db, tenant, periodEnd, kpiCsvPath, kpiStatusService);

if (!string.IsNullOrWhiteSpace(driversCsvPath))
{
    await ImportDriversAsync(db, tenant, periodEnd, driversCsvPath);
}

// Compute week-over-week deltas for KPI snapshots
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

// Generate alert and weekly focus
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

Console.WriteLine("Import completed.");

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
            RecommendedAction = "Run service recovery: fix the biggest service driver (late, short, or damaged) and stabilize throughput.",
            DiagnosticChecks = "Decompose: On-time %, Fill rate %, Damage rate; identify top WH/carrier/SKU exceptions."
        }
    };

    db.KpiDefinitions.AddRange(defs);
    await db.SaveChangesAsync();
}

static async Task ImportKpisAsync(
    DecisionOsDbContext db,
    Tenant tenant,
    DateOnly periodEnd,
    string csvPath,
    IKpiStatusService statusService)
{
    using var reader = new StreamReader(csvPath);
    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true
    };
    using var csv = new CsvReader(reader, config);

    var records = csv.GetRecords<dynamic>();

    var definitions = await db.KpiDefinitions.ToDictionaryAsync(d => d.Code);

    foreach (var record in records)
    {
        var dict = (IDictionary<string, object?>)record;
        if (!dict.TryGetValue("kpi_code", out var codeObj) || codeObj is null) continue;
        if (!dict.TryGetValue("value", out var valueObj) || valueObj is null) continue;

        var code = codeObj.ToString() ?? string.Empty;
        if (!definitions.TryGetValue(code, out var definition)) continue;

        if (!decimal.TryParse(valueObj.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            continue;

        var status = statusService.ComputeStatus(definition, value);

        var existing = await db.KpiSnapshots
            .FirstOrDefaultAsync(s =>
                s.TenantId == tenant.Id &&
                s.PeriodEnd == periodEnd &&
                s.KpiDefinitionId == definition.Id);

        if (existing is null)
        {
            existing = new KpiSnapshot
            {
                TenantId = tenant.Id,
                PeriodEnd = periodEnd,
                KpiDefinitionId = definition.Id
            };
            db.KpiSnapshots.Add(existing);
        }

        existing.Value = value;
        existing.Status = status;
    }

    await db.SaveChangesAsync();
}

static async Task ImportDriversAsync(
    DecisionOsDbContext db,
    Tenant tenant,
    DateOnly periodEnd,
    string csvPath)
{
    using var reader = new StreamReader(csvPath);
    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true
    };
    using var csv = new CsvReader(reader, config);

    var records = csv.GetRecords<dynamic>();

    var existing = db.DriverValues
        .Where(d => d.TenantId == tenant.Id && d.PeriodEnd == periodEnd);
    db.DriverValues.RemoveRange(existing);

    var list = new List<DriverValue>();

    foreach (var record in records)
    {
        var dict = (IDictionary<string, object?>)record;

        string Get(string name) => dict.TryGetValue(name, out var v) && v is not null ? v.ToString() ?? string.Empty : string.Empty;

        var pillar = Get("pillar_code");
        var driverName = Get("driver_name");
        if (string.IsNullOrWhiteSpace(pillar) || string.IsNullOrWhiteSpace(driverName))
            continue;

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

        var driver = new DriverValue
        {
            TenantId = tenant.Id,
            PeriodEnd = periodEnd,
            PillarCode = pillar,
            DriverName = driverName,
            Dimension1 = Get("dimension1"),
            Dimension2 = Get("dimension2"),
            Current = TryDecimal("current"),
            WeekOverWeekDelta = TryDecimal("wow"),
            Context = Get("context"),
            Rank = TryInt("rank"),
            Status = Get("status"),
            WhyItMatters = Get("why_it_matters")
        };

        list.Add(driver);
    }

    db.DriverValues.AddRange(list);
    await db.SaveChangesAsync();
}
