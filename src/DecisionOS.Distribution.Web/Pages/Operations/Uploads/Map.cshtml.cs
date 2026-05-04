using System.Globalization;
using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Operations.Uploads;

[Authorize(Policy = "OpsPolicy")]
public class MapModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    private readonly IWebHostEnvironment _env;

    public MapModel(DecisionOsDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [BindProperty(SupportsGet = true)] public long BatchId { get; set; }
    [BindProperty(SupportsGet = true)] public long FileId { get; set; }

    public UploadBatch? Batch { get; private set; }
    public UploadedFile? Uploaded { get; private set; }

    public IReadOnlyList<string> SourceColumns { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> SystemFields { get; private set; } = Array.Empty<string>();

    [BindProperty] public string TemplateName { get; set; } = "";
    [BindProperty] public bool SaveAsTemplate { get; set; }
    [BindProperty] public Dictionary<string, string> Mapping { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    [BindProperty] public HashSet<string> Ignored { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public async Task<IActionResult> OnGetAsync()
    {
        var loaded = await LoadAsync();
        if (loaded is not null) return loaded;

        SourceColumns = await ReadCsvHeaderAsync();
        SystemFields = DecisionOS.Distribution.Domain.Uploads.SystemFields.For(Uploaded!.ReportType);

        var existing = await _db.UploadedFileColumnMaps
            .AsNoTracking()
            .Where(m => m.UploadedFileId == Uploaded!.Id)
            .ToListAsync();
        if (existing.Count > 0)
        {
            foreach (var m in existing)
            {
                if (m.Ignore) Ignored.Add(m.SourceColumn);
                if (!string.IsNullOrWhiteSpace(m.SystemField)) Mapping[m.SourceColumn] = m.SystemField!;
            }
        }
        else
        {
            // Basic auto-map: direct match ignoring punctuation/case.
            foreach (var col in SourceColumns)
            {
                var best = SystemFields.FirstOrDefault(sf =>
                    string.Equals(Normalize(col), Normalize(sf), StringComparison.OrdinalIgnoreCase));
                if (best is not null)
                    Mapping[col] = best;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var loaded = await LoadAsync();
        if (loaded is not null) return loaded;

        SourceColumns = await ReadCsvHeaderAsync();
        SystemFields = DecisionOS.Distribution.Domain.Uploads.SystemFields.For(Uploaded!.ReportType);

        if (SaveAsTemplate)
        {
            if (string.IsNullOrWhiteSpace(TemplateName))
                ModelState.AddModelError(nameof(TemplateName), "Template name is required.");
        }

        if (!ModelState.IsValid) return Page();

        // Persist per-file mappings (upsert).
        var current = await _db.UploadedFileColumnMaps
            .Where(m => m.UploadedFileId == Uploaded!.Id)
            .ToListAsync();
        _db.UploadedFileColumnMaps.RemoveRange(current);

        var rows = SourceColumns.Select(c => new UploadedFileColumnMap
        {
            UploadedFileId = Uploaded!.Id,
            SourceColumn = c,
            Ignore = Ignored.Contains(c),
            SystemField = Ignored.Contains(c)
                ? null
                : (Mapping.TryGetValue(c, out var sf) && !string.IsNullOrWhiteSpace(sf) ? sf : null)
        }).ToList();
        _db.UploadedFileColumnMaps.AddRange(rows);

        if (SaveAsTemplate)
        {
            var t = new MappingTemplate
            {
                TenantId = Batch!.TenantId,
                ReportType = Uploaded!.ReportType,
                Name = TemplateName.Trim(),
                CreatedAt = DateTimeOffset.UtcNow,
                Rules = SourceColumns.Select(c => new MappingRule
                {
                    SourceColumn = c,
                    Ignore = Ignored.Contains(c),
                    SystemField = Ignored.Contains(c) ? null : (Mapping.TryGetValue(c, out var sf) && !string.IsNullOrWhiteSpace(sf) ? sf : null)
                }).ToList()
            };

            _db.MappingTemplates.Add(t);
        }

        await _db.SaveChangesAsync();
        return RedirectToPage("Details", new { id = Batch!.Id });
    }

    private async Task<IActionResult?> LoadAsync()
    {
        Batch = await _db.UploadBatches
            .Include(b => b.Tenant)
            .FirstOrDefaultAsync(b => b.Id == BatchId);
        if (Batch is null) return NotFound();

        Uploaded = await _db.UploadedFiles.FirstOrDefaultAsync(f => f.Id == FileId && f.UploadBatchId == BatchId);
        if (Uploaded is null) return NotFound();

        return null;
    }

    private async Task<IReadOnlyList<string>> ReadCsvHeaderAsync()
    {
        var fullPath = Path.Combine(_env.ContentRootPath, Uploaded!.StoredRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(fullPath))
            return Array.Empty<string>();

        var lines = await System.IO.File.ReadAllLinesAsync(fullPath);
        var idx = Math.Clamp(Uploaded!.HeaderRowNumber - 1, 0, Math.Max(0, lines.Length - 1));
        var headerLine = lines.Length == 0 ? "" : lines[idx];
        return SplitCsv(headerLine).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
    }

    private static List<string> SplitCsv(string line)
    {
        // Minimal CSV header splitter (handles quotes).
        var res = new List<string>();
        var cur = "";
        var inQ = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"' )
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

    private static string Normalize(string s)
    {
        var chars = s.Trim().ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c == '_')
            .ToArray();
        return new string(chars);
    }
}

