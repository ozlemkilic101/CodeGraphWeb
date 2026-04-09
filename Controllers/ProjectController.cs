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

[Authorize(Roles = $"{Roles.CompanyAdmin},{Roles.TechLead},{Roles.User}")]
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

    [Authorize(Roles = $"{Roles.CompanyAdmin},{Roles.TechLead}")]
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
    [Authorize(Roles = $"{Roles.CompanyAdmin},{Roles.TechLead}")]
    public async Task<IActionResult> AddMember(AddProjectMemberInputModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Gecersiz istek.");
        }

        if (!ProjectRoles.All.Contains(model.Role))
        {
            return BadRequest("Gecersiz proje rolu.");
        }

        var actorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return Challenge();
        }

        var actor = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == actorUserId);
        if (actor is null)
        {
            return Challenge();
        }

        var isCompanyAdmin = User.IsInRole(Roles.CompanyAdmin);
        var canManageProject = isCompanyAdmin || await _projectAuthorizationService.IsTechLeadAsync(actorUserId, model.ProjectId);
        if (!canManageProject)
        {
            return Forbid();
        }

        var project = await _dbContext.Projects.FirstOrDefaultAsync(x => x.Id == model.ProjectId);
        if (project is null)
        {
            return NotFound("Proje bulunamadi.");
        }

        if (isCompanyAdmin && actor.CompanyId != project.CompanyId)
        {
            return Forbid();
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == model.UserId);
        if (user is null)
        {
            return NotFound("Kullanici bulunamadi.");
        }

        if (isCompanyAdmin && user.CompanyId != actor.CompanyId)
        {
            return Forbid();
        }

        var alreadyExists = await _dbContext.ProjectMembers.AnyAsync(x => x.UserId == model.UserId && x.ProjectId == model.ProjectId);
        if (alreadyExists)
        {
            return Conflict("Kullanici bu projeye zaten eklenmis.");
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
            message = "Kullanici projeye eklendi.",
            projectId = model.ProjectId,
            userId = model.UserId,
            role = model.Role
        });
    }
}
