using Microsoft.AspNetCore.Mvc;
using TaskMangerLE.Models;
using TaskMangerLE.Services;

namespace TaskMangerLE.Controllers;

public class ProjectController(WorkServices services) : Controller
{
    // GET
    public IActionResult Index()
    {
        return View(services.GetAll());
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProjectItem project)
    {
        if (!ModelState.IsValid)
            return View(project);

        services.AddProject(project);
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Rename(int id)
    {
        ProjectItem? projectItem = services.GetProjectById(id);
        if (projectItem == null)
            return NotFound();

        return View(projectItem);
    }

    [HttpPost]
    public async Task<IActionResult> Rename(int? id, ProjectItem projectItem)
    {
        if (id != projectItem.Id)
            return NotFound();

        if (ModelState.IsValid) {
            services.RenameProject(projectItem);
            return RedirectToAction(nameof(Index));
        }

        return View(projectItem);
    }
}