using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Security;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DecisionOs")
                      ?? "Host=localhost;Port=5432;Database=decisionos;Username=decisionos;Password=decisionos";

builder.Services.AddDbContext<DecisionOsDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<DecisionOsDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole(AppRoles.Admin));
    options.AddPolicy("OpsPolicy", p => p.RequireRole(AppRoles.Admin, AppRoles.Operator, AppRoles.Developer));
    options.AddPolicy("AnyDistributionRole",
        p => p.RequireRole(AppRoles.Admin, AppRoles.Operator, AppRoles.Viewer, AppRoles.Developer));
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
    options.Conventions.AuthorizePage("/Index", "AnyDistributionRole");
    options.Conventions.AuthorizePage("/Dashboard", "AnyDistributionRole");
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Operations", "OpsPolicy");
});

builder.Services.AddScoped<IKpiStatusService, KpiStatusService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IWeeklyFocusService, WeeklyFocusService>();
builder.Services.AddScoped<IDriverRankingService, DriverRankingService>();

var app = builder.Build();

await IdentityDataSeeder.SeedAsync(app.Services);

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok", ts = DateTimeOffset.UtcNow }))
    .AllowAnonymous();

var api = app.MapGroup("/api").RequireAuthorization("AnyDistributionRole");

api.MapGet("/tenants/{clientId}/weeks/{periodEnd}", async (HttpRequest req, string clientId, DateOnly periodEnd, DecisionOsDbContext db) =>
{
    var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.ClientId == clientId);
    if (tenant is null) return Results.NotFound();

    var holdoverView = string.Equals(req.Query["view"], "holdover", StringComparison.OrdinalIgnoreCase);
    var profileId = tenant.BusinessProfileId;

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
            s.WeekOverWeekDelta,
            s.CardDetailLine1,
            s.CardDetailLine2
        })
        .ToListAsync();

    var alert = await db.Alerts
        .Include(a => a.KpiDefinition)
        .FirstOrDefaultAsync(a => a.TenantId == tenant.Id && a.PeriodEnd == periodEnd);

    var driversQuery = db.DriverValues
        .Where(d => d.TenantId == tenant.Id && d.PeriodEnd == periodEnd);

    if (!holdoverView && alert is not null)
        driversQuery = driversQuery.Where(d => d.PillarCode == alert.KpiDefinition.Code);

    var drivers = await driversQuery
        .OrderBy(d => d.PillarCode)
        .ThenBy(d => d.Rank)
        .Take(holdoverView ? 50 : 10)
        .Select(d => new
        {
            d.PillarCode,
            d.DriverCode,
            d.DriverName,
            d.Dimension1,
            d.Dimension2,
            d.Current,
            d.WeekOverWeekDelta,
            d.Context,
            d.Rank,
            d.Status,
            d.WhyItMatters,
            d.Owner,
            d.AssignedSummary,
            d.TargetSummary,
            d.CurrentSummary,
            d.FixProgressPercent
        })
        .ToListAsync();

    var focus = await db.WeeklyFocuses
        .Include(w => w.KpiDefinition)
        .FirstOrDefaultAsync(w => w.TenantId == tenant.Id && w.PeriodEnd == periodEnd);

    return Results.Ok(new
    {
        Tenant = new { tenant.ClientId, tenant.Name, tenant.Archetype, tenant.BusinessProfileId },
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

api.MapGet("/tenants", async (DecisionOsDbContext db) =>
{
    var tenants = await db.Tenants
        .Select(t => new { t.ClientId, t.Name, t.Archetype, t.BusinessProfileId })
        .ToListAsync();

    return Results.Ok(tenants);
});

api.MapGet("/tenants/{clientId}/weeks", async (string clientId, DecisionOsDbContext db) =>
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

app.MapRazorPages();
app.Run();
