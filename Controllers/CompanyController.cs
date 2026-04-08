using Microsoft.AspNetCore.Mvc;

namespace CodeGraphWeb.Controllers;

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