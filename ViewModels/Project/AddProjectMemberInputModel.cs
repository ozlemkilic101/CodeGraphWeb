using System.ComponentModel.DataAnnotations;
using CodeGraphWeb.Constants;

namespace CodeGraphWeb.ViewModels.Project;

public sealed class AddProjectMemberInputModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ProjectId { get; set; }

    [Required]
    public string Role { get; set; } = ProjectRoles.User;
}
