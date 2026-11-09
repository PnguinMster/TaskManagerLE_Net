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
        if (User.IsInRole(Roles.Admin))
            return View(await services.GetProjectCardsAsync());

        string? userId = userManager.GetUserId(User);
        if (userId is null)
            return Challenge();

        return View(await services.GetProjectCardsAsync(userId));
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int? projectId)
    {
        if (projectId is not { } id)
            return NotFound();

        ProjectItem? projectItem = await LoadAuthorizedAsync(services.GetProjectAsync(id));
        if (projectItem is null)
            return NotFound();

        ProjectDetailViewModel? project = await services.GetProjectDetailAsync(id);
        if (project is null)
            return NotFound();

        string? userId = userManager.GetUserId(User);
        if (userId is null)
            return Challenge();

        ViewData["AvailableProjects"] = User.IsInRole(Roles.Admin)
            ? await services.GetProjectCardsAsync()
            : await services.GetProjectCardsAsync(userId);

        return View(project);
    }

    [HttpGet]
    public IActionResult Create() => View(new ProjectItemCreateViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectItemCreateViewModel projectItemModel)
    {
        if (!ModelState.IsValid) {
            var errors = ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error => error.ErrorMessage)
                        .ToArray());

            return BadRequest(new { errors });
        }

        string? userId = userManager.GetUserId(User);
        if (userId is null)
            return Challenge();

        await services.AddProjectAsync(projectItemModel, userId);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> Update(int? projectId)
    {
        if (projectId is not { } id)
            return NotFound();

        ProjectItem? projectItem = await LoadAuthorizedAsync(services.GetProjectAsync(id));
        if (projectItem is null)
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
        ProjectItem? projectItem = await LoadAuthorizedAsync(services.GetProjectAsync(projectItemModel.Id));
        if (projectItem is null)
            return NotFound();

        if (!ModelState.IsValid)
            return View(projectItemModel);

        await services.UpdateProjectAsync(projectItem, projectItemModel);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int? projectId)
    {
        if (projectId is not { } id)
            return NotFound();

        ProjectItem? projectItem = await LoadAuthorizedAsync(services.GetProjectWithTasksAsync(id));
        if (projectItem is null)
            return NotFound();

        string? deletedByUserId = userManager.GetUserId(User);
        if (deletedByUserId is null)
            return Challenge();

        await services.DeleteProjectAsync(projectItem, deletedByUserId);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Restore()
    {
        if (User.IsInRole(Roles.Admin))
            return View(await services.GetDeletedProjectsAsync());

        string? userId = userManager.GetUserId(User);
        if (userId is null)
            return Challenge();

        return View(await services.GetDeletedProjectsForUserAsync(userId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int? projectId)
    {
        if (projectId is not { } id)
            return NotFound();

        ProjectItem? projectItem = await LoadAuthorizedAsync(services.GetDeletedProjectWithDeletedTasksAsync(id));
        if (projectItem is null)
            return NotFound();

        await services.RestoreProjectAsync(projectItem);
        return RedirectToAction(nameof(Restore));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PurgeProject(int? projectId)
    {
        if (projectId is not { } id)
            return NotFound();

        ProjectItem? projectItem = await LoadAuthorizedAsync(services.GetDeletedProjectWithAllTasksAsync(id));
        if (projectItem is null)
            return NotFound();

        await services.PurgeProjectAsync(projectItem);
        return RedirectToAction(nameof(Restore));
    }
}