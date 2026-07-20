using Microsoft.AspNetCore.Mvc;
using TaskMangerLE.Models;
using TaskMangerLE.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace TaskMangerLE.Controllers;

public class ProjectController(WorkServices services) : Controller
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
    public async Task<IActionResult> Create(ProjectItem project)
    {
        if (!ModelState.IsValid)
            return View(project);

        await services.AddProjectAsync(project);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Update(int? id)
    {
        if (id == null)
            return NotFound();

        ProjectItem? projectItem = await services.FindProjectAsync(id);

        if (projectItem == null)
            return NotFound();

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