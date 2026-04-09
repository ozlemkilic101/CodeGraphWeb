using CodeGraphWeb.Constants;
using CodeGraphWeb.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CodeGraphWeb.Data;

public static class IdentitySeed
{
    private static readonly string[] RolePriority =
    [
        Roles.SystemAdmin,
        Roles.CompanyAdmin,
        Roles.TechLead,
        Roles.User
    ];

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
                    throw new InvalidOperationException($"Rol olusturulamadi ({role}): {roleErrors}");
                }
            }
        }

        const string systemAdminEmail = "admin@codegraph.com";
        const string systemAdminPassword = "Admin123!";
        const string systemAdminFullName = "System Owner";

        var codeGraphCompany = await dbContext.Companies.FirstOrDefaultAsync(x => x.Name == "CodeGraph");
        if (codeGraphCompany is null)
        {
            codeGraphCompany = new Company
            {
                Name = "CodeGraph",
                SubscriptionId = 999
            };
            dbContext.Companies.Add(codeGraphCompany);
            await dbContext.SaveChangesAsync();
        }

        var systemAdminUser = await userManager.FindByEmailAsync(systemAdminEmail);
        if (systemAdminUser is null)
        {
            systemAdminUser = new ApplicationUser
            {
                FullName = systemAdminFullName,
                UserName = systemAdminEmail,
                Email = systemAdminEmail,
                EmailConfirmed = true,
                CompanyId = codeGraphCompany.Id
            };

            var createResult = await userManager.CreateAsync(systemAdminUser, systemAdminPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Varsayilan SystemAdmin olusturulamadi: {errors}");
            }
        }
        else if (systemAdminUser.CompanyId != codeGraphCompany.Id)
        {
            systemAdminUser.CompanyId = codeGraphCompany.Id;
            await userManager.UpdateAsync(systemAdminUser);
        }

        await NormalizeUserRolesAsync(userManager, systemAdminEmail);
    }

    private static async Task NormalizeUserRolesAsync(UserManager<ApplicationUser> userManager, string systemAdminEmail)
    {
        var users = await userManager.Users.ToListAsync();

        foreach (var user in users)
        {
            var currentRoles = await userManager.GetRolesAsync(user);
            var normalizedRoles = currentRoles
                .Select(NormalizeRoleName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var targetRole =
                user.Email != null && user.Email.Equals(systemAdminEmail, StringComparison.OrdinalIgnoreCase)
                    ? Roles.SystemAdmin
                    : RolePriority.FirstOrDefault(normalizedRoles.Contains) ?? Roles.User;

            var hasSingleCorrectRole = currentRoles.Count == 1 && currentRoles.Contains(targetRole);
            if (hasSingleCorrectRole)
            {
                continue;
            }

            if (currentRoles.Count > 0)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(x => x.Description));
                    throw new InvalidOperationException($"Kullanici rolleri temizlenemedi ({user.Email}): {errors}");
                }
            }

            var addResult = await userManager.AddToRoleAsync(user, targetRole);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Kullaniciya rol atanamadi ({user.Email}): {errors}");
            }
        }
    }

    private static string NormalizeRoleName(string role)
    {
        return role switch
        {
            Roles.SystemAdmin or "SuperAdmin" => Roles.SystemAdmin,
            Roles.CompanyAdmin or "Admin" or "CompanyLead" => Roles.CompanyAdmin,
            Roles.TechLead => Roles.TechLead,
            Roles.User or "Developer" or "Viewer" => Roles.User,
            _ => Roles.User
        };
    }
}
