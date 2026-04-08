using Microsoft.AspNetCore.Mvc;

namespace CodeGraphWeb.Controllers;

public class DashboardController:Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
