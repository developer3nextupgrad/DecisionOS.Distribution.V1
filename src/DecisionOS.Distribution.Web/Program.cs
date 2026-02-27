using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DecisionOs")
                      ?? "Host=localhost;Port=5432;Database=decisionos;Username=decisionos;Password=decisionos";

builder.Services.AddDbContext<DecisionOsDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IKpiStatusService, KpiStatusService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IWeeklyFocusService, WeeklyFocusService>();
builder.Services.AddScoped<IDriverRankingService, DriverRankingService>();
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseStaticFiles();
app.MapRazorPages();

app.MapGet("/api/tenants/{clientId}/weeks/{periodEnd}", async (string clientId, DateOnly periodEnd, DecisionOsDbContext db) =>
{
    var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.ClientId == clientId);
    if (tenant is null) return Results.NotFound();

    var snapshots = await db.KpiSnapshots
        .Include(s => s.KpiDefinition)
        .Where(s => s.TenantId == tenant.Id && s.PeriodEnd == periodEnd)
        .OrderBy(s => s.KpiDefinition.Code)
        .Select(s => new
        {
            s.KpiDefinition.Name,
            s.KpiDefinition.Code,
            s.Value,
            s.Status,
            s.WeekOverWeekDelta
        })
        .ToListAsync();

    var alert = await db.Alerts
        .Include(a => a.KpiDefinition)
        .FirstOrDefaultAsync(a => a.TenantId == tenant.Id && a.PeriodEnd == periodEnd);

    var driversQuery = db.DriverValues
        .Where(d => d.TenantId == tenant.Id && d.PeriodEnd == periodEnd);

    if (alert is not null)
        driversQuery = driversQuery.Where(d => d.PillarCode == alert.KpiDefinition.Code);

    var drivers = await driversQuery
        .OrderBy(d => d.Rank)
        .Take(10)
        .Select(d => new
        {
            d.PillarCode,
            d.DriverName,
            d.Dimension1,
            d.Dimension2,
            d.Current,
            d.WeekOverWeekDelta,
            d.Context,
            d.Rank,
            d.Status,
            d.WhyItMatters
        })
        .ToListAsync();

    var focus = await db.WeeklyFocuses
        .Include(w => w.KpiDefinition)
        .FirstOrDefaultAsync(w => w.TenantId == tenant.Id && w.PeriodEnd == periodEnd);

    return Results.Ok(new
    {
        Tenant = new { tenant.ClientId, tenant.Name, tenant.Archetype },
        PeriodEnd = periodEnd,
        Kpis = snapshots,
        TopAlert = alert is null
            ? null
            : new
            {
                Pillar = alert.KpiDefinition.Name,
                alert.Severity,
                alert.ReasonSummary
            },
        Drivers = drivers,
        WeeklyFocus = focus is null
            ? null
            : new
            {
                focus.DecisionQuestion,
                focus.RecommendedAction,
                focus.WhyNow,
                focus.Owner,
                focus.Cadence
            }
    });
});

app.MapGet("/api/tenants", async (DecisionOsDbContext db) =>
{
    var tenants = await db.Tenants
        .Select(t => new { t.ClientId, t.Name, t.Archetype })
        .ToListAsync();

    return Results.Ok(tenants);
});

app.MapGet("/api/tenants/{clientId}/weeks", async (string clientId, DecisionOsDbContext db) =>
{
    var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.ClientId == clientId);
    if (tenant is null) return Results.NotFound();

    var weeks = await db.KpiSnapshots
        .Where(s => s.TenantId == tenant.Id)
        .Select(s => s.PeriodEnd)
        .Distinct()
        .OrderByDescending(p => p)
        .ToListAsync();

    return Results.Ok(weeks);
});

app.Run();
