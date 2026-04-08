namespace CodeGraphWeb.Models;

public class Dependency
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string? Name { get; set; }

    public string? Version { get; set; }

    public string? Type { get; set; } // kütüphane mi servis mi vs...

}