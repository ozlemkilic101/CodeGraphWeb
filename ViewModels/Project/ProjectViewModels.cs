using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CodeGraphWeb.ViewModels.Project;

public sealed class ProjectIndexViewModel
{
    public bool CanCreateProject { get; set; }
    public List<ProjectListItemViewModel> Projects { get; set; } = [];
}

public sealed class ProjectListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ProjectCreatePageViewModel
{
    public CreateProjectInputModel Input { get; set; } = new();
    public List<ProjectCompanyOptionViewModel> Companies { get; set; } = [];
    public List<ProjectUserOptionViewModel> Users { get; set; } = [];
}

public sealed class CreateProjectInputModel
{
    [Required]
    [MaxLength(200)]
    public string ProjectName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Proje dosyasi zorunludur.")]
    public IFormFile? File { get; set; }

    public List<string> AssignedUserIds { get; set; } = [];
}

public sealed class ProjectCompanyOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class ProjectUserOptionViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public sealed class ProjectDetailViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? FileName { get; set; }
    public DateTime UploadDate { get; set; }
    public int MemberCount { get; set; }
    public int RiskCount { get; set; }
    public List<ProjectMemberRowViewModel> Members { get; set; } = [];
}

public sealed class ProjectMemberRowViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
