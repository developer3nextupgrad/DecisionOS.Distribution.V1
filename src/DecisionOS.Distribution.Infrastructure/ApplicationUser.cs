using Microsoft.AspNetCore.Identity;

namespace DecisionOS.Distribution.Infrastructure;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }
}
