namespace CodeGraphWeb.Models;

public class Subscription
{
    public int SubscriptionId { get; set; }
    public int PlanId { get; set; } // PlanId, abonelik planını temsil eder (örneğin, Basic, Pro )

    public DateTime ExpireDate { get; set; } // Aboneliğin sona ereceği tarih
    

    public bool IsActive => DateTime.Now < ExpireDate; // Aboneliğin aktif olup olmadığını kontrol eder

    public Subscription()
    {
        ExpireDate = DateTime.Now.AddDays(30);
    }

}