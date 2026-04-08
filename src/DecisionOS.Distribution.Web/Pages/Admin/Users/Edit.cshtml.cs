using DecisionOS.Distribution.Domain.Security;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.Users;

public class EditModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public EditModel(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public string[] SelectedRoles { get; set; } = Array.Empty<string>();

    public string? Email { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == Id);
        if (user is null) return NotFound();
        Email = user.Email;
        SelectedRoles = (await _userManager.GetRolesAsync(user)).ToArray();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == Id);
        if (user is null) return NotFound();

        var desired = new HashSet<string>(SelectedRoles ?? Array.Empty<string>(), StringComparer.Ordinal);
        foreach (var r in desired)
        {
            if (!AppRoles.All.Contains(r))
                ModelState.AddModelError(string.Empty, $"Unknown role: {r}");
        }

        if (!ModelState.IsValid)
        {
            Email = user.Email;
            return Page();
        }

        var current = await _userManager.GetRolesAsync(user);
        var toRemove = current.Where(c => !desired.Contains(c)).ToArray();
        var toAdd = desired.Where(d => !current.Contains(d)).ToArray();
        if (toRemove.Length > 0)
            await _userManager.RemoveFromRolesAsync(user, toRemove);
        if (toAdd.Length > 0)
            await _userManager.AddToRolesAsync(user, toAdd);

        return RedirectToPage("Index");
    }
}
