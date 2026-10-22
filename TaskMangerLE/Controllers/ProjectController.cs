using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TaskMangerLE.Models;
using TaskMangerLE.Services;
using TaskMangerLE.Authorization;

namespace TaskMangerLE.Controllers;

[Authorize]
public class ProjectController(ProjectService services, UserManager<ApplicationUser> userManager, IAuthorizationService authorizationService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        string? userId = userManager.GetUserId(User);
        bool isAdmin = User.IsInRole(Roles.User);
        return isAdmin ? View(await services.GetAllAsync()) : View(await services.GetUserAllAsync(userId));
    }

    [HttpGet]
    public IActionResult Create() => View(new ProjectItemCreateViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectItemCreateViewModel projectItemModel)
    {
        if (!ModelState.IsValid)
            return View(projectItemModel);

        string? userId = userManager.GetUserId(User);

        await services.AddProjectAsync(projectItemModel, userId);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Update(int? projectId)
    {
        ProjectItem? projectItem = await services.FindProjectAsync(projectId);

        if (projectItem == null)
            return NotFound();

        var authorizationResult = await authorizationService.AuthorizeAsync(User, projectItem, Policies.ProjectOwner);
        if (!authorizationResult.Succeeded)
            return Forbid();

        ProjectItemUpdateViewModel projectItemModel = new ProjectItemUpdateViewModel
        {
            Id = projectItem.Id,
            Name = projectItem.Name,
            Description = projectItem.Description,
            Status = projectItem.Status,
        };

        ViewData["ProjectName"] = projectItem.Name;
        return View(projectItemModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProjectItemUpdateViewModel projectItemModel)
    {
        if (!ModelState.IsValid)
            return View(projectItemModel);

        bool isUpdated = await services.UpdateProjectAsync(projectItemModel);
        if (!isUpdated)
            return View(projectItemModel);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int? projectId)
    {
        ProjectItem? projectItem = await services.GetByIdWithTasksAsync(projectId);
        if (projectItem == null)
            return NotFound();

        var authResult = await authorizationService.AuthorizeAsync(User, projectItem, Policies.ProjectOwner);
        if (!authResult.Succeeded)
            return Forbid();

        string? deletedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await services.DeleteProject(projectItem, deletedByUserId);

        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() =>
        RedirectToAction(nameof(Index), "Home");

    [HttpGet]
    public async Task<IActionResult> Restore()
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var deletedProjects = await services.GetDeleted(userId);

        return View(nameof(Restore), deletedProjects);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int? projectId)
    {
        ProjectItem? projectItem = await services.GetDeletedProjectWithDeletedTasks(projectId);
        if (projectItem == null)
            return NotFound();

        var authResult = await authorizationService.AuthorizeAsync(User, projectItem, Policies.ProjectOwner);
        if (!authResult.Succeeded)
            return Forbid();

        await services.RestoreProject(projectItem);
        return RedirectToAction(nameof(Restore), "Project");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PurgeProject(int? projectId)
    {
        ProjectItem? projectItem = await services.GetDeletedProjectAllTasks(projectId);

        if (projectItem == null)
            return NotFound();

        var authResult = await authorizationService.AuthorizeAsync(User, projectItem, Policies.ProjectOwner);
        if (!authResult.Succeeded)
            return Forbid();

        await services.PurgeProject(projectItem);
        return RedirectToAction(nameof(Restore), "Project");
    }
}