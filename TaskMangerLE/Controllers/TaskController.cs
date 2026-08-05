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
        ViewData["ProjectName"] = projectItem?.Name;
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

        TaskItem taskItem = new TaskItem { ProjectId = projectItem.Id };
        ViewData["ProjectName"] = projectItem.Name;
        return View(taskItem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskItem taskItem)
    {
        ProjectItem? projectItem = await projectService.FindProjectAsync(taskItem.ProjectId);
        if (projectItem == null)
            return NotFound();

        if (!ModelState.IsValid)
            return View(taskItem);

        await taskService.AddTaskAsync(taskItem);
        ViewData["ProjectName"] = projectItem.Name;
        return RedirectToAction(nameof(Index), nameof(Task), new { projectId = taskItem.ProjectId });
    }

    public async Task<IActionResult> Update(int? taskId)
    {
        if (taskId == null)
            return NotFound();

        TaskItem? taskItem = await taskService.FindTaskAsync(taskId);

        if (taskItem == null)
            return NotFound();

        ViewData["TaskName"] = taskItem.Name;
        return View(taskItem);
    }

    [HttpPost]
    public async Task<IActionResult> Update(TaskItem taskItem)
    {
        if (!ModelState.IsValid)
            return View(taskItem);

        await taskService.UpdateTaskAsync(taskItem);
        return RedirectToAction(nameof(Index), nameof(Task), new { projectId = taskItem.ProjectId });
    }
}