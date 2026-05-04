using System.Globalization;
using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Operations.Uploads;

[Authorize(Policy = "OpsPolicy")]
public class DetailsModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly UploadBatchImportService _pipeline;

    public DetailsModel(DecisionOsDbContext db, IWebHostEnvironment env, UploadBatchImportService pipeline)
    {
        _db = db;
        _env = env;
        _pipeline = pipeline;
    }

    [BindProperty(SupportsGet = true)] public long Id { get; set; }

    public UploadBatch? Batch { get; private set; }
    public IReadOnlyList<UploadedFile> Files { get; private set; } = Array.Empty<UploadedFile>();
    public IReadOnlyList<UploadBatchIssue> Issues { get; private set; } = Array.Empty<UploadBatchIssue>();

    [BindProperty] public ReportType ReportType { get; set; } = ReportType.Sales;
    [BindProperty] public int HeaderRowNumber { get; set; } = 1;
    [BindProperty] public IFormFile? UploadFile { get; set; }

    public string? UploadError { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Batch = await _db.UploadBatches
            .Include(b => b.Tenant)
            .FirstOrDefaultAsync(b => b.Id == Id);

        if (Batch is null) return NotFound();

        Files = await _db.UploadedFiles
            .AsNoTracking()
            .Where(f => f.UploadBatchId == Batch.Id)
            .OrderBy(f => f.ReportType)
            .ThenByDescending(f => f.UploadedAt)
            .ToListAsync();

        Issues = await _db.UploadBatchIssues
            .AsNoTracking()
            .Where(i => i.UploadBatchId == Batch.Id)
            .OrderByDescending(i => i.Severity)
            .ThenBy(i => i.Category)
            .Take(200)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        await OnGetAsync();
        if (Batch is null) return NotFound();

        if (UploadFile is null || UploadFile.Length == 0)
        {
            UploadError = "Please select a file.";
            return Page();
        }

        if (HeaderRowNumber < 1 || HeaderRowNumber > 50)
        {
            UploadError = "Header row must be between 1 and 50.";
            return Page();
        }

        var originalName = Path.GetFileName(UploadFile.FileName);
        var ext = Path.GetExtension(originalName);
        if (!string.Equals(ext, ".csv", StringComparison.OrdinalIgnoreCase))
        {
            UploadError = "V1 currently supports CSV uploads for mapping/validation (Excel → export to CSV).";
            return Page();
        }

        await using var ms = new MemoryStream();
        await UploadFile.CopyToAsync(ms);
        var bytes = ms.ToArray();
        var sha = UploadedFile.ComputeSha256Hex(bytes);

        var folder = Path.Combine(_env.ContentRootPath, "App_Data", "uploads", Batch.Tenant.ClientId, Batch.PeriodEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(folder);

        var storedName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}_{sha[..10]}{ext}";
        var fullPath = Path.Combine(folder, storedName);
        await System.IO.File.WriteAllBytesAsync(fullPath, bytes);

        var rel = Path.GetRelativePath(_env.ContentRootPath, fullPath).Replace('\\', '/');

        var uploaded = new UploadedFile
        {
            UploadBatchId = Batch.Id,
            ReportType = ReportType,
            OriginalFileName = originalName,
            StoredFileName = storedName,
            StoredRelativePath = rel,
            Sha256Hex = sha,
            HeaderRowNumber = HeaderRowNumber,
            UploadedAt = DateTimeOffset.UtcNow
        };
        _db.UploadedFiles.Add(uploaded);

        Batch.Status = UploadBatchStatuses.MappingInProgress;
        await _db.SaveChangesAsync();

        // Duplicate detection: same tenant + report type + week + same sha.
        var dup = await _db.UploadedFiles
            .AsNoTracking()
            .Join(_db.UploadBatches.AsNoTracking(),
                f => f.UploadBatchId,
                b => b.Id,
                (f, b) => new { f, b })
            .Where(x =>
                x.b.TenantId == Batch.TenantId &&
                x.b.PeriodEnd == Batch.PeriodEnd &&
                x.f.ReportType == uploaded.ReportType &&
                x.f.Sha256Hex == uploaded.Sha256Hex &&
                x.f.Id != uploaded.Id)
            .OrderByDescending(x => x.f.UploadedAt)
            .FirstOrDefaultAsync();

        if (dup is not null)
        {
            _db.UploadBatchIssues.Add(new UploadBatchIssue
            {
                UploadBatchId = Batch.Id,
                UploadedFileId = uploaded.Id,
                Severity = UploadIssueSeverity.Warning,
                Category = "Duplicate",
                Message = $"This looks identical to an earlier {uploaded.ReportType} upload for the same week (SHA match). Consider replacing instead of appending.",
                Field = "sha256"
            });
            await _db.SaveChangesAsync();
        }

        // Auto-apply the most recent mapping template for this tenant/report type (if exists).
        var template = await _db.MappingTemplates
            .Include(t => t.Rules)
            .Where(t => t.TenantId == Batch.TenantId && t.ReportType == uploaded.ReportType)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        if (template is not null)
        {
            var header = await ReadCsvHeaderAsyncLocal(uploaded);
            var maps = header.Select(col =>
            {
                var rule = template.Rules.FirstOrDefault(r =>
                    string.Equals(r.SourceColumn, col, StringComparison.OrdinalIgnoreCase));
                return new UploadedFileColumnMap
                {
                    UploadedFileId = uploaded.Id,
                    SourceColumn = col,
                    Ignore = rule?.Ignore ?? false,
                    SystemField = rule?.Ignore == true ? null : rule?.SystemField
                };
            }).ToList();

            if (maps.Count > 0)
            {
                _db.UploadedFileColumnMaps.AddRange(maps);
                await _db.SaveChangesAsync();
            }
        }

        return RedirectToPage("Details", new { id = Batch.Id });
    }

    private async Task<IReadOnlyList<string>> ReadCsvHeaderAsyncLocal(UploadedFile file)
    {
        var fullPath = Path.Combine(_env.ContentRootPath, file.StoredRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(fullPath))
            return Array.Empty<string>();

        var lines = await System.IO.File.ReadAllLinesAsync(fullPath);
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

    public async Task<IActionResult> OnPostValidateAsync()
    {
        await _pipeline.ValidateAsync(Id, _env.ContentRootPath);
        return RedirectToPage("Details", new { id = Id });
    }

    public async Task<IActionResult> OnPostRunImportAsync()
    {
        await _pipeline.RunImportAsync(Id, _env.ContentRootPath);
        await OnGetAsync();
        var clientId = Batch?.Tenant.ClientId;
        var periodEnd = Batch?.PeriodEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return clientId is null || periodEnd is null
            ? RedirectToPage("Details", new { id = Id })
            : RedirectToPage("/Dashboard", new { clientId, periodEnd });
    }
}

