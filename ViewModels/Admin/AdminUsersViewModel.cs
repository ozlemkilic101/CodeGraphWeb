using System.ComponentModel.DataAnnotations;
using CodeGraphWeb.Constants;

namespace CodeGraphWeb.ViewModels.Admin;

public sealed class AdminUsersViewModel
{
    public List<AdminUserRowViewModel> Users { get; set; } = [];
}

public sealed class AdminUserRowViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CurrentRole { get; set; } = Roles.User;
}

public sealed class AssignRoleInputModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = Roles.User;
}
