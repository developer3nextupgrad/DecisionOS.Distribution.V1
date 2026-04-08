using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.Tenants;

public class DeleteModel : PageModel
{
    private readonly DecisionOsDbContext _db;
    public DeleteModel(DecisionOsDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public Domain.Tenant? Tenant { get; private set; }
    public bool CanDelete { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == Id);
        if (Tenant is null) return NotFound();
        CanDelete = !await _db.KpiSnapshots.AnyAsync(s => s.TenantId == Id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var t = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == Id);
        if (t is null) return NotFound();
        if (await _db.KpiSnapshots.AnyAsync(s => s.TenantId == Id))
            return RedirectToPage("Delete", new { Id });

        _db.Tenants.Remove(t);
        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
