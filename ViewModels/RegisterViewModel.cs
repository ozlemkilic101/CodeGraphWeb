using System.ComponentModel.DataAnnotations;

namespace CodeGraphWeb.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Ad Soyad zorunludur.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Þifre zorunludur.")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Þifre en az 6 karakter olmalýdýr.")]
    [Display(Name = "Þifre")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Þifre tekrarý zorunludur.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Þifreler eþleþmiyor.")]
    [Display(Name = "Þifre Tekrar")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
