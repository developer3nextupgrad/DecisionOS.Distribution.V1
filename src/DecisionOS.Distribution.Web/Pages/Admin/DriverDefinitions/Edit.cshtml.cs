using System.ComponentModel.DataAnnotations;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.DriverDefinitions;

public class EditModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public EditModel(DecisionOsDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public string PillarCode { get; set; } = "";
        public string DriverCode { get; set; } = "";
        [Required, MaxLength(500)]
        public string DisplayName { get; set; } = "";
        [MaxLength(2000)]
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var d = await _db.DriverDefinitions.FirstOrDefaultAsync(x => x.Id == Id);
        if (d is null) return NotFound();
        Input = new InputModel
        {
            PillarCode = d.PillarCode,
            DriverCode = d.DriverCode,
            DisplayName = d.DisplayName,
            Description = d.Description,
            SortOrder = d.SortOrder,
            IsActive = d.IsActive
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var d = await _db.DriverDefinitions.FirstOrDefaultAsync(x => x.Id == Id);
        if (d is null) return NotFound();
        if (!ModelState.IsValid)
        {
            Input.PillarCode = d.PillarCode;
            Input.DriverCode = d.DriverCode;
            return Page();
        }

        d.DisplayName = Input.DisplayName.Trim();
        d.Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim();
        d.SortOrder = Input.SortOrder;
        d.IsActive = Input.IsActive;
        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
