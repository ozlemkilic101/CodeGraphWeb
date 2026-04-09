using CodeGraphWeb.Constants;
using CodeGraphWeb.Data;
using CodeGraphWeb.Models;
using CodeGraphWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CodeGraphWeb.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _dbContext;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["Title"] = "Login";
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["Title"] = "Login";
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        ModelState.AddModelError(string.Empty, "Giris basarisiz. E-posta veya sifre hatali.");
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Register(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Register";
        var model = await BuildRegisterViewModelAsync(new RegisterViewModel(), cancellationToken);
        return View(model);
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        ViewData["Title"] = "Register";
        model = await BuildRegisterViewModelAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var company = await _dbContext.Companies.FirstOrDefaultAsync(x => x.Id == model.CompanyId);
        if (company is null)
        {
            ModelState.AddModelError(nameof(model.CompanyId), "Secilen sirket bulunamadi.");
            model = await BuildRegisterViewModelAsync(model);
            return View(model);
        }

        var user = new ApplicationUser
        {
            FullName = model.FullName,
            UserName = model.Email,
            Email = model.Email,
            CompanyId = company.Id
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model = await BuildRegisterViewModelAsync(model);
            return View(model);
        }

        var addRoleResult = await _userManager.AddToRoleAsync(user, Roles.User);
        if (!addRoleResult.Succeeded)
        {
            foreach (var error in addRoleResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await _userManager.DeleteAsync(user);
            model = await BuildRegisterViewModelAsync(model);
            return View(model);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);

        return RedirectToAction("Index", "Dashboard");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        ViewData["Title"] = "Erisim Reddedildi";
        return View();
    }

    private async Task<RegisterViewModel> BuildRegisterViewModelAsync(RegisterViewModel model, CancellationToken cancellationToken = default)
    {
        model.AvailableCompanies = await _dbContext.Companies
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new RegisterCompanyOptionViewModel
            {
                Id = x.Id,
                Name = x.Name ?? $"Sirket #{x.Id}"
            })
            .ToListAsync(cancellationToken);

        return model;
    }
}
