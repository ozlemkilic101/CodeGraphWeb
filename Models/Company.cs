namespace CodeGraphWeb.Models;

public class Company
{
    public int Id{get;set;}

    public string? Name{get;set;}

    public int SubscriptionId{get;set;} //üyelik tipini gösterecek. kişinin proje oluşturup oluşturamayacağı bilgisi buradan alınacak.

    public DateTime CreatedAt{get;set;}

    public Company(){
        CreatedAt = DateTime.Now;
    }
}