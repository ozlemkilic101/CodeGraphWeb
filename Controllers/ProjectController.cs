using Microsoft.AspNetCore.Mvc;

namespace CodeGraphWeb.Controllers;

public class ProjectController:Controller
{
    public IActionResult Index()
    {
        return View();
    }

        public IActionResult Create()
    {
        return View();
    }

    public IActionResult Detail()
    {
        return View();
    }


}