using CodeGraphWeb.Constants;
using CodeGraphWeb.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CodeGraphWeb.Data;

public static class IdentitySeed
{
    public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await dbContext.Database.MigrateAsync();

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                if (!roleResult.Succeeded)
                {
                    var roleErrors = string.Join(", ", roleResult.Errors.Select(x => x.Description));
                    throw new InvalidOperationException($"Rol oluþturulamadý ({role}): {roleErrors}");
                }
            }
        }

        const string superAdminEmail = "admin@codegraph.com";
        const string superAdminPassword = "Admin123!";
        const string superAdminFullName = "System Owner";

        var superAdminUser = await userManager.FindByEmailAsync(superAdminEmail);
        if (superAdminUser is null)
        {
            superAdminUser = new ApplicationUser
            {
                FullName = superAdminFullName,
                UserName = superAdminEmail,
                Email = superAdminEmail,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(superAdminUser, superAdminPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Varsayýlan SuperAdmin oluþturulamadý: {errors}");
            }
        }

        var existingRoles = await userManager.GetRolesAsync(superAdminUser);
        if (existingRoles.Count > 0)
        {
            await userManager.RemoveFromRolesAsync(superAdminUser, existingRoles);
        }

        if (!await userManager.IsInRoleAsync(superAdminUser, Roles.SuperAdmin))
        {
            var addRoleResult = await userManager.AddToRoleAsync(superAdminUser, Roles.SuperAdmin);
            if (!addRoleResult.Succeeded)
            {
                var errors = string.Join(", ", addRoleResult.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Varsayýlan kullanýcýya SuperAdmin atanamadý: {errors}");
            }
        }
    }
}
