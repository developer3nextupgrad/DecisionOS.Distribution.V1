using DecisionOS.Distribution.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Web.Pages.Admin.Users;

public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    public IReadOnlyList<UserRow> Items { get; private set; } = Array.Empty<UserRow>();

    public sealed record UserRow(Guid Id, string Email, string Roles);

    public async Task OnGetAsync()
    {
        var list = new List<UserRow>();
        foreach (var u in await _userManager.Users.OrderBy(x => x.Email!).ToListAsync())
        {
            var roles = await _userManager.GetRolesAsync(u);
            list.Add(new UserRow(u.Id, u.Email ?? u.UserName ?? "", string.Join(", ", roles.OrderBy(r => r))));
        }

        Items = list;
    }
}
