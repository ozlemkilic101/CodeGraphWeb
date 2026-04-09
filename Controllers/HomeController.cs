using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CodeGraphWeb.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Home";
        return View();
    }

    public IActionResult Solutions()
    {
        ViewData["Title"] = "Çözümler";
        return View("Pricing");
    }

    public IActionResult Pricing()
    {
        return RedirectToAction(nameof(Solutions));
    }
}
