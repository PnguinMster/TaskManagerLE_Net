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
        ProjectItem? projectItem = await LoadAuthorizedAsync(projectService.GetByIdWithTasksAsync(projectId));
        if (projectItem == null)
            return NotFound();

        ViewData["ProjectName"] = projectItem.Name;
        return View(projectItem);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? projectId)
    {
        ProjectItem? projectItem = await LoadAuthorizedAsync(projectService.FindProjectAsync(projectId));
        if (projectItem == null)
            return NotFound();

        ViewData["ProjectName"] = projectItem.Name;
        return View(new TaskItemCreateViewModel { ProjectId = projectItem.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskItemCreateViewModel taskItemModel)
    {
        ProjectItem? projectItem = await LoadAuthorizedAsync(projectService.FindProjectAsync(taskItemModel.ProjectId));
        if (projectItem == null)
            return NotFound();

        if (!ModelState.IsValid)
            return View(taskItemModel);

        await taskService.AddTaskAsync(taskItemModel);
        ViewData["ProjectName"] = projectItem.Name;
        return RedirectToAction(nameof(Index), new { projectId = taskItemModel.ProjectId });
    }

    [HttpGet]
    public async Task<IActionResult> Update(int? taskId)
    {
        TaskItem? taskItem = await LoadAuthorizedAsync(taskService.FindTaskAsync(taskId), t => t.ProjectItem);
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(TaskItemUpdateViewModel taskItemModel)
    {
        if (!ModelState.IsValid)
            return View(taskItemModel);

        TaskItem? taskItem = await LoadAuthorizedAsync(taskService.FindTaskAsync(taskItemModel.Id), t => t.ProjectItem);
        if (taskItem == null)
            return NotFound();

        await taskService.UpdateTaskAsync(taskItem, taskItemModel);
        return RedirectToAction(nameof(Index), new { projectId = taskItemModel.ProjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int taskId)
    {
        TaskItem? taskItem = await LoadAuthorizedAsync(taskService.FindTaskWithOwnerAsync(taskId), t => t.ProjectItem);
        if (taskItem == null)
            return NotFound();

        string? deletedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await taskService.DeleteTask(taskItem, deletedByUserId);
        return RedirectToAction(nameof(Index), new { projectId = taskItem.ProjectId });
    }

    [HttpGet]
    public async Task<IActionResult> Restore(int? projectId)
    {
        ProjectItem? projectItem = await LoadAuthorizedAsync(projectService.GetDeletedProjectWithDeletedTasks(projectId));
        if (projectItem == null)
            return NotFound();

        return View(nameof(Restore), projectItem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Restore")]
    public async Task<IActionResult> RestoreConfirmed(int? taskId)
    {
        TaskItem? taskItem = await LoadAuthorizedAsync(taskService.GetIsDeletedTask(taskId), t => t.ProjectItem);
        if (taskItem == null)
            return NotFound();

        await taskService.RestoreTask(taskItem);
        return RedirectToAction(nameof(Restore), new { projectId = taskItem.ProjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PurgeTask(int? taskId)
    {
        TaskItem? taskItem = await LoadAuthorizedAsync(taskService.GetIsDeletedTask(taskId), t => t.ProjectItem);
        if (taskItem == null)
            return NotFound();

        int? taskProjectId = taskItem.ProjectId;
        await taskService.PurgeTask(taskItem);
        return RedirectToAction(nameof(Restore), new { projectId = taskProjectId });
    }
}