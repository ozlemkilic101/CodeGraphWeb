namespace CodeGraphWeb.Models;

public class AnalysisResult
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string? Summary { get; set; }

    public DateTime GeneratedAt { get; set; } // Analizin yapıldığı tarih

    public AnalysisResult()
    {
        GeneratedAt = DateTime.Now; // Analiz sonucu oluşturulduğunda tarih otomatik olarak atanır
    }
}