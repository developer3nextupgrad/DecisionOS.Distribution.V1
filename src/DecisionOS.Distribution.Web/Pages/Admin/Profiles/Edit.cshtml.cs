using System.ComponentModel.DataAnnotations;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.Profiles;

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
        var p = await _db.BusinessProfiles.FirstOrDefaultAsync(x => x.Id == Id);
        if (p is null) return NotFound();
        Input = new InputModel
        {
            Code = p.Code,
            Name = p.Name,
            Description = p.Description,
            IsActive = p.IsActive
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var p = await _db.BusinessProfiles.FirstOrDefaultAsync(x => x.Id == Id);
        if (p is null) return NotFound();
        if (!ModelState.IsValid)
        {
            Input.Code = p.Code;
            return Page();
        }

        p.Name = Input.Name.Trim();
        p.Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim();
        p.IsActive = Input.IsActive;
        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}

