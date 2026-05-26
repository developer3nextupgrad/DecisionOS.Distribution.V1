using System.ComponentModel.DataAnnotations;
using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.Influencers;

public class CreateModel : PageModel
{
    private readonly DecisionOsDbContext _db;

    public CreateModel(DecisionOsDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? ProfileId { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList? PillarOptions { get; private set; }

    public SelectList? DriverOptions { get; private set; }

    public class InputModel
    {
        [Required]
        [MaxLength(64)]
        public string PillarCode { get; set; } = "";

        [Required]
        [MaxLength(120)]
        public string DriverCode { get; set; } = "";

        [Required]
        [MaxLength(120)]
        public string InfluencerCode { get; set; } = "";

        [Required]
        [MaxLength(500)]
        public string DisplayName { get; set; } = "";

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Range(0, 100)]
        public int Weight { get; set; } = 50;

        public InfluencerImpactDirection Direction { get; set; }
            = InfluencerImpactDirection.Neutral;

        public bool IsActive { get; set; } = true;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (ProfileId is null)
            return RedirectToPage("Index");

        await LoadOptionsAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ProfileId is null)
            return RedirectToPage("Index");

        await LoadOptionsAsync();

        if (!ModelState.IsValid)
            return Page();

        var pillar = Input.PillarCode.Trim();

        var driver = Input.DriverCode.Trim();

        var code = Input.InfluencerCode
            .Trim()
            .ToUpperInvariant();

        var driverExists = await _db.DriverDefinitions.AnyAsync(d =>
            (d.BusinessProfileId == ProfileId ||
             d.BusinessProfileId == null) &&
            d.PillarCode == pillar &&
            d.DriverCode == driver);

        if (!driverExists)
        {
            ModelState.AddModelError(
                nameof(Input.DriverCode),
                "Driver does not exist."
            );

            return Page();
        }

        var influencerExists = await _db.InfluencerDefinitions.AnyAsync(i =>
            i.BusinessProfileId == ProfileId &&
            i.PillarCode == pillar &&
            i.DriverCode == driver &&
            i.InfluencerCode == code);

        if (influencerExists)
        {
            ModelState.AddModelError(
                nameof(Input.InfluencerCode),
                "Influencer code already exists."
            );

            return Page();
        }

        _db.InfluencerDefinitions.Add(new InfluencerDefinition
        {
            BusinessProfileId = ProfileId,

            PillarCode = pillar,

            DriverCode = driver,

            InfluencerCode = code,

            DisplayName = Input.DisplayName.Trim(),

            Description = string.IsNullOrWhiteSpace(Input.Description)
                ? null
                : Input.Description.Trim(),

            Weight = Input.Weight,

            Direction = Input.Direction,

            IsActive = Input.IsActive
        });

        await _db.SaveChangesAsync();

        return RedirectToPage(
            "Index",
            new { profileId = ProfileId }
        );
    }

    private async Task LoadOptionsAsync()
    {
        // LOAD KPI PILLARS
        var pillars = await _db.KpiDefinitions
            .Where(k =>
                k.BusinessProfileId == ProfileId ||
                k.BusinessProfileId == null)
            .OrderBy(k => k.Code)
            .Select(k => k.Code)
            .Distinct()
            .ToListAsync();

        PillarOptions = new SelectList(pillars);

        // LOAD DRIVERS
        var drivers = await _db.DriverDefinitions
            .Where(d =>
                d.BusinessProfileId == ProfileId ||
                d.BusinessProfileId == null)
            .OrderBy(d => d.PillarCode)
            .ThenBy(d => d.DriverCode)
            .Select(d => new
            {
                Value = d.DriverCode,
                Label = d.PillarCode + " / " + d.DriverCode
            })
            .ToListAsync();

        DriverOptions = new SelectList(
            drivers,
            "Value",
            "Label"
        );

        // DEFAULT SELECTED VALUES
        if (string.IsNullOrWhiteSpace(Input.PillarCode)
            && pillars.Count > 0)
        {
            Input.PillarCode = pillars[0];
        }

        if (string.IsNullOrWhiteSpace(Input.DriverCode)
            && drivers.Count > 0)
        {
            Input.DriverCode = drivers[0].Value;
        }
    }
}