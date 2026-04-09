using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CodeGraphWeb.Controllers;

[Authorize]
public class CompanyController:Controller
{
    public IActionResult Index()
    {
        return View();
    }

        public IActionResult Create()
    {
        return View();
    }
}
