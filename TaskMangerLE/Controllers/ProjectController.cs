using Microsoft.AspNetCore.Mvc;
using TaskMangerLE.Models;
using TaskMangerLE.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace TaskMangerLE.Controllers;

[Authorize]
public class ProjectController(ProjectService services) : Controller
{
    // GET
    public async Task<IActionResult> Index()
    {
        return View(await services.GetAllAsync());
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