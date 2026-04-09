using CodeGraphWeb.Constants;
using CodeGraphWeb.Data;
using CodeGraphWeb.Models;
using CodeGraphWeb.ViewModels.Company;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CodeGraphWeb.Controllers;

[Authorize(Roles = $"{Roles.SystemAdmin},{Roles.CompanyAdmin}")]
public class CompanyController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public CompanyController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        var isSystemAdmin = await _userManager.IsInRoleAsync(currentUser, Roles.SystemAdmin);

        var query = _dbContext.Companies
            .AsNoTracking()
            .Include(x => x.Users)
            .Include(x => x.Projects)
            .AsQueryable();

        if (!isSystemAdmin)
        {
            if (!currentUser.CompanyId.HasValue)
            {
                return Forbid();
            }

            query = query.Where(x => x.Id == currentUser.CompanyId.Value);
        }

        var companies = await query
            .OrderBy(x => x.Name)
            .Select(x => new CompanyCardViewModel
            {
                Id = x.Id,
                Name = x.Name ?? $"Sirket #{x.Id}",
                UserCount = x.Users.Count,
                ProjectCount = x.Projects.Count
            })
            .ToListAsync(cancellationToken);

        return View(new CompanyIndexViewModel
        {
            IsSystemAdmin = isSystemAdmin,
            Companies = companies
        });
    }

    [HttpGet]
    [Authorize(Roles = Roles.SystemAdmin)]
    public IActionResult Create()
    {
        return View(new CreateCompanyInputModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.SystemAdmin)]
    public async Task<IActionResult> Create(CreateCompanyInputModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedName = NormalizeCompanyName(model.Name);
        var normalizedKey = NormalizeCompanyKey(normalizedName);

        var existingKeys = await _dbContext.Companies
            .AsNoTracking()
            .Where(x => x.Name != null)
            .Select(x => x.Name!)
            .ToListAsync(cancellationToken);

        var exists = existingKeys.Any(x => NormalizeCompanyKey(x) == normalizedKey);

        if (exists)
        {
            ModelState.AddModelError(nameof(model.Name), "Bu isimde bir sirket zaten var.");
            return View(model);
        }

        var company = new Company
        {
            Name = normalizedName,
            SubscriptionId = 1
        };

        _dbContext.Companies.Add(company);
        await _dbContext.SaveChangesAsync(cancellationToken);

        TempData["CompanySuccess"] = "Sirket olusturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.SystemAdmin)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var company = await _dbContext.Companies
            .Include(x => x.Users)
            .Include(x => x.Projects)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (company is null)
        {
            TempData["CompanyError"] = "Sirket bulunamadi.";
            return RedirectToAction(nameof(Index));
        }

        var hasSystemAdminUser = await _dbContext.Users
            .Where(x => x.CompanyId == company.Id)
            .Join(_dbContext.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => ur.RoleId)
            .Join(_dbContext.Roles, roleId => roleId, role => role.Id, (roleId, role) => role.Name)
            .AnyAsync(x => x == Roles.SystemAdmin, cancellationToken);

        if (hasSystemAdminUser)
        {
            TempData["CompanyError"] = "SystemAdmin kullanicisi olan sirket silinemez.";
            return RedirectToAction(nameof(Index));
        }

        _dbContext.Companies.Remove(company);
        await _dbContext.SaveChangesAsync(cancellationToken);

        TempData["CompanySuccess"] = "Sirket silindi.";
        return RedirectToAction(nameof(Index));
    }

    private static string NormalizeCompanyName(string name)
    {
        return string.Join(" ", name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string NormalizeCompanyKey(string name)
    {
        return NormalizeCompanyName(name).ToLowerInvariant();
    }
}
