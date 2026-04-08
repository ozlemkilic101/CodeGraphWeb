namespace CodeGraphWeb.Models;

public class Project
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public int CompanyId { get; set; } // Projenin ait olduğu şirketin ID'si

    public int CreatedByUserId { get; set; } // Projeyi oluşturan kullanıcının ID'si

    public DateTime CreatedAt{get;set;}

    public Project(){
        CreatedAt = DateTime.Now;

    }

}