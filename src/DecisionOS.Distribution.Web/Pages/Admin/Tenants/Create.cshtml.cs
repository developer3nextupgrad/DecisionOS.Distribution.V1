using System.ComponentModel.DataAnnotations;
using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.Tenants;

public class CreateModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public CreateModel(DecisionOsDbContext db) => _db = db;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, MaxLength(120)]
        public string ClientId { get; set; } = "";
        [Required, MaxLength(500)]
        public string Name { get; set; } = "";
        [MaxLength(120)]
        public string? Archetype { get; set; }
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        if (await _db.Tenants.AnyAsync(t => t.ClientId == Input.ClientId.Trim()))
        {
            ModelState.AddModelError(nameof(Input.ClientId), "Client ID already exists.");
            return Page();
        }

        _db.Tenants.Add(new Tenant
        {
            Id = Guid.NewGuid(),
            ClientId = Input.ClientId.Trim(),
            Name = Input.Name.Trim(),
            Archetype = string.IsNullOrWhiteSpace(Input.Archetype) ? null : Input.Archetype.Trim()
        });
        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
