using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Security;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

static string? TryBuildNpgsqlFromRailwayDatabaseUrl(string? databaseUrl)
{
    if (string.IsNullOrWhiteSpace(databaseUrl))
        return null;

    if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri))
        return null;

    // Expected: postgres://user:pass@host:port/dbname?sslmode=require
    if (!string.Equals(uri.Scheme, "postgres", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(uri.Scheme, "postgresql", StringComparison.OrdinalIgnoreCase))
        return null;

    var userInfo = uri.UserInfo.Split(':', 2);
    var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";

    var database = uri.AbsolutePath.Trim('/');
    if (string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(uri.Host))
        return null;

    var port = uri.Port > 0 ? uri.Port : 5432;

    var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
    var sslMode = query.TryGetValue("sslmode", out var ssl) ? ssl.ToString() : null;

    // Npgsql accepts: Ssl Mode=Require; Trust Server Certificate=true
    var cs = $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password}";
    if (!string.IsNullOrWhiteSpace(sslMode))
        cs += $";Ssl Mode={sslMode}";

    if (query.TryGetValue("trustservercertificate", out var tsc) &&
        string.Equals(tsc.ToString(), "true", StringComparison.OrdinalIgnoreCase))
        cs += ";Trust Server Certificate=true";

    return cs;
}

var envConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DecisionOs");
var connectionString =
    (!string.IsNullOrWhiteSpace(envConnectionString) ? envConnectionString : null)
    ?? TryBuildNpgsqlFromRailwayDatabaseUrl(Environment.GetEnvironmentVariable("DATABASE_URL"))
    ?? builder.Configuration.GetConnectionString("DecisionOs")
    ?? "Host=localhost;Port=5432;Database=decisionos;Username=decisionos;Password=decisionos";

var dataProtectionPath = builder.Configuration["DataProtection:Path"];
if (!string.IsNullOrWhiteSpace(dataProtectionPath))
{
    builder.Services
        .AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
        .SetApplicationName("DecisionOS.Distribution");
}

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
builder.Services.AddScoped<UploadBatchImportService>();

var app = builder.Build();

// Ensure schema exists before we run any startup seeders (Identity roles/users).
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DecisionOsDbContext>();
    app.Logger.LogInformation("Applying database migrations (if any)...");
    await db.Database.MigrateAsync();
}

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

    var importRun = await db.ImportRuns
        .AsNoTracking()
        .Where(r => r.TenantId == tenant.Id && r.PeriodEnd == periodEnd)
        .OrderByDescending(r => r.StartedAt)
        .FirstOrDefaultAsync();

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
            s.DataConfidence,
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

    var holdoverActionsQuery = db.ActionItems.AsNoTracking()
        .Where(a => a.TenantId == tenant.Id);

    if (!holdoverView)
        holdoverActionsQuery = holdoverActionsQuery.Where(a => a.PeriodEnd == periodEnd || a.Status != ActionStatuses.Completed);

    var actions = await holdoverActionsQuery
        .Include(a => a.KpiDefinition)
        .OrderByDescending(a => a.PeriodEnd)
        .ThenBy(a => a.Priority)
        .Take(holdoverView ? 50 : 20)
        .Select(a => new
        {
            a.Id,
            a.PeriodEnd,
            PillarCode = a.KpiDefinition != null ? a.KpiDefinition.Code : null,
            Pillar = a.KpiDefinition != null ? a.KpiDefinition.Name : null,
            a.Title,
            a.Description,
            a.Owner,
            a.DueDate,
            a.Status,
            a.Priority,
            a.UpdatedAt,
            a.CompletedAt,
            a.Notes
        })
        .ToListAsync();

    return Results.Ok(new
    {
        Tenant = new { tenant.ClientId, tenant.Name, tenant.Archetype, tenant.BusinessProfileId },
        PeriodEnd = periodEnd,
        Import = importRun is null ? null : new { importRun.Status, importRun.ReadinessStatus, importRun.ValidationSummary },
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
        Actions = actions,
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

api.MapPost("/tenants/{clientId}/actions/{actionId:long}/status", async (string clientId, long actionId, string status, DecisionOsDbContext db) =>
{
    var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.ClientId == clientId);
    if (tenant is null) return Results.NotFound();

    var action = await db.ActionItems.FirstOrDefaultAsync(a => a.Id == actionId && a.TenantId == tenant.Id);
    if (action is null) return Results.NotFound();

    var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ActionStatuses.NotStarted,
        ActionStatuses.InProgress,
        ActionStatuses.AtRisk,
        ActionStatuses.Completed,
        ActionStatuses.Deferred,
        ActionStatuses.Blocked
    };
    if (!allowed.Contains(status))
        return Results.BadRequest(new { error = "Invalid status." });

    action.Status = status;
    action.UpdatedAt = DateTimeOffset.UtcNow;
    action.CompletedAt = string.Equals(status, ActionStatuses.Completed, StringComparison.OrdinalIgnoreCase)
        ? DateTimeOffset.UtcNow
        : null;

    await db.SaveChangesAsync();
    return Results.Ok(new { action.Id, action.Status, action.UpdatedAt, action.CompletedAt });
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
