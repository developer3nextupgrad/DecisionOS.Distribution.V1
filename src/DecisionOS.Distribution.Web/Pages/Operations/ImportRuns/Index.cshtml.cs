using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Operations.ImportRuns;

public class IndexModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public IndexModel(DecisionOsDbContext db) => _db = db;

    public IReadOnlyList<Row> Items { get; private set; } = Array.Empty<Row>();

    public sealed record Row(
        int Id,
        string ClientId,
        string TenantName,
        DateOnly PeriodEnd,
        DateTimeOffset StartedAt,
        DateTimeOffset? CompletedAt,
        string Status,
        string? ReadinessStatus,
        int KpiRows,
        int DriverRows,
        string? Fingerprint,
        string? Error,
        string? ValidationSummary);

    public async Task OnGetAsync()
    {
        Items = await (from run in _db.ImportRuns.AsNoTracking()
                join t in _db.Tenants.AsNoTracking() on run.TenantId equals t.Id
                orderby run.StartedAt descending
                select new Row(
                    run.Id,
                    t.ClientId,
                    t.Name,
                    run.PeriodEnd,
                    run.StartedAt,
                    run.CompletedAt,
                    run.Status,
                    run.ReadinessStatus,
                    run.KpiRowsProcessed,
                    run.DriverRowsProcessed,
                    run.SourceFingerprint,
                    run.ErrorMessage,
                    run.ValidationSummary))
            .Take(200)
            .ToListAsync();
    }
}
