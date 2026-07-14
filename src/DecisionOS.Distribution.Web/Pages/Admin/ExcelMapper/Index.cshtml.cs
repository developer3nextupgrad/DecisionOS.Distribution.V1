using DecisionOS.Distribution.Domain.Uploads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DecisionOS.Distribution.Web.Pages.Admin.ExcelMapper;

[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private readonly IExcelMapperService _mapper;

    public IndexModel(IExcelMapperService mapper) => _mapper = mapper;

    public string? Error { get; private set; }

    [BindProperty] public IFormFile? WorkbookFile { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostUploadAsync(CancellationToken ct)
    {
        if (WorkbookFile is null || WorkbookFile.Length == 0)
        {
            Error = "Please select an Excel workbook (.xlsx).";
            return Page();
        }

        var ext = Path.GetExtension(WorkbookFile.FileName);
        if (!string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(ext, ".xls", StringComparison.OrdinalIgnoreCase))
        {
            Error = "Only Excel (.xlsx/.xls) workbooks are supported.";
            return Page();
        }

        await using var ms = new MemoryStream();
        await WorkbookFile.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        try
        {
            var session = await _mapper.StartSessionAsync(bytes, WorkbookFile.FileName, ct);
            return RedirectToPage("Review", new { sessionId = session.SessionId });
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            return Page();
        }
    }
}
