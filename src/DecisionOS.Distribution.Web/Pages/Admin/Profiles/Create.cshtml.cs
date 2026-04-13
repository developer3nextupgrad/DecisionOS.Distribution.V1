using System.ComponentModel.DataAnnotations;
using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.Profiles;

public class CreateModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public CreateModel(DecisionOsDbContext db) => _db = db;

    public SelectList? VerticalOptions { get; private set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public Guid? VerticalLibraryId { get; set; }
        [Required, MaxLength(80)]
        public string Code { get; set; } = "";
        [Required, MaxLength(200)]
        public string Name { get; set; } = "";
        [MaxLength(2000)]
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public string? ActiveKpiProfileCode { get; set; }
        public string? LocationStructure { get; set; }
        public string? ChannelStructure { get; set; }
        public string? ThresholdProfileCode { get; set; }
    }

    public void OnGet()
    {
        VerticalOptions = new SelectList(_db.VerticalLibraries.Where(v => v.IsActive).OrderBy(v => v.Name), "Id", "Name");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        VerticalOptions = new SelectList(await _db.VerticalLibraries.Where(v => v.IsActive).OrderBy(v => v.Name).ToListAsync(), "Id", "Name");
        if (!ModelState.IsValid) return Page();

        var code = Input.Code.Trim().ToUpperInvariant();
        if (await _db.BusinessProfiles.AnyAsync(p => p.Code == code))
        {
            ModelState.AddModelError(nameof(Input.Code), "Profile code already exists.");
            return Page();
        }

        _db.BusinessProfiles.Add(new BusinessProfile
        {
            Id = Guid.NewGuid(),
            VerticalLibraryId = Input.VerticalLibraryId,
            Code = code,
            Name = Input.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim(),
            IsActive = Input.IsActive,
            ActiveKpiProfileCode = string.IsNullOrWhiteSpace(Input.ActiveKpiProfileCode) ? null : Input.ActiveKpiProfileCode.Trim(),
            LocationStructure = string.IsNullOrWhiteSpace(Input.LocationStructure) ? null : Input.LocationStructure.Trim(),
            ChannelStructure = string.IsNullOrWhiteSpace(Input.ChannelStructure) ? null : Input.ChannelStructure.Trim(),
            ThresholdProfileCode = string.IsNullOrWhiteSpace(Input.ThresholdProfileCode) ? null : Input.ThresholdProfileCode.Trim()
        });
        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}

