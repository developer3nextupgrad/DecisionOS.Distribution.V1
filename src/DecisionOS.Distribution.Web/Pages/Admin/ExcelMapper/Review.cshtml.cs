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
            Detection = await _mapper.GetDetectionAsync(SessionId, ct);
            BuildReviewFormFromDetection();
            LoadMappingEditor();
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
            ActionSuccess = "Mappings saved.";
            return RedirectToPage(new { sessionId = SessionId, editSheet = EditSheet });
        }
        catch (Exception ex)
        {
            ActionError = ex.Message;
            Detection = await _mapper.GetDetectionAsync(SessionId, ct);
            LoadMappingEditor();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostGenerateAsync(CancellationToken ct)
    {
        try
        {
            var input = BuildReviewInputFromForm();
            await _mapper.SaveReviewAsync(SessionId, input, ct);
            var bytes = await _mapper.GenerateMappedWorkbookAsync(SessionId, ct);
            var fileName = _mapper.GetSuggestedDownloadName(SessionId);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            ActionError = ex.Message;
            Detection = await _mapper.GetDetectionAsync(SessionId, ct);
            BuildReviewFormFromDetection();
            LoadMappingEditor();
            return Page();
        }
    }

    private ExcelMapperReviewInput BuildReviewInputFromForm()
    {
        var sheets = new List<ExcelMapperSheetReview>();
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

            sheets.Add(new ExcelMapperSheetReview
            {
                SheetName = row.SheetName,
                Kind = row.Kind,
                ColumnMappings = mappings
            });
        }

        return new ExcelMapperReviewInput { Sheets = sheets };
    }

    private void BuildReviewFormFromDetection()
    {
        if (Detection is null) return;

        ReviewSheets = Detection.Sheets.Select(s => new SheetReviewFormRow
        {
            SheetName = s.SheetName,
            Kind = s.Kind,
            MappingKeys = s.Headers.ToList(),
            MappingValues = s.Headers.Select(h =>
                s.ColumnMappings.TryGetValue(h, out var m) ? m : "").ToList()
        }).ToList();
    }

    private void LoadMappingEditor()
    {
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
}
