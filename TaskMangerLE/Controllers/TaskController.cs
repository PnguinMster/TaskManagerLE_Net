using Microsoft.AspNetCore.Mvc;
using TaskMangerLE.Models;
using TaskMangerLE.Services;

namespace TaskMangerLE.Controllers;

public class TaskController(TaskService taskService, ProjectService projectService) : Controller
{
    // GET
    public async Task<IActionResult> Index(int? projectId)
    {
        if (projectId == null)
            return NotFound();

        ProjectItem? projectItem = await projectService.GetByIdWithTasksAsync(projectId);
        return View(projectItem);
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
        ViewData["ProjectName"] = projectItem.Name;
        return View(taskItem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int projectId, TaskItem taskItem)
    {
        ProjectItem? projectItem = await projectService.FindProjectAsync(taskItem.ProjectId);
        if (!ModelState.IsValid || projectItem == null)
            return View(taskItem);

        taskItem.ProjectId = projectId;

        await taskService.AddProjectAsync(taskItem);
        return RedirectToAction(nameof(Index), projectId);
    }
}