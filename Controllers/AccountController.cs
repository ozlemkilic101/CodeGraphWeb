using CodeGraphWeb.Constants;
using CodeGraphWeb.Models;
using CodeGraphWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CodeGraphWeb.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
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

        ModelState.AddModelError(string.Empty, "Giriþ baþarýsýz. E-posta veya þifre hatalý.");
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        ViewData["Title"] = "Register";
        return View(new RegisterViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        ViewData["Title"] = "Register";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            FullName = model.FullName,
            UserName = model.Email,
            Email = model.Email
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

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
        ViewData["Title"] = "Eriþim Reddedildi";
        return View();
    }
}
