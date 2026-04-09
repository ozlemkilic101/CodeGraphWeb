namespace CodeGraphWeb.ViewModels;

public class DashboardViewModel
{
    public string Role { get; set; } = string.Empty;
    public string? ProjectRole { get; set; }
    public string WelcomeMessage { get; set; } = string.Empty;

    public int TotalCompanies { get; set; }
    public List<ProjectDto> Projects { get; set; } = [];
    public int TotalUsers { get; set; }
    public int TotalProjects { get; set; }
    public int TotalRisks { get; set; }

    public List<CompanyUsersDto> CompanyGroups { get; set; } = [];
    public List<ActivityLogDto> RecentActivities { get; set; } = [];
    public List<UserDto> RecentUsers { get; set; } = [];
    public Dictionary<string, int> RoleDistribution { get; set; } = new();
    public List<RiskDto> Risks { get; set; } = [];
}

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ActivityLogDto
{
    public DateTime Date { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Detail { get; set; }
}

public class UserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class RiskDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Severity { get; set; } = "Unknown";
    public string Description { get; set; } = string.Empty;
}

public class CompanyUsersDto
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public List<CompanyUserDto> Users { get; set; } = [];
}

public class CompanyUserDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
