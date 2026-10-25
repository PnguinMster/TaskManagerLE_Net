using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TaskMangerLE.Models;
using TaskMangerLE.Services;
using TaskMangerLE.Authorization;

namespace TaskMangerLE.Controllers;

[Authorize]
public class ProjectController(ProjectService services, UserManager<ApplicationUser> userManager, IAuthorizationService authorizationService)
    : ItemControllerBase(authorizationService)
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        string? userId = userManager.GetUserId(User);
        bool isAdmin = User.IsInRole(Roles.Admin);
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
        ProjectItem? projectItem = await LoadAuthorizedAsync(services.FindProjectAsync(projectId));
        if (projectItem == null)
            return NotFound();

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

        ProjectItem? projectItem = await LoadAuthorizedAsync(services.FindProjectAsync(projectItemModel.Id));
        if (projectItem == null)
            return NotFound();

        await services.UpdateProjectAsync(projectItem, projectItemModel);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int? projectId)
    {
        ProjectItem? projectItem = await LoadAuthorizedAsync(services.GetByIdWithTasksAsync(projectId));
        if (projectItem == null)
            return NotFound();

        string? deletedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await services.DeleteProject(projectItem, deletedByUserId);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Restore()
    {
        bool isAdmin = User.IsInRole(Roles.Admin);
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var deletedProjects = isAdmin ? await services.GetAllDeletedAsync() : await services.GetAllDeletedForUserAsync(userId);

        return View(deletedProjects);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int? projectId)
    {
        ProjectItem? projectItem = await LoadAuthorizedAsync(services.GetDeletedProjectWithDeletedTasks(projectId));
        if (projectItem == null)
            return NotFound();

        await services.RestoreProject(projectItem);
        return RedirectToAction(nameof(Restore));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PurgeProject(int? projectId)
    {
        ProjectItem? projectItem = await LoadAuthorizedAsync(services.GetDeletedProjectAllTasks(projectId));
        if (projectItem == null)
            return NotFound();

        await services.PurgeProject(projectItem);
        return RedirectToAction(nameof(Restore));
    }
}