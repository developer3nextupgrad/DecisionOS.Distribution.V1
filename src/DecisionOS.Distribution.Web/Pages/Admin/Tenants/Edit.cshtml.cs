using System.ComponentModel.DataAnnotations;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.Tenants;

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
        public string ClientId { get; set; } = "";
        [Required, MaxLength(500)]
        public string Name { get; set; } = "";
        [MaxLength(120)]
        public string? Archetype { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var t = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == Id);
        if (t is null) return NotFound();
        Input = new InputModel
        {
            ClientId = t.ClientId,
            Name = t.Name,
            Archetype = t.Archetype
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var t = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == Id);
        if (t is null) return NotFound();
        if (!ModelState.IsValid)
        {
            Input.ClientId = t.ClientId;
            return Page();
        }

        t.Name = Input.Name.Trim();
        t.Archetype = string.IsNullOrWhiteSpace(Input.Archetype) ? null : Input.Archetype.Trim();
        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
