using System.Security.Claims;
using CodeGraphWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeGraphWeb.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var model = await _dashboardService.BuildAsync(userId, cancellationToken);
        if (model is null)
        {
            return Challenge();
        }

        ViewData["Title"] = "Dashboard";
        return View(model);
    }
}
