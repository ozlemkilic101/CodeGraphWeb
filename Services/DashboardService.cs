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
        var projectRole = await ResolveProjectRoleAsync(userId, cancellationToken);

        var model = role switch
        {
            Roles.SuperAdmin => await BuildSuperAdminModelAsync(cancellationToken),
            Roles.Admin => await BuildAdminModelAsync(userId, cancellationToken),
            _ => await BuildUserModelAsync(userId, projectRole, cancellationToken)
        };

        model.Role = role;
        model.ProjectRole = projectRole;
        model.WelcomeMessage = BuildWelcomeMessage(role, projectRole);

        return model;
    }

    private async Task<DashboardViewModel> BuildSuperAdminModelAsync(CancellationToken cancellationToken)
    {
        var users = await _dbContext.Users
            .OrderByDescending(x => x.Id)
            .Take(5)
            .Select(x => new UserDto
            {
                FullName = string.IsNullOrWhiteSpace(x.FullName) ? "Isimsiz Kullanici" : x.FullName,
                Email = x.Email ?? "-"
            })
            .ToListAsync(cancellationToken);

        var roleDistribution = await (from role in _dbContext.Roles
                                      join userRole in _dbContext.UserRoles on role.Id equals userRole.RoleId into grouped
                                      select new { Name = role.Name ?? string.Empty, Count = grouped.Count() })
            .ToDictionaryAsync(x => x.Name, x => x.Count, cancellationToken);

        var projects = await _dbContext.Projects
            .OrderByDescending(x => x.CreatedAt)
            .Take(8)
            .Select(MapProject)
            .ToListAsync(cancellationToken);

        var recentActivities = await _dbContext.AnalysisResults
            .OrderByDescending(x => x.GeneratedAt)
            .Take(8)
            .Select(x => new ActivityLogDto
            {
                Date = x.GeneratedAt,
                Action = "Analiz calistirildi",
                Detail = x.Summary
            })
            .ToListAsync(cancellationToken);

        return new DashboardViewModel
        {
            TotalUsers = await _dbContext.Users.CountAsync(cancellationToken),
            TotalProjects = await _dbContext.Projects.CountAsync(cancellationToken),
            TotalRisks = await _dbContext.Risks.CountAsync(cancellationToken),
            RecentUsers = users,
            RoleDistribution = roleDistribution,
            Projects = projects,
            RecentActivities = recentActivities
        };
    }

    private async Task<DashboardViewModel> BuildAdminModelAsync(string userId, CancellationToken cancellationToken)
    {
        // Company mapping modeli henuz olmadigi icin Admin scope'u adminin dahil oldugu projelerden hesaplanir.
        var projectIds = await _dbContext.ProjectMembers
            .Where(x => x.UserId == userId)
            .Select(x => x.ProjectId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var projects = await _dbContext.Projects
            .Where(x => projectIds.Contains(x.Id))
            .OrderByDescending(x => x.CreatedAt)
            .Select(MapProject)
            .ToListAsync(cancellationToken);

        var scopedUserIds = await _dbContext.ProjectMembers
            .Where(x => projectIds.Contains(x.ProjectId))
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var activities = await _dbContext.AnalysisResults
            .Where(x => projectIds.Contains(x.ProjectId))
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
            Projects = projects,
            TotalProjects = projectIds.Count,
            TotalUsers = scopedUserIds.Count,
            TotalRisks = await _dbContext.Risks.CountAsync(x => projectIds.Contains(x.ProjectId), cancellationToken),
            RecentActivities = activities
        };
    }

    private async Task<DashboardViewModel> BuildUserModelAsync(string userId, string? projectRole, CancellationToken cancellationToken)
    {
        var projectIds = await _dbContext.ProjectMembers
            .Where(x => x.UserId == userId)
            .Select(x => x.ProjectId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var projects = await _dbContext.Projects
            .Where(x => projectIds.Contains(x.Id))
            .OrderByDescending(x => x.CreatedAt)
            .Select(MapProject)
            .ToListAsync(cancellationToken);

        var risks = await _dbContext.Risks
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
            TotalProjects = projectIds.Count,
            TotalRisks = risks.Count,
            RecentActivities = activities,
            TotalUsers = 1,
            ProjectRole = projectRole
        };
    }

    private async Task<string?> ResolveProjectRoleAsync(string userId, CancellationToken cancellationToken)
    {
        var projectRoles = await _dbContext.ProjectMembers
            .Where(x => x.UserId == userId)
            .Select(x => x.Role)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (projectRoles.Contains(ProjectRoles.TechLead))
        {
            return ProjectRoles.TechLead;
        }

        if (projectRoles.Contains(ProjectRoles.Developer))
        {
            return ProjectRoles.Developer;
        }

        return null;
    }

    private static string ResolveGlobalRole(IList<string> roles)
    {
        if (roles.Contains(Roles.SuperAdmin))
        {
            return Roles.SuperAdmin;
        }

        if (roles.Contains(Roles.Admin))
        {
            return Roles.Admin;
        }

        return Roles.User;
    }

    private static string BuildWelcomeMessage(string role, string? projectRole)
    {
        return role switch
        {
            Roles.SuperAdmin => "Platformin tamamini yonetebileceginiz SuperAdmin panelindesiniz.",
            Roles.Admin => "Sirket kaynaklarini ve ekipleri yonettiginiz Admin panelindesiniz.",
            _ when projectRole == ProjectRoles.TechLead => "TechLead paneline hos geldiniz. Analiz ve ekip yonetimi burada.",
            _ => "Developer paneline hos geldiniz. Projelerinizi ve risklerinizi takip edin."
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
