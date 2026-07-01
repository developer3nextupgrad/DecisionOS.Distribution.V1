using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DecisionOS.Distribution.Web.Pages.Shared;

namespace DecisionOS.Distribution.Web.Pages;

[Authorize(Policy = "AnyDistributionRole")]
public class IndexModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    private readonly DashboardContextService _context;

    public IndexModel(DecisionOsDbContext db, DashboardContextService context)
    {
        _db = db;
        _context = context;
    }

    public ContextSelectorViewModel Selector { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SelectedClientId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedCustomerId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedPeriodEnd { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await BuildSelectorAsync();

        if (!string.IsNullOrEmpty(SelectedClientId) && string.IsNullOrEmpty(SelectedPeriodEnd))
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.ClientId == SelectedClientId);
            if (tenant is not null)
            {
                var weeks = await _context.GetWeeksAsync(tenant.Id, SelectedCustomerId);
                if (weeks.Count > 0)
                {
                    return RedirectToPage("Dashboard", new
                    {
                        clientId = SelectedClientId,
                        periodEnd = weeks[0].ToString("yyyy-MM-dd"),
                        customerId = SelectedCustomerId
                    });
                }
            }
        }

        if (!string.IsNullOrEmpty(SelectedClientId) && !string.IsNullOrEmpty(SelectedPeriodEnd))
        {
            return RedirectToPage("Dashboard", new
            {
                clientId = SelectedClientId,
                periodEnd = SelectedPeriodEnd,
                customerId = SelectedCustomerId
            });
        }

        return Page();
    }

    private async Task BuildSelectorAsync()
    {
        var tenants = await _db.Tenants.OrderBy(t => t.Name).ToListAsync();
        var tenantItems = tenants.Select(t => new SelectListItem(t.Name, t.ClientId)).ToList();
        tenantItems.Insert(0, new SelectListItem("— Select distributor —", ""));

        var customerItems = new List<SelectListItem>
        {
            new("All buyers (distributor total)", "")
        };
        var weekItems = new List<SelectListItem>
        {
            new("— Select week —", "")
        };

        if (!string.IsNullOrEmpty(SelectedClientId))
        {
            var tenant = tenants.FirstOrDefault(t => t.ClientId == SelectedClientId);
            if (tenant is not null)
            {
                var customers = await _context.GetCustomersAsync(tenant.Id);
                customerItems.AddRange(customers.Select(c =>
                    new SelectListItem(c.DisplayName, c.CustomerId)));

                var weeks = await _context.GetWeeksAsync(tenant.Id, SelectedCustomerId);
                weekItems.AddRange(weeks.Select(w =>
                    new SelectListItem(w.ToString("yyyy-MM-dd"), w.ToString("yyyy-MM-dd"))));
            }
        }

        Selector = new ContextSelectorViewModel
        {
            Tenants = tenantItems,
            Customers = customerItems,
            Weeks = weekItems,
            SelectedClientId = SelectedClientId,
            SelectedCustomerId = SelectedCustomerId ?? "",
            SelectedPeriodEnd = SelectedPeriodEnd,
            ShowGoButton = false
        };
    }
}
