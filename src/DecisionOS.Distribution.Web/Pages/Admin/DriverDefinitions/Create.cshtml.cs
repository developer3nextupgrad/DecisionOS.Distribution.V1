using System.ComponentModel.DataAnnotations;
using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.DriverDefinitions;

public class CreateModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public CreateModel(DecisionOsDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public Guid? ProfileId { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList? PillarOptions { get; private set; }

    public class InputModel
    {
        [Required, MaxLength(64)]
        public string PillarCode { get; set; } = "";
        [Required, MaxLength(120)]
        public string DriverCode { get; set; } = "";
        [Required, MaxLength(500)]
        public string DisplayName { get; set; } = "";
        [MaxLength(2000)]
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public async Task OnGetAsync()
    {
        var codes = await _db.KpiDefinitions
            .Where(k => k.BusinessProfileId == ProfileId)
            .OrderBy(k => k.Code)
            .Select(k => k.Code)
            .ToListAsync();
        PillarOptions = new SelectList(codes);
        if (string.IsNullOrWhiteSpace(Input.PillarCode) && codes.Count > 0)
            Input.PillarCode = codes[0];
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var codes = await _db.KpiDefinitions
            .Where(k => k.BusinessProfileId == ProfileId)
            .OrderBy(k => k.Code)
            .Select(k => k.Code)
            .ToListAsync();
        PillarOptions = new SelectList(codes);

        if (!ModelState.IsValid)
            return Page();

        var pillar = Input.PillarCode.Trim();
        var code = Input.DriverCode.Trim();
        if (!await _db.KpiDefinitions.AnyAsync(k => k.BusinessProfileId == ProfileId && k.Code == pillar))
        {
            ModelState.AddModelError(nameof(Input.PillarCode), "Unknown pillar code.");
            return Page();
        }

        if (await _db.DriverDefinitions.AnyAsync(d => d.BusinessProfileId == ProfileId && d.PillarCode == pillar && d.DriverCode == code))
        {
            ModelState.AddModelError(nameof(Input.DriverCode), "This driver code already exists for the pillar.");
            return Page();
        }

        _db.DriverDefinitions.Add(new DriverDefinition
        {
            BusinessProfileId = ProfileId,
            PillarCode = pillar,
            DriverCode = code,
            DisplayName = Input.DisplayName.Trim(),
            Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim(),
            SortOrder = Input.SortOrder,
            IsActive = Input.IsActive
        });
        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
