using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TaskMangerLE.Models;
using TaskMangerLE.Services;

namespace TaskMangerLE.Controllers;

[Authorize]
public class TaskController(TaskService taskService, ProjectService projectService, IAuthorizationService authorizationService)
    : ItemControllerBase(authorizationService)
{
    [HttpGet]
    public async Task<IActionResult> Index(int? projectId)
    {
        if (projectId is not { } id)
            return NotFound();

        ProjectItem? projectItem = await LoadAuthorizedAsync(projectService.GetProjectWithTasksAsync(id));
        if (projectItem is null)
            return NotFound();

        ViewData["ProjectName"] = projectItem.Name;
        return View(projectItem);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? projectId)
    {
        if (projectId is not { } id)
            return NotFound();

        ProjectItem? projectItem = await LoadAuthorizedAsync(projectService.GetProjectAsync(id));
        if (projectItem is null)
            return NotFound();

        ViewData["ProjectName"] = projectItem.Name;
        return View(new TaskItemCreateViewModel
        {
            ProjectId = projectItem.Id,
            DueDate = DateOnly.FromDateTime(DateTime.Today)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskItemCreateViewModel taskItemModel)
    {
        if (!ModelState.IsValid)
            return View(taskItemModel);

        ProjectItem? projectItem = await LoadAuthorizedAsync(projectService.GetProjectAsync(taskItemModel.ProjectId));
        if (projectItem is null)
            return NotFound();

        ViewData["ProjectName"] = projectItem.Name;
        await taskService.AddTaskAsync(taskItemModel);
        return RedirectToAction(nameof(Index), new { projectId = taskItemModel.ProjectId });
    }

    [HttpGet]
    public async Task<IActionResult> Update(int? taskId)
    {
        if (taskId is not { } id)
            return NotFound();

        TaskItem? taskItem = await LoadAuthorizedAsync(taskService.GetTaskWithProjectAsync(id), t => t.Project);
        if (taskItem is null)
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(TaskItemUpdateViewModel taskItemModel)
    {
        if (!ModelState.IsValid)
            return View(taskItemModel);

        TaskItem? taskItem = await LoadAuthorizedAsync(taskService.GetTaskWithProjectAsync(taskItemModel.Id), t => t.Project);
        if (taskItem is null)
            return NotFound();

        await taskService.UpdateTaskAsync(taskItem, taskItemModel);
        return RedirectToAction(nameof(Index), new { projectId = taskItem.ProjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int? taskId)
    {
        if (taskId is not { } id)
            return NotFound();

        TaskItem? taskItem = await LoadAuthorizedAsync(taskService.GetTaskWithProjectAsync(id), t => t.Project);
        if (taskItem is null)
            return NotFound();

        string? deletedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (deletedByUserId is null)
            return Challenge();

        await taskService.DeleteTaskAsync(taskItem, deletedByUserId);
        return RedirectToAction(nameof(Index), new { projectId = taskItem.ProjectId });
    }

    [HttpGet]
    public async Task<IActionResult> Restore(int? projectId)
    {
        if (projectId is not { } id)
            return NotFound();

        ProjectItem? projectItem = await LoadAuthorizedAsync(projectService.GetProjectWithDeletedTasksAsync(id));
        if (projectItem is null)
            return NotFound();

        return View(nameof(Restore), projectItem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Restore")]
    public async Task<IActionResult> RestoreConfirmed(int? taskId)
    {
        if (taskId is not { } id)
            return NotFound();

        TaskItem? taskItem = await LoadAuthorizedAsync(taskService.GetDeletedTaskAsync(id), t => t.Project);
        if (taskItem is null)
            return NotFound();

        await taskService.RestoreTaskAsync(taskItem);
        return RedirectToAction(nameof(Restore), new { projectId = taskItem.ProjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PurgeTask(int? taskId)
    {
        if (taskId is not { } id)
            return NotFound();

        TaskItem? taskItem = await LoadAuthorizedAsync(taskService.GetDeletedTaskAsync(id), t => t.Project);
        if (taskItem is null)
            return NotFound();

        int taskProjectId = taskItem.ProjectId;
        await taskService.PurgeTaskAsync(taskItem);
        return RedirectToAction(nameof(Restore), new { projectId = taskProjectId });
    }
}