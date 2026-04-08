namespace CodeGraphWeb.Models;

public class DependencyRelation
{
    public int Id { get; set; }

    public int SourceId { get; set; }

    public int TargetId { get; set; }


    //A kütüphanesi b kütüphanesine bağımlıysa, Source:A Target:B olur. Yani Source bağımlı olan, Target ise bağımlılık olan kütüphaneyi temsil eder.

}