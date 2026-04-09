using System.Security.Claims;
using CodeGraphWeb.Constants;
using CodeGraphWeb.Data;
using CodeGraphWeb.Models;
using CodeGraphWeb.Services;
using CodeGraphWeb.ViewModels.Project;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CodeGraphWeb.Controllers;

[Authorize]
public class ProjectController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IProjectAuthorizationService _projectAuthorizationService;

    public ProjectController(
        ApplicationDbContext dbContext,
        IProjectAuthorizationService projectAuthorizationService)
    {
        _dbContext = dbContext;
        _projectAuthorizationService = projectAuthorizationService;
    }

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMember(AddProjectMemberInputModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Geçersiz istek.");
        }

        if (!ProjectRoles.All.Contains(model.Role))
        {
            return BadRequest("Geçersiz proje rolü.");
        }

        var actorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return Challenge();
        }

        var canManageProject = await _projectAuthorizationService.IsTechLeadAsync(actorUserId, model.ProjectId);
        if (!canManageProject)
        {
            return Forbid();
        }

        var projectExists = await _dbContext.Projects.AnyAsync(x => x.Id == model.ProjectId);
        if (!projectExists)
        {
            return NotFound("Proje bulunamadý.");
        }

        var userExists = await _dbContext.Users.AnyAsync(x => x.Id == model.UserId);
        if (!userExists)
        {
            return NotFound("Kullanýcý bulunamadý.");
        }

        var alreadyExists = await _dbContext.ProjectMembers.AnyAsync(x => x.UserId == model.UserId && x.ProjectId == model.ProjectId);
        if (alreadyExists)
        {
            return Conflict("Kullanýcý bu projeye zaten eklenmiþ.");
        }

        var projectMember = new ProjectMember
        {
            UserId = model.UserId,
            ProjectId = model.ProjectId,
            Role = model.Role
        };

        _dbContext.ProjectMembers.Add(projectMember);
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "Kullanýcý projeye eklendi.",
            projectId = model.ProjectId,
            userId = model.UserId,
            role = model.Role
        });
    }
}
