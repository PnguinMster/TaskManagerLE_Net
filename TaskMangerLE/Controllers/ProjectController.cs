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
        var (projectItem, error) = await LoadAndAuthProjectAsync(() => services.FindProjectAsync(projectId));
        if (error != null)
            return error;

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

        var (projectItem, error) = await LoadAndAuthProjectAsync(() => services.FindProjectAsync(projectItemModel.Id));
        if (error != null)
            return error;

        await services.UpdateProjectAsync(projectItem, projectItemModel);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int? projectId)
    {
        var (projectItem, error) = await LoadAndAuthProjectAsync(() => services.GetByIdWithTasksAsync(projectId));
        if (error != null)
            return error;

        string? deletedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await services.DeleteProject(projectItem, deletedByUserId);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Restore()
    {
        bool isAdmin = User.IsInRole(Roles.User);
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var deletedProjects = isAdmin ? await services.GetAllDeletedAsync() : await services.GetAllDeletedForUserAsync(userId);

        return View(deletedProjects);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int? projectId)
    {
        var (projectItem, error) = await LoadAndAuthProjectAsync(() => services.GetDeletedProjectWithDeletedTasks(projectId));
        if (error != null)
            return error;

        await services.RestoreProject(projectItem);
        return RedirectToAction(nameof(Restore));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PurgeProject(int? projectId)
    {
        var (projectItem, error) = await LoadAndAuthProjectAsync(() => services.GetDeletedProjectAllTasks(projectId));
        if (error != null)
            return error;

        await services.PurgeProject(projectItem);
        return RedirectToAction(nameof(Restore));
    }

    private async Task<(ProjectItem projectItem, IActionResult? error)> LoadAndAuthProjectAsync(Func<Task<ProjectItem?>> getProject)
    {
        ProjectItem? projectItem = await getProject();
        if (projectItem == null)
            return (null, NotFound())!;

        var authResult = await authorizationService.AuthorizeAsync(User, projectItem, Policies.ProjectOwner);
        if (!authResult.Succeeded)
            return (null, Forbid())!;

        return (projectItem, null);
    }
}