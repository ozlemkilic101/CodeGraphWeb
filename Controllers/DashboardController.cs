using System.Security.Claims;
using CodeGraphWeb.Constants;
using CodeGraphWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeGraphWeb.Controllers;

[Authorize(Policy = "DashboardAccess")]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public IActionResult Index(CancellationToken cancellationToken)
    {
        if (User.IsInRole(Roles.SystemAdmin))
        {
            return RedirectToAction(nameof(SystemAdmin));
        }

        if (User.IsInRole(Roles.CompanyAdmin))
        {
            return RedirectToAction(nameof(CompanyAdmin));
        }

        if (User.IsInRole(Roles.TechLead))
        {
            return RedirectToAction(nameof(TechLead));
        }

        if (User.IsInRole(Roles.User))
        {
            return RedirectToAction(nameof(UserDashboard));
        }

        return Forbid();
    }

    [HttpGet("Dashboard/SystemAdmin")]
    [Authorize(Roles = Roles.SystemAdmin)]
    public async Task<IActionResult> SystemAdmin(CancellationToken cancellationToken)
    {
        return await BuildDashboardResultAsync(Roles.SystemAdmin, cancellationToken);
    }

    [HttpGet("Dashboard/CompanyAdmin")]
    [Authorize(Roles = Roles.CompanyAdmin)]
    public async Task<IActionResult> CompanyAdmin(CancellationToken cancellationToken)
    {
        return await BuildDashboardResultAsync(Roles.CompanyAdmin, cancellationToken);
    }

    [HttpGet("Dashboard/TechLead")]
    [Authorize(Roles = Roles.TechLead)]
    public async Task<IActionResult> TechLead(CancellationToken cancellationToken)
    {
        return await BuildDashboardResultAsync(Roles.TechLead, cancellationToken);
    }

    [HttpGet("Dashboard/User")]
    [Authorize(Roles = Roles.User)]
    public async Task<IActionResult> UserDashboard(CancellationToken cancellationToken)
    {
        return await BuildDashboardResultAsync(Roles.User, cancellationToken);
    }

    private async Task<IActionResult> BuildDashboardResultAsync(string role, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var model = await _dashboardService.BuildForRoleAsync(userId, role, cancellationToken);
        if (model is null)
        {
            return Forbid();
        }

        ViewData["Title"] = "Dashboard";
        return View("Index", model);
    }
}
