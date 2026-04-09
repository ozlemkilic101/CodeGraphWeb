using CodeGraphWeb.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeGraphWeb.Controllers;

[Authorize(Roles = $"{Roles.SystemAdmin},{Roles.CompanyAdmin}")]
public class CompanyController : Controller
{
    public IActionResult Index()
    {
        if (!User.IsInRole(Roles.SystemAdmin) && !User.IsInRole(Roles.CompanyAdmin))
        {
            return Forbid();
        }

        return View();
    }

    [Authorize(Roles = Roles.SystemAdmin)]
    public IActionResult Create()
    {
        return View();
    }
}
