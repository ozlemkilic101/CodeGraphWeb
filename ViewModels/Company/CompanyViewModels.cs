using System.ComponentModel.DataAnnotations;

namespace CodeGraphWeb.ViewModels.Company;

public sealed class CompanyIndexViewModel
{
    public bool IsSystemAdmin { get; set; }
    public List<CompanyCardViewModel> Companies { get; set; } = [];
}

public sealed class CompanyCardViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public int ProjectCount { get; set; }
}

public sealed class CreateCompanyInputModel
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
}
