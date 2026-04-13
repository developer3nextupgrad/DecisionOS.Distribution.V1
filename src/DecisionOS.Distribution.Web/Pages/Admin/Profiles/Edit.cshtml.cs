using System.ComponentModel.DataAnnotations;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.Profiles;

public class EditModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public EditModel(DecisionOsDbContext db) => _db = db;

    public SelectList? VerticalOptions { get; private set; }

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
        public Guid? VerticalLibraryId { get; set; }
        public string? ActiveKpiProfileCode { get; set; }
        public string? LocationStructure { get; set; }
        public string? ChannelStructure { get; set; }
        public string? ThresholdProfileCode { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var p = await _db.BusinessProfiles.FirstOrDefaultAsync(x => x.Id == Id);
        if (p is null) return NotFound();
        var verticals = await _db.VerticalLibraries.Where(v => v.IsActive).OrderBy(v => v.Name).ToListAsync();
        VerticalOptions = new SelectList(verticals, "Id", "Name", p.VerticalLibraryId);
        Input = new InputModel
        {
            Code = p.Code,
            Name = p.Name,
            Description = p.Description,
            IsActive = p.IsActive,
            VerticalLibraryId = p.VerticalLibraryId,
            ActiveKpiProfileCode = p.ActiveKpiProfileCode,
            LocationStructure = p.LocationStructure,
            ChannelStructure = p.ChannelStructure,
            ThresholdProfileCode = p.ThresholdProfileCode
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var p = await _db.BusinessProfiles.FirstOrDefaultAsync(x => x.Id == Id);
        if (p is null) return NotFound();
        var verticals = await _db.VerticalLibraries.Where(v => v.IsActive).OrderBy(v => v.Name).ToListAsync();
        VerticalOptions = new SelectList(verticals, "Id", "Name", Input.VerticalLibraryId);
        if (!ModelState.IsValid)
        {
            Input.Code = p.Code;
            return Page();
        }

        p.Name = Input.Name.Trim();
        p.Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim();
        p.IsActive = Input.IsActive;
        p.VerticalLibraryId = Input.VerticalLibraryId;
        p.ActiveKpiProfileCode = string.IsNullOrWhiteSpace(Input.ActiveKpiProfileCode) ? null : Input.ActiveKpiProfileCode.Trim();
        p.LocationStructure = string.IsNullOrWhiteSpace(Input.LocationStructure) ? null : Input.LocationStructure.Trim();
        p.ChannelStructure = string.IsNullOrWhiteSpace(Input.ChannelStructure) ? null : Input.ChannelStructure.Trim();
        p.ThresholdProfileCode = string.IsNullOrWhiteSpace(Input.ThresholdProfileCode) ? null : Input.ThresholdProfileCode.Trim();
        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}

