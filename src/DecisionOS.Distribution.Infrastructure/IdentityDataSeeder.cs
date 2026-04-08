using DecisionOS.Distribution.Domain.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DecisionOS.Distribution.Infrastructure;

public static class IdentityDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DecisionOsDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(IdentityDataSeeder));

        foreach (var roleName in AppRoles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var role = new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = roleName };
            var r = await roleManager.CreateAsync(role);
            if (!r.Succeeded)
                logger.LogError("Create role {Role} failed: {Errors}", roleName,
                    string.Join("; ", r.Errors.Select(e => e.Description)));
        }

        var config = scope.ServiceProvider.GetService<IConfiguration>();
        var adminEmail = config?["SeedAdmin:Email"] ?? "admin@decisionos.local";
        var adminPassword = config?["SeedAdmin:Password"] ?? "ChangeMe!DecisionOS1";

        if (string.IsNullOrWhiteSpace(adminPassword) || adminPassword.Length < 8)
        {
            logger.LogWarning("SeedAdmin:Password missing or too short; skipping admin user seed.");
            return;
        }

        var admin = await userManager.Users.FirstOrDefaultAsync(u => u.Email == adminEmail, cancellationToken);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                DisplayName = "System Administrator"
            };
            var created = await userManager.CreateAsync(admin, adminPassword);
            if (!created.Succeeded)
            {
                logger.LogError("Admin user create failed: {Errors}",
                    string.Join("; ", created.Errors.Select(e => e.Description)));
                return;
            }
        }

        foreach (var r in AppRoles.All)
        {
            if (!await userManager.IsInRoleAsync(admin, r))
                await userManager.AddToRoleAsync(admin, r);
        }
    }
}
