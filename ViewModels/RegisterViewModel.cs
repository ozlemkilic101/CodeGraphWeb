using System.ComponentModel.DataAnnotations;

namespace CodeGraphWeb.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Ad Soyad zorunludur.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Gecerli bir e-posta giriniz.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sirket secimi zorunludur.")]
    [Display(Name = "Sirket")]
    [Range(1, int.MaxValue, ErrorMessage = "Gecerli bir sirket seciniz.")]
    public int CompanyId { get; set; }

    public List<RegisterCompanyOptionViewModel> AvailableCompanies { get; set; } = [];

    [Required(ErrorMessage = "Sifre zorunludur.")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Sifre en az 6 karakter olmalidir.")]
    [Display(Name = "Sifre")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sifre tekrari zorunludur.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Sifreler eslesmiyor.")]
    [Display(Name = "Sifre Tekrar")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class RegisterCompanyOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
