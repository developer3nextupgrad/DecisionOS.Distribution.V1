using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Operations.Uploads;

[Authorize(Policy = "OpsPolicy")]
public class IndexModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public IndexModel(DecisionOsDbContext db) => _db = db;

    public IReadOnlyList<Row> Items { get; private set; } = Array.Empty<Row>();

    public sealed record Row(
        long Id,
        string ClientId,
        string TenantName,
        DateOnly PeriodEnd,
        DateTimeOffset CreatedAt,
        string Status,
        string? Readiness,
        int FileCount);

    public async Task OnGetAsync()
    {
        Items = await (from b in _db.UploadBatches.AsNoTracking()
                join t in _db.Tenants.AsNoTracking() on b.TenantId equals t.Id
                orderby b.CreatedAt descending
                select new Row(
                    b.Id,
                    t.ClientId,
                    t.Name,
                    b.PeriodEnd,
                    b.CreatedAt,
                    b.Status,
                    b.ReadinessStatus,
                    _db.UploadedFiles.Count(f => f.UploadBatchId == b.Id)))
            .Take(200)
            .ToListAsync();
    }
}

