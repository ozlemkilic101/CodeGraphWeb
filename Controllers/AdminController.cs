using CodeGraphWeb.Constants;
using CodeGraphWeb.Models;
using CodeGraphWeb.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CodeGraphWeb.Controllers;

[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
[Route("Admin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("Users")]
    public async Task<IActionResult> Users()
    {
        ViewData["Title"] = "Admin Paneli";

        var users = _userManager.Users.OrderBy(x => x.Email).ToList();
        var rows = new List<AdminUserRowViewModel>(users.Count);

        foreach (var user in users)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            rows.Add(new AdminUserRowViewModel
            {
                UserId = user.Id,
                FullName = string.IsNullOrWhiteSpace(user.FullName) ? "-" : user.FullName,
                Email = user.Email ?? "-",
                CurrentRole = userRoles.Contains(Roles.SuperAdmin) ? Roles.SuperAdmin : (userRoles.Contains(Roles.Admin) ? Roles.Admin : Roles.User)
            });
        }

        return View(new AdminUsersViewModel { Users = rows });
    }

    [HttpPost("AssignRole")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(AssignRoleInputModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["RoleError"] = "Geçersiz istek.";
            return RedirectToAction(nameof(Users));
        }

        if (model.Role != Roles.Admin && model.Role != Roles.User)
        {
            TempData["RoleError"] = "Geçersiz rol seçimi.";
            return RedirectToAction(nameof(Users));
        }

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user is null)
        {
            TempData["RoleError"] = "Kullanýcý bulunamadý.";
            return RedirectToAction(nameof(Users));
        }

        var existingRoles = await _userManager.GetRolesAsync(user);
        if (existingRoles.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, existingRoles);
            if (!removeResult.Succeeded)
            {
                TempData["RoleError"] = string.Join(" ", removeResult.Errors.Select(x => x.Description));
                return RedirectToAction(nameof(Users));
            }
        }

        var addResult = await _userManager.AddToRoleAsync(user, model.Role);
        if (!addResult.Succeeded)
        {
            TempData["RoleError"] = string.Join(" ", addResult.Errors.Select(x => x.Description));
            return RedirectToAction(nameof(Users));
        }

        TempData["RoleSuccess"] = "Kullanýcý rolü güncellendi.";
        return RedirectToAction(nameof(Users));
    }
}


