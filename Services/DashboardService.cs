using System.Linq.Expressions;
using CodeGraphWeb.Constants;
using CodeGraphWeb.Data;
using CodeGraphWeb.Models;
using CodeGraphWeb.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CodeGraphWeb.Services;

public interface IDashboardService
{
    Task<DashboardViewModel?> BuildAsync(string userId, CancellationToken cancellationToken = default);
    Task<DashboardViewModel?> BuildForRoleAsync(string userId, string role, CancellationToken cancellationToken = default);
}

public sealed class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardService(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<DashboardViewModel?> BuildAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = ResolveGlobalRole(roles);

        return await BuildForRoleAsync(userId, role, cancellationToken);
    }

    public async Task<DashboardViewModel?> BuildForRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        if (!userRoles.Contains(role))
        {
            return null;
        }

        var projectRole = await ResolveProjectRoleAsync(userId, cancellationToken);

        var model = role switch
        {
            Roles.SystemAdmin => await BuildSystemAdminModelAsync(cancellationToken),
            Roles.CompanyAdmin => await BuildCompanyAdminModelAsync(user, cancellationToken),
            Roles.TechLead => await BuildAssignedProjectModelAsync(userId, projectRole, cancellationToken),
            Roles.User => await BuildAssignedProjectModelAsync(userId, projectRole, cancellationToken),
            _ => null
        };

        if (model is null)
        {
            return null;
        }

        model.Role = role;
        model.ProjectRole = projectRole;
        model.WelcomeMessage = BuildWelcomeMessage(role, projectRole);

        return model;
    }

    private async Task<DashboardViewModel> BuildSystemAdminModelAsync(CancellationToken cancellationToken)
    {
        var companies = await _dbContext.Companies
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                Name = x.Name ?? $"Sirket #{x.Id}"
            })
            .ToListAsync(cancellationToken);

        var companyUsers = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.CompanyId.HasValue)
            .Select(x => new
            {
                x.Id,
                x.CompanyId,
                x.Email,
                x.FullName
            })
            .ToListAsync(cancellationToken);

        var userIds = companyUsers.Select(x => x.Id).ToList();
        var roleRows = await (from ur in _dbContext.UserRoles
                              join r in _dbContext.Roles on ur.RoleId equals r.Id
                              where userIds.Contains(ur.UserId)
                              select new
                              {
                                  ur.UserId,
                                  Role = r.Name ?? Roles.User
                              })
            .ToListAsync(cancellationToken);

        static string ResolveCurrentRole(string userId, IEnumerable<dynamic> rows)
        {
            var order = new[] { Roles.SystemAdmin, Roles.CompanyAdmin, Roles.TechLead, Roles.User };
            var set = rows.Where(x => x.UserId == userId).Select(x => (string)x.Role).ToHashSet();
            return order.FirstOrDefault(set.Contains) ?? Roles.User;
        }

        var groupedUsers = companyUsers
            .Where(x => x.CompanyId.HasValue)
            .GroupBy(x => x.CompanyId!.Value)
            .ToDictionary(
                x => x.Key,
                x => x
                    .OrderBy(u => string.IsNullOrWhiteSpace(u.FullName) ? u.Email : u.FullName)
                    .Select(u => new CompanyUserDto
                    {
                        UserId = u.Id,
                        FullName = string.IsNullOrWhiteSpace(u.FullName) ? "Isimsiz Kullanici" : u.FullName,
                        Email = u.Email ?? "-",
                        Role = ResolveCurrentRole(u.Id, roleRows)
                    })
                    .ToList());

        var companyGroups = companies
            .Select(company => new CompanyUsersDto
            {
                CompanyId = company.Id,
                CompanyName = company.Name,
                Users = groupedUsers.TryGetValue(company.Id, out var users) ? users : []
            })
            .ToList();

        var roleDistribution = await (from role in _dbContext.Roles
                                      join userRole in _dbContext.UserRoles on role.Id equals userRole.RoleId into grouped
                                      select new { Name = role.Name ?? string.Empty, Count = grouped.Count() })
            .ToDictionaryAsync(x => x.Name, x => x.Count, cancellationToken);

        return new DashboardViewModel
        {
            TotalCompanies = companies.Count,
            TotalUsers = companyUsers.Count,
            RoleDistribution = roleDistribution,
            CompanyGroups = companyGroups
        };
    }

    private async Task<DashboardViewModel> BuildCompanyAdminModelAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (!user.CompanyId.HasValue)
        {
            return new DashboardViewModel();
        }

        var companyId = user.CompanyId.Value;
        var companyProjectIds = await _dbContext.Projects
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var projects = await _dbContext.Projects
            .AsNoTracking()
            .Where(x => companyProjectIds.Contains(x.Id))
            .OrderByDescending(x => x.CreatedAt)
            .Select(MapProject)
            .ToListAsync(cancellationToken);

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.FullName)
            .Select(x => new UserDto
            {
                FullName = string.IsNullOrWhiteSpace(x.FullName) ? "Isimsiz Kullanici" : x.FullName,
                Email = x.Email ?? "-"
            })
            .ToListAsync(cancellationToken);

        var activities = await _dbContext.AnalysisResults
            .AsNoTracking()
            .Where(x => companyProjectIds.Contains(x.ProjectId))
            .OrderByDescending(x => x.GeneratedAt)
            .Take(8)
            .Select(x => new ActivityLogDto
            {
                Date = x.GeneratedAt,
                Action = "Proje analizi tamamlandi",
                Detail = x.Summary
            })
            .ToListAsync(cancellationToken);

        return new DashboardViewModel
        {
            TotalUsers = users.Count,
            TotalProjects = projects.Count,
            TotalRisks = await _dbContext.Risks.CountAsync(x => companyProjectIds.Contains(x.ProjectId), cancellationToken),
            Projects = projects,
            RecentUsers = users.Take(8).ToList(),
            RecentActivities = activities
        };
    }

    private async Task<DashboardViewModel> BuildAssignedProjectModelAsync(
        string userId,
        string? projectRole,
        CancellationToken cancellationToken)
    {
        var projectIds = await _dbContext.ProjectMembers
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.ProjectId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var projects = await _dbContext.Projects
            .AsNoTracking()
            .Where(x => projectIds.Contains(x.Id))
            .OrderByDescending(x => x.CreatedAt)
            .Select(MapProject)
            .ToListAsync(cancellationToken);

        var risks = await _dbContext.Risks
            .AsNoTracking()
            .Where(x => projectIds.Contains(x.ProjectId))
            .OrderByDescending(x => x.Id)
            .Take(10)
            .Join(_dbContext.Projects, r => r.ProjectId, p => p.Id, (r, p) => new RiskDto
            {
                ProjectId = p.Id,
                ProjectName = p.Name,
                Severity = string.IsNullOrWhiteSpace(r.Severity) ? "Unknown" : r.Severity!,
                Description = string.IsNullOrWhiteSpace(r.Description) ? "Aciklama bulunamadi." : r.Description!
            })
            .ToListAsync(cancellationToken);

        var activities = await _dbContext.AnalysisResults
            .AsNoTracking()
            .Where(x => projectIds.Contains(x.ProjectId))
            .OrderByDescending(x => x.GeneratedAt)
            .Take(8)
            .Select(x => new ActivityLogDto
            {
                Date = x.GeneratedAt,
                Action = "Analiz guncellendi",
                Detail = x.Summary
            })
            .ToListAsync(cancellationToken);

        return new DashboardViewModel
        {
            Projects = projects,
            Risks = risks,
            TotalProjects = projects.Count,
            TotalRisks = risks.Count,
            RecentActivities = activities,
            TotalUsers = 1,
            ProjectRole = projectRole
        };
    }

    private async Task<string?> ResolveProjectRoleAsync(string userId, CancellationToken cancellationToken)
    {
        var projectRoles = await _dbContext.ProjectMembers
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.Role)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (projectRoles.Contains(ProjectRoles.TechLead))
        {
            return ProjectRoles.TechLead;
        }

        if (projectRoles.Contains(ProjectRoles.User))
        {
            return ProjectRoles.User;
        }

        return null;
    }

    private static string ResolveGlobalRole(IList<string> roles)
    {
        if (roles.Contains(Roles.SystemAdmin))
        {
            return Roles.SystemAdmin;
        }

        if (roles.Contains(Roles.CompanyAdmin))
        {
            return Roles.CompanyAdmin;
        }

        if (roles.Contains(Roles.TechLead))
        {
            return Roles.TechLead;
        }

        if (roles.Contains(Roles.User))
        {
            return Roles.User;
        }

        return Roles.User;
    }

    private static string BuildWelcomeMessage(string role, string? projectRole)
    {
        return role switch
        {
            Roles.SystemAdmin => "Sistem yonetimi panelindesiniz. Sirketleri ve kullanici rollerini buradan yonetebilirsiniz.",
            Roles.CompanyAdmin => "Sirket yonetim panelindesiniz. Takim ve proje yonetimini buradan yapabilirsiniz.",
            Roles.TechLead => "TechLead paneline hos geldiniz. Atanmis projeleriniz uzerinde islem yapabilirsiniz.",
            Roles.User => "Atandiginiz projeleri read-only olarak goruyorsunuz.",
            _ when projectRole == ProjectRoles.TechLead => "TechLead paneline hos geldiniz.",
            _ => "Dashboard'a hos geldiniz."
        };
    }

    private static readonly Expression<Func<Project, ProjectDto>> MapProject = x => new ProjectDto
    {
        Id = x.Id,
        Name = x.Name,
        Description = x.Description,
        CreatedAt = x.CreatedAt
    };
}
