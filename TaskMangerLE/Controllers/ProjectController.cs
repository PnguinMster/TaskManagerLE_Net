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
        return View(new ProjectItem());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectItem projectItem)
    {
        if (!ModelState.IsValid)
            return View(projectItem);

        // User data
        projectItem.OwnerId = userManager.GetUserId(User)!;

        await services.AddProjectAsync(projectItem);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Update(int? projectId)
    {
        if (projectId == null)
            return NotFound();

        ProjectItem? projectItem = await services.FindProjectAsync(projectId);

        if (projectItem == null)
            return NotFound();

        var result = await authorizationService.AuthorizeAsync(User, projectItem, new ProjectOwnerRequirement());
        if (!result.Succeeded) return Forbid();

        ViewData["ProjectName"] = projectItem.Name;
        return View(projectItem);
    }

    [HttpPost]
    public async Task<IActionResult> Update(ProjectItem projectItem)
    {
        if (!ModelState.IsValid)
            return View(projectItem);

        await services.UpdateProjectAsync(projectItem);
        return RedirectToAction(nameof(Index));
    }
}