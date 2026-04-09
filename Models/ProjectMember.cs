using System.ComponentModel.DataAnnotations;

namespace CodeGraphWeb.Models;

public class ProjectMember
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public int ProjectId { get; set; }

    [Required]
    [MaxLength(32)]
    public string Role { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public Project? Project { get; set; }
}
