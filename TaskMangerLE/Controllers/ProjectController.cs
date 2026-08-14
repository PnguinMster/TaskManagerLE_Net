using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TaskMangerLE.Models;
using TaskMangerLE.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace TaskMangerLE.Controllers;

[Authorize]
public class ProjectController(ProjectService services, UserManager<ApplicationUser> userManager, IAuthorizationService authorizationService) : Controller
{
    // GET
    public async Task<IActionResult> Index()
    {
        string? userId = userManager.GetUserId(User);
        bool isAdmin = User.IsInRole("Admin");

        if (isAdmin)
            return View(await services.GetAllAsync());

        return View(await services.GetUserAllAsync(userId));
    }

    public IActionResult Create()
    {
        return View(new ProjectItemCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectItemCreateViewModel projectItemModel)
    {
        if (!ModelState.IsValid)
            return View(projectItemModel);

        // User data
        string? userId = userManager.GetUserId(User);

        await services.AddProjectAsync(projectItemModel, userId);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Update(int? projectId)
    {
        if (projectId == null)
            return NotFound();

        ProjectItem? projectItem = await services.FindProjectAsync(projectId);

        if (projectItem == null)
            return NotFound();

        ProjectItemUpdateViewModel projectItemModel = new ProjectItemUpdateViewModel
        {
            Id = projectItem.Id,
            Name = projectItem.Name,
            Description = projectItem.Description,
            Status = projectItem.Status
        };

        var result = await authorizationService.AuthorizeAsync(User, projectItemModel, new ProjectOwnerRequirement());
        if (!result.Succeeded) return Forbid();

        ViewData["ProjectName"] = projectItem.Name;
        return View(projectItemModel);
    }

    [HttpPost]
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
    public async Task<IActionResult> Delete(int projectId)
    {
        ProjectItem? projectItem = await services.GetByIdWithTasksAsync(projectId);
        if (projectItem == null) return NotFound();

        var authResult = await authorizationService.AuthorizeAsync(User, projectItem, "ProjectOwner");
        if (!authResult.Succeeded) return Forbid();

        string? deletedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await services.DeleteProject(projectItem, deletedByUserId);

        return RedirectToAction("Index", "Project");
    }
}