using System.Security.Claims;
using CodeGraphWeb.Constants;
using CodeGraphWeb.Data;
using CodeGraphWeb.Models;
using CodeGraphWeb.Services;
using CodeGraphWeb.ViewModels.Project;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CodeGraphWeb.Controllers;

[Authorize(Roles = $"{Roles.CompanyAdmin},{Roles.TechLead},{Roles.User}")]
public class ProjectController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IProjectAuthorizationService _projectAuthorizationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IConfiguration _configuration;

    public ProjectController(
        ApplicationDbContext dbContext,
        IProjectAuthorizationService projectAuthorizationService,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment webHostEnvironment,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _projectAuthorizationService = projectAuthorizationService;
        _userManager = userManager;
        _webHostEnvironment = webHostEnvironment;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        var isCompanyAdmin = await _userManager.IsInRoleAsync(currentUser, Roles.CompanyAdmin);
        IQueryable<Project> query = _dbContext.Projects
            .AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Members);

        if (isCompanyAdmin)
        {
            if (!currentUser.CompanyId.HasValue)
            {
                return Forbid();
            }

            query = query.Where(x => x.CompanyId == currentUser.CompanyId.Value);
        }
        else
        {
            query = query.Where(x => x.Members.Any(m => m.UserId == currentUser.Id));
        }

        var projects = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ProjectListItemViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                CompanyName = x.Company != null ? (x.Company.Name ?? $"Sirket #{x.CompanyId}") : $"Sirket #{x.CompanyId}",
                MemberCount = x.Members.Count,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return View(new ProjectIndexViewModel
        {
            CanCreateProject = isCompanyAdmin,
            Projects = projects
        });
    }

    [HttpGet]
    [Authorize(Roles = Roles.CompanyAdmin)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = await BuildCreateModelAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.CompanyAdmin)]
    public async Task<IActionResult> Create(CreateProjectInputModel input, CancellationToken cancellationToken)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildCreateModelAsync(cancellationToken, input);
            return View(invalidModel);
        }

        var maxFileSizeBytes = GetMaxFileSizeBytes();
        var allowedExtensions = GetAllowedExtensions();

        if (input.File is null || input.File.Length == 0)
        {
            ModelState.AddModelError(nameof(input.File), "Proje dosyasi secmelisiniz.");
            var noFileModel = await BuildCreateModelAsync(cancellationToken, input);
            return View(noFileModel);
        }

        if (input.File.Length > maxFileSizeBytes)
        {
            var maxMb = maxFileSizeBytes / (1024 * 1024);
            ModelState.AddModelError(nameof(input.File), $"Dosya boyutu en fazla {maxMb} MB olabilir.");
            var tooLargeModel = await BuildCreateModelAsync(cancellationToken, input);
            return View(tooLargeModel);
        }

        var originalFileName = Path.GetFileName(input.File.FileName);
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            ModelState.AddModelError(nameof(input.File), "Sadece .zip, .tar veya .rar dosyalari kabul edilir.");
            var invalidExtensionModel = await BuildCreateModelAsync(cancellationToken, input);
            return View(invalidExtensionModel);
        }

        if (!currentUser.CompanyId.HasValue || input.CompanyId != currentUser.CompanyId.Value)
        {
            return Forbid();
        }

        var companyExists = await _dbContext.Companies.AnyAsync(x => x.Id == input.CompanyId, cancellationToken);
        if (!companyExists)
        {
            ModelState.AddModelError(nameof(input.CompanyId), "Sirket bulunamadi.");
            var notFoundModel = await BuildCreateModelAsync(cancellationToken, input);
            return View(notFoundModel);
        }

        var allowedUserIds = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.CompanyId == input.CompanyId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var selectedUserIds = input.AssignedUserIds
            .Where(x => allowedUserIds.Contains(x))
            .Distinct()
            .ToList();

        if (!selectedUserIds.Contains(currentUser.Id))
        {
            selectedUserIds.Add(currentUser.Id);
        }

        var project = new Project
        {
            Name = input.ProjectName.Trim(),
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            CompanyId = input.CompanyId
        };

        var uploadsRoot = Path.Combine(_webHostEnvironment.WebRootPath ?? "wwwroot", "uploads", "projects");
        Directory.CreateDirectory(uploadsRoot);

        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        var absoluteFilePath = Path.Combine(uploadsRoot, uniqueFileName);

        try
        {
            await using var stream = new FileStream(absoluteFilePath, FileMode.CreateNew, FileAccess.Write);
            await input.File.CopyToAsync(stream, cancellationToken);
        }
        catch
        {
            ModelState.AddModelError(nameof(input.File), "Dosya yukleme sirasinda hata olustu.");
            var uploadFailedModel = await BuildCreateModelAsync(cancellationToken, input);
            return View(uploadFailedModel);
        }

        project.FilePath = $"/uploads/projects/{uniqueFileName}";
        project.FileName = originalFileName;
        project.UploadDate = DateTime.UtcNow;

        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var userId in selectedUserIds)
        {
            _dbContext.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = project.Id,
                UserId = userId,
                Role = userId == currentUser.Id ? ProjectRoles.TechLead : ProjectRoles.User
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        TempData["ProjectSuccess"] = "Proje olusturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        var isCompanyAdmin = await _userManager.IsInRoleAsync(currentUser, Roles.CompanyAdmin);

        var project = await _dbContext.Projects
            .AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (project is null)
        {
            return NotFound();
        }

        var canAccess = isCompanyAdmin
            ? currentUser.CompanyId == project.CompanyId
            : project.Members.Any(x => x.UserId == currentUser.Id);

        if (!canAccess)
        {
            return Forbid();
        }

        var riskCount = await _dbContext.Risks.CountAsync(x => x.ProjectId == project.Id, cancellationToken);

        var model = new ProjectDetailViewModel
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CompanyName = project.Company?.Name ?? $"Sirket #{project.CompanyId}",
            CreatedAt = project.CreatedAt,
            FileName = project.FileName,
            UploadDate = project.UploadDate,
            MemberCount = project.Members.Count,
            RiskCount = riskCount,
            Members = project.Members
                .OrderBy(x => x.User!.FullName)
                .Select(x => new ProjectMemberRowViewModel
                {
                    UserId = x.UserId,
                    FullName = string.IsNullOrWhiteSpace(x.User!.FullName) ? "Isimsiz Kullanici" : x.User.FullName,
                    Email = x.User.Email ?? "-",
                    Role = x.Role
                })
                .ToList()
        };

        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = Roles.CompanyAdmin)]
    public async Task<IActionResult> CompanyUsers(int companyId, CancellationToken cancellationToken)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        if (!currentUser.CompanyId.HasValue || currentUser.CompanyId.Value != companyId)
        {
            return Forbid();
        }

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.FullName)
            .Select(x => new ProjectUserOptionViewModel
            {
                Id = x.Id,
                FullName = string.IsNullOrWhiteSpace(x.FullName) ? "Isimsiz Kullanici" : x.FullName,
                Email = x.Email ?? "-"
            })
            .ToListAsync(cancellationToken);

        return Json(users);
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

        if (user.CompanyId != project.CompanyId)
        {
            return Forbid();
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

    private async Task<ProjectCreatePageViewModel> BuildCreateModelAsync(CancellationToken cancellationToken, CreateProjectInputModel? input = null)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser is null || !currentUser.CompanyId.HasValue)
        {
            return new ProjectCreatePageViewModel();
        }

        var companyId = currentUser.CompanyId.Value;

        var companies = await _dbContext.Companies
            .AsNoTracking()
            .Where(x => x.Id == companyId)
            .OrderBy(x => x.Name)
            .Select(x => new ProjectCompanyOptionViewModel
            {
                Id = x.Id,
                Name = x.Name ?? $"Sirket #{x.Id}"
            })
            .ToListAsync(cancellationToken);

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.FullName)
            .Select(x => new ProjectUserOptionViewModel
            {
                Id = x.Id,
                FullName = string.IsNullOrWhiteSpace(x.FullName) ? "Isimsiz Kullanici" : x.FullName,
                Email = x.Email ?? "-"
            })
            .ToListAsync(cancellationToken);

        var model = new ProjectCreatePageViewModel
        {
            Input = input ?? new CreateProjectInputModel { CompanyId = companyId },
            Companies = companies,
            Users = users
        };

        if (model.Input.CompanyId == 0 && companies.Count > 0)
        {
            model.Input.CompanyId = companies[0].Id;
        }

        return model;
    }

    private long GetMaxFileSizeBytes()
    {
        var configuredMb = _configuration.GetValue<long?>("ProjectUpload:MaxFileSizeMb");
        var maxMb = configuredMb.GetValueOrDefault(50);
        return maxMb * 1024 * 1024;
    }

    private HashSet<string> GetAllowedExtensions()
    {
        var configured = _configuration
            .GetSection("ProjectUpload:AllowedExtensions")
            .Get<string[]>();

        var defaults = new[] { ".zip", ".tar", ".rar" };
        var source = configured is { Length: > 0 } ? configured : defaults;

        return source
            .Select(x => x.StartsWith('.') ? x.ToLowerInvariant() : $".{x.ToLowerInvariant()}")
            .ToHashSet();
    }
}
