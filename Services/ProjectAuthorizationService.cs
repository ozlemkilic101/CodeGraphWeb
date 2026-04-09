using CodeGraphWeb.Constants;
using CodeGraphWeb.Data;
using Microsoft.EntityFrameworkCore;

namespace CodeGraphWeb.Services;

public interface IProjectAuthorizationService
{
    Task<bool> IsTechLeadAsync(string userId, int projectId, CancellationToken cancellationToken = default);
    Task<List<UserProjectRoleItem>> GetUserProjectRolesAsync(string userId, CancellationToken cancellationToken = default);
}

public sealed class ProjectAuthorizationService : IProjectAuthorizationService
{
    private readonly ApplicationDbContext _dbContext;

    public ProjectAuthorizationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> IsTechLeadAsync(string userId, int projectId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ProjectMembers.AnyAsync(
            x => x.UserId == userId && x.ProjectId == projectId && x.Role == ProjectRoles.TechLead,
            cancellationToken);
    }

    public Task<List<UserProjectRoleItem>> GetUserProjectRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ProjectMembers
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.ProjectId)
            .Select(x => new UserProjectRoleItem
            {
                ProjectId = x.ProjectId,
                ProjectName = x.Project != null ? x.Project.Name : $"Project #{x.ProjectId}",
                Role = x.Role
            })
            .ToListAsync(cancellationToken);
    }
}

public sealed class UserProjectRoleItem
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
