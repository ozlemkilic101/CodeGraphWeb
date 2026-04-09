using CodeGraphWeb.Constants;
using CodeGraphWeb.Data;
using CodeGraphWeb.Models;
using CodeGraphWeb.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CodeGraphWeb.Controllers;

[Authorize(Policy = "TeamManagement")]
[Route("Admin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;

    public AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    [HttpGet("Users")]
    public async Task<IActionResult> Users(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Admin Paneli";

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        var isSystemAdmin = await _userManager.IsInRoleAsync(currentUser, Roles.SystemAdmin);
        var query = _userManager.Users.AsNoTracking();

        if (!isSystemAdmin)
        {
            query = query.Where(x => x.CompanyId == currentUser.CompanyId);
        }

        var users = await query.OrderBy(x => x.CompanyId).ThenBy(x => x.Email).ToListAsync(cancellationToken);

        var companyIds = users.Where(x => x.CompanyId.HasValue).Select(x => x.CompanyId!.Value).Distinct().ToList();
        var companyMap = await _dbContext.Companies
            .Where(x => companyIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name ?? $"Şirket #{x.Id}", cancellationToken);

        var userIds = users.Select(x => x.Id).ToList();
        var roleRows = await (from ur in _dbContext.UserRoles
                              join r in _dbContext.Roles on ur.RoleId equals r.Id
                              where userIds.Contains(ur.UserId)
                              select new { ur.UserId, Role = r.Name ?? Roles.User }).ToListAsync(cancellationToken);

        static string ResolveCurrentRole(string userId, IEnumerable<dynamic> rows)
        {
            var order = new[] { Roles.SystemAdmin, Roles.CompanyAdmin, Roles.TechLead, Roles.User };
            var set = rows.Where(x => x.UserId == userId).Select(x => (string)x.Role).ToHashSet();
            return order.FirstOrDefault(set.Contains) ?? Roles.User;
        }

        var items = users.Select(x => new AdminUserRowViewModel
        {
            UserId = x.Id,
            FullName = string.IsNullOrWhiteSpace(x.FullName) ? "-" : x.FullName,
            Email = x.Email ?? "-",
            CompanyId = x.CompanyId,
            CompanyName = x.CompanyId.HasValue && companyMap.TryGetValue(x.CompanyId.Value, out var companyName)
                ? companyName
                : "Atanmamış Şirket",
            CurrentRole = ResolveCurrentRole(x.Id, roleRows)
        }).ToList();

        var groups = items
            .GroupBy(x => new { x.CompanyId, x.CompanyName })
            .OrderBy(x => x.Key.CompanyName)
            .Select(x => new AdminCompanyGroupViewModel
            {
                CompanyId = x.Key.CompanyId,
                CompanyName = x.Key.CompanyName,
                Users = x.OrderBy(u => u.FullName).ToList()
            }).ToList();

        var model = new AdminUsersViewModel
        {
            IsSystemAdmin = isSystemAdmin,
            AvailableRoles = [Roles.CompanyAdmin, Roles.TechLead, Roles.User],
            CompanyGroups = groups
        };

        return View(model);
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

        var allowedRoles = new[] { Roles.CompanyAdmin, Roles.TechLead, Roles.User };
        if (!allowedRoles.Contains(model.Role))
        {
            TempData["RoleError"] = "Geçersiz rol seçimi.";
            return RedirectToAction(nameof(Users));
        }

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user is null)
        {
            TempData["RoleError"] = "Kullanıcı bulunamadı.";
            return RedirectToAction(nameof(Users));
        }

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        var isSystemAdmin = await _userManager.IsInRoleAsync(currentUser, Roles.SystemAdmin);
        if (!isSystemAdmin && user.CompanyId != currentUser.CompanyId)
        {
            TempData["RoleError"] = "Sadece kendi şirketinizdeki kullanıcıları yönetebilirsiniz.";
            return RedirectToAction(nameof(Users));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Contains(Roles.SystemAdmin))
        {
            TempData["RoleError"] = "SystemAdmin rolü panelden değiştirilemez.";
            return RedirectToAction(nameof(Users));
        }

        if (currentRoles.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
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

        TempData["RoleSuccess"] = "Kullanıcı rolü güncellendi.";
        return RedirectToAction(nameof(Users));
    }
}

