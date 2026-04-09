namespace CodeGraphWeb.Models;

public class Company
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public int SubscriptionId { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();

    public Company()
    {
        CreatedAt = DateTime.Now;
    }
}
