using System.ComponentModel.DataAnnotations;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.VerticalLibraries;

public class EditModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public EditModel(DecisionOsDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public string Code { get; set; } = "";
        [Required, MaxLength(200)]
        public string Name { get; set; } = "";
        [MaxLength(2000)]
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var v = await _db.VerticalLibraries.FirstOrDefaultAsync(x => x.Id == Id);
        if (v is null) return NotFound();
        Input = new InputModel
        {
            Code = v.Code,
            Name = v.Name,
            Description = v.Description,
            IsActive = v.IsActive
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var v = await _db.VerticalLibraries.FirstOrDefaultAsync(x => x.Id == Id);
        if (v is null) return NotFound();
        if (!ModelState.IsValid)
        {
            Input.Code = v.Code;
            return Page();
        }

        v.Name = Input.Name.Trim();
        v.Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim();
        v.IsActive = Input.IsActive;
        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}

