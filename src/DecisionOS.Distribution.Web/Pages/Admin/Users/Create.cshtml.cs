using System.ComponentModel.DataAnnotations;
using DecisionOS.Distribution.Domain.Security;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DecisionOS.Distribution.Web.Pages.Admin.Users;

public class CreateModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, MinLength(8), DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [MaxLength(200)]
        public string? DisplayName { get; set; }
    }

    [BindProperty]
    public string[] SelectedRoles { get; set; } = { AppRoles.Viewer };

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        if (await _userManager.FindByEmailAsync(Input.Email) is not null)
        {
            ModelState.AddModelError(nameof(Input.Email), "A user with this email already exists.");
            return Page();
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = Input.Email.Trim(),
            Email = Input.Email.Trim(),
            EmailConfirmed = true,
            DisplayName = string.IsNullOrWhiteSpace(Input.DisplayName) ? null : Input.DisplayName.Trim()
        };
        var created = await _userManager.CreateAsync(user, Input.Password);
        if (!created.Succeeded)
        {
            foreach (var e in created.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            return Page();
        }

        var roles = (SelectedRoles ?? Array.Empty<string>()).Where(AppRoles.All.Contains).Distinct().ToList();
        if (roles.Count == 0)
            roles.Add(AppRoles.Viewer);
        await _userManager.AddToRolesAsync(user, roles);

        return RedirectToPage("Index");
    }
}
