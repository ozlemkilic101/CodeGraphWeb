namespace CodeGraphWeb.Models;
public class ProjectMember
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public int UserId { get; set; }

    public string? Role{get;set;} // Proje içindeki rolü (örneğin, "Developer", "Manager", "Tester" vb.)
}