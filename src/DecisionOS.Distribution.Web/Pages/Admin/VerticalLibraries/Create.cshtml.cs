using System.ComponentModel.DataAnnotations;
using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.VerticalLibraries;

public class CreateModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public CreateModel(DecisionOsDbContext db) => _db = db;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, MaxLength(80)]
        public string Code { get; set; } = "";
        [Required, MaxLength(200)]
        public string Name { get; set; } = "";
        [MaxLength(2000)]
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var code = Input.Code.Trim().ToUpperInvariant();
        if (await _db.VerticalLibraries.AnyAsync(v => v.Code == code))
        {
            ModelState.AddModelError(nameof(Input.Code), "Vertical code already exists.");
            return Page();
        }

        _db.VerticalLibraries.Add(new VerticalLibrary
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = Input.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim(),
            IsActive = Input.IsActive
        });
        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}

