using System.ComponentModel.DataAnnotations;
using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.KpiDefinitions;

public class EditModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public EditModel(DecisionOsDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public string Code { get; set; } = "";
        [Required, MaxLength(500)]
        public string Name { get; set; } = "";
        [Required, MaxLength(32)]
        public string Unit { get; set; } = "pct";
        public KpiDirection Direction { get; set; }
        public decimal Target { get; set; }
        public decimal AmberThreshold { get; set; }
        public decimal RedThreshold { get; set; }
        [Range(1, 1000)]
        public int AlertPriority { get; set; } = 100;
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        [Required]
        public string RecommendedAction { get; set; } = "";
        [Required]
        public string DiagnosticChecks { get; set; } = "";
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var k = await _db.KpiDefinitions.FirstOrDefaultAsync(x => x.Id == Id);
        if (k is null) return NotFound();
        Input = new InputModel
        {
            Code = k.Code,
            Name = k.Name,
            Unit = k.Unit,
            Direction = k.Direction,
            Target = k.Target,
            AmberThreshold = k.AmberThreshold,
            RedThreshold = k.RedThreshold,
            AlertPriority = k.AlertPriority,
            MinValue = k.MinValue,
            MaxValue = k.MaxValue,
            RecommendedAction = k.RecommendedAction,
            DiagnosticChecks = k.DiagnosticChecks
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var k = await _db.KpiDefinitions.FirstOrDefaultAsync(x => x.Id == Id);
        if (k is null) return NotFound();
        if (!ModelState.IsValid)
        {
            Input.Code = k.Code;
            return Page();
        }

        k.Name = Input.Name.Trim();
        k.Unit = Input.Unit.Trim();
        k.Direction = Input.Direction;
        k.Target = Input.Target;
        k.AmberThreshold = Input.AmberThreshold;
        k.RedThreshold = Input.RedThreshold;
        k.AlertPriority = Input.AlertPriority;
        k.MinValue = Input.MinValue;
        k.MaxValue = Input.MaxValue;
        k.RecommendedAction = Input.RecommendedAction.Trim();
        k.DiagnosticChecks = Input.DiagnosticChecks.Trim();
        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
