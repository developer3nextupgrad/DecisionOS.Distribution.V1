using System.ComponentModel.DataAnnotations;
using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.Influencers;

public class EditModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public EditModel(DecisionOsDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public Guid? ProfileId { get; private set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public string PillarCode { get; set; } = "";
        public string DriverCode { get; set; } = "";
        public string InfluencerCode { get; set; } = "";
        [Required, MaxLength(500)]
        public string DisplayName { get; set; } = "";
        [MaxLength(2000)]
        public string? Description { get; set; }
        [Range(0, 100)]
        public int Weight { get; set; } = 50;
        public InfluencerImpactDirection Direction { get; set; } = InfluencerImpactDirection.Neutral;
        public bool IsActive { get; set; } = true;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var i = await _db.InfluencerDefinitions.FirstOrDefaultAsync(x => x.Id == Id);
        if (i is null) return NotFound();
        ProfileId = i.BusinessProfileId;
        Input = new InputModel
        {
            PillarCode = i.PillarCode,
            DriverCode = i.DriverCode,
            InfluencerCode = i.InfluencerCode,
            DisplayName = i.DisplayName,
            Description = i.Description,
            Weight = i.Weight,
            Direction = i.Direction,
            IsActive = i.IsActive
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var i = await _db.InfluencerDefinitions.FirstOrDefaultAsync(x => x.Id == Id);
        if (i is null) return NotFound();
        ProfileId = i.BusinessProfileId;
        if (!ModelState.IsValid) return Page();

        i.DisplayName = Input.DisplayName.Trim();
        i.Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim();
        i.Weight = Input.Weight;
        i.Direction = Input.Direction;
        i.IsActive = Input.IsActive;
        await _db.SaveChangesAsync();
        return RedirectToPage("Index", new { profileId = ProfileId });
    }
}

