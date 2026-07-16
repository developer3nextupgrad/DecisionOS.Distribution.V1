using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure.Workbooks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DecisionOS.Distribution.Web.Pages.Admin.ExcelMapper;

[Authorize(Policy = "AdminOnly")]
public class ReviewModel : PageModel
{
    private readonly IExcelMapperService _mapper;

    public ReviewModel(IExcelMapperService mapper) => _mapper = mapper;

    [BindProperty(SupportsGet = true)] public Guid SessionId { get; set; }
    [BindProperty(SupportsGet = true)] public string? EditSheet { get; set; }
    [BindProperty] public List<SheetReviewFormRow> ReviewSheets { get; set; } = [];

    public WorkbookDetectionResult? Detection { get; private set; }
    public ExcelMapperReviewInput? Review { get; private set; }
    public ExcelMapperReadinessResult? Readiness { get; private set; }
    public string? ActionError { get; private set; }
    public string? ActionSuccess { get; private set; }

    public SelectList SheetKindOptions { get; private set; } = null!;
    public DetectedSheet? MappingEditSheet { get; private set; }
    public IReadOnlyList<string> SystemFieldsForEdit { get; private set; } = Array.Empty<string>();
    public bool MappingEditIsReferenceOnly { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        try
        {
            await LoadStateAsync(ct);
            ActionSuccess = TempData["ExcelMapperSuccess"] as string;
            return Page();
        }
        catch (Exception ex)
        {
            ActionError = ex.Message;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSaveReviewAsync(CancellationToken ct)
    {
        try
        {
            var input = BuildReviewInputFromForm();
            await _mapper.SaveReviewAsync(SessionId, input, ct);
            TempData["ExcelMapperSuccess"] =
                "Mappings saved. Uncertain tab warnings clear after you choose a Role and save.";
            return RedirectToPage(new { sessionId = SessionId, editSheet = EditSheet });
        }
        catch (Exception ex)
        {
            ActionError = ex.Message;
            await LoadStateAsync(ct);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostGenerateAsync(CancellationToken ct)
    {
        try
        {
            var input = BuildReviewInputFromForm();
            await _mapper.SaveReviewAsync(SessionId, input, ct);

            var detection = await _mapper.GetDetectionAsync(SessionId, ct);
            var review = await _mapper.GetReviewAsync(SessionId, ct);
            var readiness = _mapper.EvaluateReadiness(detection, review);
            if (!readiness.CanGenerate)
            {
                ActionError = string.Join(" ", readiness.BlockingIssues);
                await LoadStateAsync(ct);
                return Page();
            }

            var bytes = await _mapper.GenerateMappedWorkbookAsync(SessionId, ct);
            var fileName = _mapper.GetSuggestedDownloadName(SessionId);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            ActionError = ex.Message;
            await LoadStateAsync(ct);
            return Page();
        }
    }

    private async Task LoadStateAsync(CancellationToken ct)
    {
        Detection = await _mapper.GetDetectionAsync(SessionId, ct);
        Review = await _mapper.GetReviewAsync(SessionId, ct);
        BuildReviewFormFromReview();
        Readiness = _mapper.EvaluateReadiness(Detection, Review);
        LoadMappingEditor();
    }

    private ExcelMapperReviewInput BuildReviewInputFromForm()
    {
        var sheets = new List<ExcelMapperSheetReview>();
        foreach (var row in ReviewSheets)
        {
            if (string.IsNullOrWhiteSpace(row.SheetName)) continue;
            var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var mappingsProvided = row.MappingKeys is not null;
            if (mappingsProvided && row.MappingValues is not null)
            {
                for (var i = 0; i < Math.Min(row.MappingKeys!.Count, row.MappingValues.Count); i++)
                {
                    var key = row.MappingKeys[i];
                    var val = row.MappingValues[i];
                    if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(val))
                        mappings[key] = val;
                }
            }

            sheets.Add(new ExcelMapperSheetReview
            {
                SheetName = row.SheetName,
                Kind = row.Kind,
                ColumnMappings = mappings,
                ColumnMappingsProvided = mappingsProvided
            });
        }

        return new ExcelMapperReviewInput { Sheets = sheets };
    }

    private void BuildReviewFormFromReview()
    {
        if (Detection is null || Review is null) return;

        var reviewByName = Review.Sheets.ToDictionary(s => s.SheetName, StringComparer.OrdinalIgnoreCase);

        ReviewSheets = Detection.Sheets.Select(s =>
        {
            reviewByName.TryGetValue(s.SheetName, out var rev);
            var kind = rev?.Kind ?? s.Kind;
            var maps = rev?.ColumnMappings ?? s.ColumnMappings;
            return new SheetReviewFormRow
            {
                SheetName = s.SheetName,
                Kind = kind,
                MappingKeys = s.Headers.ToList(),
                MappingValues = s.Headers.Select(h =>
                    maps.TryGetValue(h, out var m) ? m : "").ToList()
            };
        }).ToList();
    }

    private void LoadMappingEditor()
    {
        SheetKindOptions = new SelectList(
            Enum.GetValues<WorkbookSheetKind>()
                .Select(k => new { Value = (int)k, Text = WorkbookSheetKindDisplay.Label(k) }),
            "Value",
            "Text");

        if (!string.IsNullOrWhiteSpace(EditSheet) && Detection is not null && Review is not null)
        {
            var reviewSheet = Review.Sheets.FirstOrDefault(s =>
                string.Equals(s.SheetName, EditSheet, StringComparison.OrdinalIgnoreCase));
            MappingEditSheet = Detection.Sheets.FirstOrDefault(s =>
                string.Equals(s.SheetName, EditSheet, StringComparison.OrdinalIgnoreCase));

            if (MappingEditSheet is not null)
            {
                var kind = reviewSheet?.Kind ?? MappingEditSheet.Kind;
                MappingEditIsReferenceOnly = WorkbookReviewFieldCatalog.IsReferenceOnly(kind);
                SystemFieldsForEdit = WorkbookReviewFieldCatalog.ForKind(kind);
                // Present the operator-confirmed kind in the editor subtitle
                MappingEditSheet = new DetectedSheet
                {
                    SheetName = MappingEditSheet.SheetName,
                    SheetIndex = MappingEditSheet.SheetIndex,
                    Kind = kind,
                    ReportType = WorkbookReviewFieldCatalog.ReportTypeForKind(kind),
                    Confidence = MappingEditSheet.Confidence,
                    DataRowCount = MappingEditSheet.DataRowCount,
                    HeaderRowNumber = MappingEditSheet.HeaderRowNumber,
                    Headers = MappingEditSheet.Headers,
                    ColumnMappings = reviewSheet?.ColumnMappings ?? MappingEditSheet.ColumnMappings
                };
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
}
