using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure;
using DecisionOS.Distribution.Infrastructure.Workbooks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Operations.Uploads.Simplified;

[Authorize(Policy = "OpsPolicy")]
public class VerifyModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    private readonly ISimplifiedWorkbookImportService _simplified;
    private readonly ISimplifiedWorkbookReviewService _review;
    private readonly IWebHostEnvironment _env;

    public VerifyModel(
        DecisionOsDbContext db,
        ISimplifiedWorkbookImportService simplified,
        ISimplifiedWorkbookReviewService review,
        IWebHostEnvironment env)
    {
        _db = db;
        _simplified = simplified;
        _review = review;
        _env = env;
    }

    [BindProperty(SupportsGet = true)] public long Id { get; set; }
    [BindProperty] public DateOnly? AnchorPeriodEnd { get; set; }
    [BindProperty(SupportsGet = true)] public string? EditSheet { get; set; }
    [BindProperty] public bool AcknowledgeGrayKpis { get; set; }
    [BindProperty] public List<SheetReviewFormRow> ReviewSheets { get; set; } = [];
    [BindProperty] public List<string> ExcludedPeriodKeys { get; set; } = [];

    public UploadBatch? Batch { get; private set; }
    public WorkbookDetectionResult? Detection { get; private set; }
    public IReadOnlyList<UploadBatchIssue> Issues { get; private set; } = Array.Empty<UploadBatchIssue>();
    public IReadOnlyList<KpiCoverageLine> KpiCoverage { get; private set; } = Array.Empty<KpiCoverageLine>();
    public string? ActionError { get; private set; }
    public string? ActionSuccess { get; private set; }

    public SelectList SheetKindOptions { get; private set; } = null!;
    public DetectedSheet? MappingEditSheet { get; private set; }
    public IReadOnlyList<string> SystemFieldsForEdit { get; private set; } = Array.Empty<string>();
    public bool MappingEditIsReferenceOnly { get; private set; }

    public bool HasGrayRisk => KpiCoverage.Any(k =>
        k.Status is KpiCoverageStatus.MissingExpectGray or KpiCoverageStatus.NotInSystem);

    public string GrayKpiList => string.Join(", ",
        KpiCoverage.Where(k => k.Status is KpiCoverageStatus.MissingExpectGray or KpiCoverageStatus.NotInSystem)
            .Select(k => k.KpiCode));

    public string DashboardUrl =>
        Batch is null ? "/" : $"/Dashboard?ClientId={Batch.Tenant.ClientId}&PeriodEnd={Batch.PeriodEnd:yyyy-MM-dd}";

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        if (Batch is null) return NotFound();
        if (Detection is null)
            return RedirectToPage("Detect", new { id = Id });
        AnchorPeriodEnd = Batch.AnchorPeriodEnd ?? Detection.SuggestedAnchorPeriodEnd;
        BuildReviewFormFromDetection();
        return Page();
    }

    public async Task<IActionResult> OnPostReanalyzeAsync()
    {
        await LoadAsync();
        if (Batch is null) return NotFound();

        try
        {
            await _simplified.ReanalyzeStoredWorkbookAsync(Id, _env.ContentRootPath, AnchorPeriodEnd, Batch.Cadence);
            return RedirectToPage(new { id = Id });
        }
        catch (Exception ex)
        {
            ActionError = ex.Message;
            await LoadAsync();
            BuildReviewFormFromDetection();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSaveReviewAsync()
    {
        await LoadAsync();
        if (Batch is null || Detection is null) return NotFound();

        try
        {
            var input = BuildReviewInputFromForm();
            await _review.SaveReviewAsync(Id, input);
            ActionSuccess = "Review saved and package re-validated.";
            return RedirectToPage(new { id = Id, editSheet = EditSheet });
        }
        catch (Exception ex)
        {
            ActionError = ex.Message;
            await LoadAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostValidateAsync()
    {
        await LoadAsync();
        if (Batch is null) return NotFound();
        await _simplified.ValidateSimplifiedAsync(Id);
        return RedirectToPage(new { id = Id, editSheet = EditSheet });
    }

    public async Task<IActionResult> OnPostRunImportAsync()
    {
        await LoadAsync();
        if (Batch is null) return NotFound();

        if (HasGrayRisk && !AcknowledgeGrayKpis)
        {
            ActionError = "Confirm that you accept GRAY dashboard KPIs for: " + GrayKpiList;
            BuildReviewFormFromDetection();
            return Page();
        }

        try
        {
            await _simplified.RunSimplifiedImportAsync(Id, _env.ContentRootPath);
            return RedirectToPage("/Operations/ImportRuns/Index");
        }
        catch (Exception ex)
        {
            ActionError = ex.Message;
            await LoadAsync();
            BuildReviewFormFromDetection();
            return Page();
        }
    }

    private WorkbookReviewInput BuildReviewInputFromForm()
    {
        var excluded = ExcludedPeriodKeys
            .Select(k => DateOnly.TryParse(k, out var d) ? (DateOnly?)d : null)
            .Where(d => d is not null)
            .Select(d => d!.Value)
            .ToList();

        var sheets = new List<SheetReviewInput>();
        foreach (var row in ReviewSheets)
        {
            if (string.IsNullOrWhiteSpace(row.SheetName)) continue;
            var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (row.MappingKeys is not null && row.MappingValues is not null)
            {
                for (var i = 0; i < Math.Min(row.MappingKeys.Count, row.MappingValues.Count); i++)
                {
                    var key = row.MappingKeys[i];
                    var val = row.MappingValues[i];
                    if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(val))
                        mappings[key] = val;
                }
            }

            sheets.Add(new SheetReviewInput
            {
                SheetName = row.SheetName,
                Kind = row.Kind,
                ColumnMappings = mappings
            });
        }

        return new WorkbookReviewInput
        {
            Sheets = sheets,
            ExcludedPeriodEnds = excluded,
            AcknowledgeGrayKpis = AcknowledgeGrayKpis
        };
    }

    private void BuildReviewFormFromDetection()
    {
        if (Detection is null) return;

        var excludedSet = Detection.ExcludedPeriodEnds.ToHashSet();
        ExcludedPeriodKeys = Detection.FilteredPeriodEnds
            .Concat(Detection.RawPeriodEnds)
            .Distinct()
            .Where(d => excludedSet.Contains(d))
            .Select(d => d.ToString("yyyy-MM-dd"))
            .ToList();

        ReviewSheets = Detection.Sheets.Select(s => new SheetReviewFormRow
        {
            SheetName = s.SheetName,
            Kind = s.Kind,
            MappingKeys = s.Headers.ToList(),
            MappingValues = s.Headers.Select(h =>
                s.ColumnMappings.TryGetValue(h, out var m) ? m : "").ToList()
        }).ToList();
    }

    private async Task LoadAsync()
    {
        Batch = await _db.UploadBatches.Include(b => b.Tenant).FirstOrDefaultAsync(b => b.Id == Id);
        if (Batch is null || Batch.ImportMode != UploadImportMode.Simplified) return;

        Detection = WorkbookAnalyzer.Deserialize(Batch.DetectionSummaryJson);
        Issues = await _db.UploadBatchIssues.AsNoTracking()
            .Where(i => i.UploadBatchId == Id)
            .OrderByDescending(i => i.Severity)
            .ToListAsync();

        if (Detection is not null)
            KpiCoverage = await _review.GetKpiCoverageForBatchAsync(Id);

        SheetKindOptions = new SelectList(
            Enum.GetValues<WorkbookSheetKind>().Select(k => new { Value = (int)k, Text = k.ToString() }),
            "Value",
            "Text");

        if (!string.IsNullOrWhiteSpace(EditSheet) && Detection is not null)
        {
            MappingEditSheet = Detection.Sheets.FirstOrDefault(s =>
                string.Equals(s.SheetName, EditSheet, StringComparison.OrdinalIgnoreCase));
            if (MappingEditSheet is not null)
            {
                MappingEditIsReferenceOnly = WorkbookReviewFieldCatalog.IsReferenceOnly(MappingEditSheet.Kind);
                SystemFieldsForEdit = WorkbookReviewFieldCatalog.ForKind(MappingEditSheet.Kind);
            }
        }
    }

    public sealed class SheetReviewFormRow
    {
        public string SheetName { get; set; } = "";
        public WorkbookSheetKind Kind { get; set; }
        public List<string>? MappingKeys { get; set; }
        public List<string>? MappingValues { get; set; }
    }

    public static string CoverageStatusLabel(KpiCoverageStatus status) => status switch
    {
        KpiCoverageStatus.ReadyFromRollup => "Ready (rollup)",
        KpiCoverageStatus.ReadyFromDetail => "Ready (detail)",
        KpiCoverageStatus.DependsOnOther => "Depends on other KPIs",
        KpiCoverageStatus.MissingExpectGray => "Missing — expect GRAY",
        KpiCoverageStatus.NotInSystem => "Will auto-create on save",
        _ => status.ToString()
    };

    public static string CoverageStatusClass(KpiCoverageStatus status) => status switch
    {
        KpiCoverageStatus.ReadyFromRollup or KpiCoverageStatus.ReadyFromDetail => "text-green-700",
        KpiCoverageStatus.DependsOnOther => "text-amber-700",
        _ => "text-red-700"
    };
}
