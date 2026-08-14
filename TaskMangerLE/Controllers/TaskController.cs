using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TaskMangerLE.Models;
using TaskMangerLE.Services;

namespace TaskMangerLE.Controllers;

public class TaskController(TaskService taskService, ProjectService projectService, IAuthorizationService authorizationService) : Controller
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

        TaskItemCreateViewModel taskItemModel = new TaskItemCreateViewModel { ProjectId = projectItem.Id };
        ViewData["ProjectName"] = projectItem.Name;
        return View(taskItemModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskItemCreateViewModel taskItemModel)
    {
        ProjectItem? projectItem = await projectService.FindProjectAsync(taskItemModel.ProjectId);
        if (projectItem == null)
            return NotFound();

        if (!ModelState.IsValid)
            return View(taskItemModel);

        await taskService.AddTaskAsync(taskItemModel);
        ViewData["ProjectName"] = projectItem.Name;
        return RedirectToAction(nameof(Index), nameof(Task), new { projectId = taskItemModel.ProjectId });
    }

    [HttpGet]
    public async Task<IActionResult> Update(int? taskId)
    {
        if (taskId == null)
            return NotFound();

        TaskItem? taskItem = await taskService.FindTaskAsync(taskId);

        if (taskItem == null)
            return NotFound();

        TaskItemUpdateViewModel taskItemModel = new TaskItemUpdateViewModel
        {
            Id = taskItem.Id,
            ProjectId = taskItem.ProjectId,
            Name = taskItem.Name,
            Description = taskItem.Description,
            Priority = taskItem.Priority,
            Status = taskItem.Status,
            DueDate = taskItem.DueDate,
        };

        ViewData["TaskName"] = taskItem.Name;
        return View(taskItemModel);
    }

    [HttpPost]
    public async Task<IActionResult> Update(TaskItemUpdateViewModel taskItemModel)
    {
        if (!ModelState.IsValid)
            return View(taskItemModel);

        bool isUpdated = await taskService.UpdateTaskAsync(taskItemModel);
        if (!isUpdated)
            return View(taskItemModel);

        return RedirectToAction(nameof(Index), nameof(Task), new { projectId = taskItemModel.ProjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int taskId)
    {
        TaskItem? taskItem = await taskService.FindTaskAsync(taskId);

        if (taskItem == null) return NotFound();

        var authResult = await authorizationService.AuthorizeAsync(User, taskItem.ProjectItem, "ProjectOwner");
        if (!authResult.Succeeded) return Forbid();

        string? deletedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await taskService.DeleteTask(taskItem, deletedByUserId);

        return RedirectToAction("Index", "Project", new { id = taskItem.ProjectId });
    }
}