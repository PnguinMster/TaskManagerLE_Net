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
}