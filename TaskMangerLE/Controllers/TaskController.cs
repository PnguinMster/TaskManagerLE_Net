using Microsoft.AspNetCore.Mvc;
using TaskMangerLE.Models;
using TaskMangerLE.Services;

namespace TaskMangerLE.Controllers;

public class TaskController(TaskService taskService, ProjectService projectService) : Controller
{
    // GET
    public async Task<IActionResult> Index()
    {
        return View(await taskService.GetAllAsync());
    }

    // GET: Task/Create
    public async Task<IActionResult> Create(int? projectId)
    {
        if (projectId == null)
            return NotFound();

        ProjectItem? projectItem = await projectService.FindProjectAsync(projectId);

        if (projectItem == null)
            return NotFound();

        TaskItem taskItem = new TaskItem() { ProjectId = projectItem.Id };
        return View(taskItem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskItem taskItem)
    {
        ProjectItem? projectItem = await projectService.FindProjectAsync(taskItem.ProjectId);
        if (!ModelState.IsValid || projectItem == null)
            return View(taskItem);

        await taskService.AddProjectAsync(taskItem);
        return RedirectToAction(nameof(Index));
    }
}