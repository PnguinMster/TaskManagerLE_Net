using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TaskMangerLE.Models;
using TaskMangerLE.Services;
using TaskMangerLE.Authorization;

namespace TaskMangerLE.Controllers;

[Authorize]
public class TaskController(TaskService taskService, ProjectService projectService, IAuthorizationService authorizationService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? projectId)
    {
        ProjectItem? projectItem = await projectService.GetByIdWithTasksAsync(projectId);
        if (projectItem == null)
            return NotFound();

        var authResult = await authorizationService.AuthorizeAsync(User, projectItem, Policies.ProjectOwner);
        if (!authResult.Succeeded)
            return Forbid();

        ViewData["ProjectName"] = projectItem?.Name;
        return View(projectItem);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? projectId)
    {
        ProjectItem? projectItem = await projectService.FindProjectAsync(projectId);
        if (projectItem == null)
            return NotFound();

        var authResult = await authorizationService.AuthorizeAsync(User, projectItem, Policies.ProjectOwner);
        if (!authResult.Succeeded)
            return Forbid();

        ViewData["ProjectName"] = projectItem.Name;
        return View(new TaskItemCreateViewModel { ProjectId = projectItem.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskItemCreateViewModel taskItemModel)
    {
        ProjectItem? projectItem = await projectService.FindProjectAsync(taskItemModel.ProjectId);
        if (projectItem == null)
            return NotFound();

        var authResult = await authorizationService.AuthorizeAsync(User, projectItem, Policies.ProjectOwner);
        if (!authResult.Succeeded)
            return Forbid();

        if (!ModelState.IsValid)
            return View(taskItemModel);

        await taskService.AddTaskAsync(taskItemModel);
        ViewData["ProjectName"] = projectItem.Name;
        return RedirectToAction(nameof(Index), new { projectId = taskItemModel.ProjectId });
    }

    [HttpGet]
    public async Task<IActionResult> Update(int? taskId)
    {
        TaskItem? taskItem = await taskService.FindTaskAsync(taskId);
        if (taskItem == null)
            return NotFound();

        var authResult = await authorizationService.AuthorizeAsync(User, taskItem, Policies.ProjectOwner);
        if (!authResult.Succeeded)
            return Forbid();

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

        TaskItem? taskItem = await taskService.FindTaskAsync(taskItemModel.Id);
        if (taskItem == null)
            return NotFound();

        var authResult = await authorizationService.AuthorizeAsync(User, taskItem, Policies.ProjectOwner);
        if (!authResult.Succeeded)
            return Forbid();

        await taskService.UpdateTaskAsync(taskItem, taskItemModel);
        return RedirectToAction(nameof(Index), new { projectId = taskItemModel.ProjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int taskId)
    {
        TaskItem? taskItem = await taskService.FindTaskWithOwnerAsync(taskId);

        if (taskItem == null) return NotFound();

        var authResult = await authorizationService.AuthorizeAsync(User, taskItem.ProjectItem, Policies.ProjectOwner);
        if (!authResult.Succeeded) return Forbid();

        string? deletedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await taskService.DeleteTask(taskItem, deletedByUserId);
        return RedirectToAction(nameof(Index), new { projectId = taskItem.ProjectId });
    }

    [HttpGet]
    public async Task<IActionResult> Restore(int? projectId)
    {
        ProjectItem? projectItem = await projectService.GetDeletedProjectWithDeletedTasks(projectId);
        if (projectItem == null)
            return NotFound();

        var authResult = await authorizationService.AuthorizeAsync(User, projectItem, Policies.ProjectOwner);
        if (!authResult.Succeeded)
            return Forbid();

        return View(nameof(Restore), projectItem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Restore")]
    public async Task<IActionResult> RestoreConfirmed(int? taskId)
    {
        TaskItem? taskItem = await taskService.GetDeletedTask(taskId);
        if (taskItem == null) return NotFound();

        var authResult = await authorizationService.AuthorizeAsync(User, taskItem.ProjectItem, Policies.ProjectOwner);
        if (!authResult.Succeeded) return Forbid();

        await taskService.RestoreTask(taskItem);
        return RedirectToAction(nameof(Restore), new { projectId = taskItem.ProjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PurgeTask(int? taskId)
    {
        TaskItem? taskItem = await taskService.GetIsDeletedTask(taskId);
        if (taskItem == null) return NotFound();

        var authResult = await authorizationService.AuthorizeAsync(User, taskItem.ProjectItem, Policies.ProjectOwner);
        if (!authResult.Succeeded) return Forbid();

        int? taskProjectId = taskItem.ProjectId;
        await taskService.PurgeTask(taskItem);
        return RedirectToAction(nameof(Restore), new { projectId = taskProjectId });
    }
}