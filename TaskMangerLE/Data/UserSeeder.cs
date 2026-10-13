using Microsoft.AspNetCore.Identity;
using TaskMangerLE.Models;

namespace TaskMangerLE.Data;

public static class UserSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        RoleManager<IdentityRole> roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        UserManager<ApplicationUser> userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Create Roles
        foreach (var role in new[] { "Admin", "User" }) {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Admin Read
        string? email = config["SeedAdmin:Email"];
        string? password = config["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        // Admin User
        ApplicationUser? admin = await userManager.FindByEmailAsync(email);
        if (admin == null) {
            admin = new ApplicationUser
            {
                DisplayName = "Administrator",
                Email = email,
                EmailConfirmed = false // Warning: Hope I remember to change
            };

            IdentityResult result = await userManager.CreateAsync(admin, password);
            if (!result.Succeeded)
                throw new Exception("Admin Seeding Failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Check Admin
        if (!await userManager.IsInRoleAsync(admin, "Admin")) {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}