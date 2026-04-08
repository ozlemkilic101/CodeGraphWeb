namespace CodeGraphWeb.Models;
public class Risk
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string? DependencyId { get; set; }

    public string? Severity { get; set; } // Riskin ciddiyetini belirtir (örneğin, "Low", "Medium", "High")

    public string? Description { get; set; } 

}