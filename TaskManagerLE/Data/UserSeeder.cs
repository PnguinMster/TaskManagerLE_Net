using Microsoft.AspNetCore.Identity;
using TaskManagerLE.Authorization;
using TaskManagerLE.Models;

namespace TaskManagerLE.Data;

public static class UserSeeder
{
    private const string AdminSection = "SeedAdmin";

    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration config)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var timeProvider = serviceProvider.GetRequiredService<TimeProvider>();
        ILogger logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(UserSeeder));

        await SeedRolesAsync(roleManager);
        await SeedAdminAsync(userManager, config, timeProvider, logger);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (string role in new[] { Roles.Admin, Roles.User }) {
            if (await roleManager.RoleExistsAsync(role))
                continue;

            EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole(role)), $"create role '{role}'");
        }
    }

    private static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager, IConfiguration config,
        TimeProvider timeProvider, ILogger logger)
    {
        string? username = config[$"{AdminSection}:UserName"];
        string? email = config[$"{AdminSection}:Email"];
        string? password = config[$"{AdminSection}:Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) {
            logger.LogWarning("Admin seeding skipped: {Section} is not fully configured (UserName, Email, Password).", AdminSection);
            return;
        }

        ApplicationUser? admin = await userManager.FindByEmailAsync(email);

        if (admin is null) {
            admin = new ApplicationUser
            {
                UserName = username,
                Email = email,
                EmailConfirmed = true,
                CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
            };

            EnsureSucceeded(await userManager.CreateAsync(admin, password), $"create admin user '{email}'");
            logger.LogInformation("Seeded the admin user {UserId}.", admin.Id);
        }

        if (!await userManager.IsInRoleAsync(admin, Roles.Admin)) {
            EnsureSucceeded(await userManager.AddToRoleAsync(admin, Roles.Admin), $"add '{email}' to role '{Roles.Admin}'");
            logger.LogInformation("Granted the '{Role}' role to the admin user.", Roles.Admin);
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (result.Succeeded)
            return;

        string errors = string.Join(", ", result.Errors.Select(e => e.Description));
        throw new InvalidOperationException($"Seeding failed to {action}: {errors}");
    }
}