using System.ComponentModel.DataAnnotations;
using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.KpiDefinitions;

public class CreateModel : PageModel
{
    private readonly DecisionOsDbContext _db;

    public CreateModel(DecisionOsDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Code is required")]
        public string Code { get; set; } = "";

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Unit is required")]
        public string Unit { get; set; } = "pct";

        public KpiDirection Direction { get; set; }

        public decimal Target { get; set; }

        public decimal AmberThreshold { get; set; }

        public decimal RedThreshold { get; set; }

        public int AlertPriority { get; set; } = 100;

        public decimal? MinValue { get; set; }

        public decimal? MaxValue { get; set; }

        public string RecommendedAction { get; set; } = "";

        public string DiagnosticChecks { get; set; } = "";
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        string normalizedCode = (Input.Code ?? string.Empty)
            .Trim()
            .ToUpperInvariant();

        bool codeExists = await _db.KpiDefinitions
            .AsNoTracking()
            .AnyAsync(x =>
                x.Code != null &&
                x.Code.Trim().ToUpper() == normalizedCode);

        if (codeExists)
        {
            ModelState.AddModelError(
                "Input.Code",
                $"A KPI with code '{normalizedCode}' already exists.");

            return Page();
        }

        var kpi = new KpiDefinition
        {
            Code = normalizedCode,
            Name = Input.Name.Trim(),
            Unit = Input.Unit.Trim(),
            Direction = Input.Direction,
            Target = Input.Target,
            AmberThreshold = Input.AmberThreshold,
            RedThreshold = Input.RedThreshold,
            AlertPriority = Input.AlertPriority,
            MinValue = Input.MinValue,
            MaxValue = Input.MaxValue,
            RecommendedAction = Input.RecommendedAction?.Trim() ?? "",
            DiagnosticChecks = Input.DiagnosticChecks?.Trim() ?? ""
        };

        _db.KpiDefinitions.Add(kpi);

        await _db.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}