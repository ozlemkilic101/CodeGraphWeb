using System.ComponentModel.DataAnnotations;
using CodeGraphWeb.Constants;

namespace CodeGraphWeb.ViewModels.Admin;

public sealed class AdminUsersViewModel
{
    public bool IsSystemAdmin { get; set; }
    public List<string> AvailableRoles { get; set; } = [];
    public List<AdminCompanyOptionViewModel> AvailableCompanies { get; set; } = [];
    public List<AdminCompanyGroupViewModel> CompanyGroups { get; set; } = [];
}

public sealed class AdminCompanyGroupViewModel
{
    public int? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public List<AdminUserRowViewModel> Users { get; set; } = [];
}

public sealed class AdminUserRowViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int? CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CurrentRole { get; set; } = Roles.User;
}

public sealed class AssignRoleInputModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = Roles.User;
}

public sealed class AssignCompanyInputModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CompanyId { get; set; }
}

public sealed class AdminCompanyOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class UpdateUserInputModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MinLength(6)]
    public string? NewPassword { get; set; }
}
