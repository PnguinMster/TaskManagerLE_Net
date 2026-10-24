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
        var (projectItem, error) = await LoadAndAuthAsync(() => projectService.GetByIdWithTasksAsync(projectId), p => p);
        if (error != null)
            return error;

        ViewData["ProjectName"] = projectItem?.Name;
        return View(projectItem);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? projectId)
    {
        var (projectItem, error) = await LoadAndAuthAsync(() => projectService.FindProjectAsync(projectId), p => p);
        if (error != null)
            return error;

        ViewData["ProjectName"] = projectItem.Name;
        return View(new TaskItemCreateViewModel { ProjectId = projectItem.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskItemCreateViewModel taskItemModel)
    {
        var (projectItem, error) = await LoadAndAuthAsync(() => projectService.FindProjectAsync(taskItemModel.ProjectId), p => p);
        if (error != null)
            return error;

        if (!ModelState.IsValid)
            return View(taskItemModel);

        await taskService.AddTaskAsync(taskItemModel);
        ViewData["ProjectName"] = projectItem.Name;
        return RedirectToAction(nameof(Index), new { projectId = taskItemModel.ProjectId });
    }

    [HttpGet]
    public async Task<IActionResult> Update(int? taskId)
    {
        var (taskItem, error) = await LoadAndAuthAsync(() => taskService.FindTaskAsync(taskId), t => t.ProjectItem);
        if (error != null)
            return error;

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

        var (taskItem, error) = await LoadAndAuthAsync(() => taskService.FindTaskAsync(taskItemModel.Id), t => t.ProjectItem);
        if (error != null)
            return error;

        await taskService.UpdateTaskAsync(taskItem, taskItemModel);
        return RedirectToAction(nameof(Index), new { projectId = taskItemModel.ProjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int taskId)
    {
        var (taskItem, error) = await LoadAndAuthAsync(() => taskService.FindTaskWithOwnerAsync(taskId), t => t.ProjectItem);
        if (error != null)
            return error;

        string? deletedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await taskService.DeleteTask(taskItem, deletedByUserId);
        return RedirectToAction(nameof(Index), new { projectId = taskItem.ProjectId });
    }

    [HttpGet]
    public async Task<IActionResult> Restore(int? projectId)
    {
        var (projectItem, error) = await LoadAndAuthAsync(() => projectService.GetDeletedProjectWithDeletedTasks(projectId), p => p);
        if (error != null)
            return error;

        return View(nameof(Restore), projectItem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Restore")]
    public async Task<IActionResult> RestoreConfirmed(int? taskId)
    {
        var (taskItem, error) = await LoadAndAuthAsync(() => taskService.GetIsDeletedTask(taskId), t => t.ProjectItem);
        if (error != null)
            return error;

        await taskService.RestoreTask(taskItem);
        return RedirectToAction(nameof(Restore), new { projectId = taskItem.ProjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PurgeTask(int? taskId)
    {
        var (taskItem, error) = await LoadAndAuthAsync(() => taskService.GetIsDeletedTask(taskId), t => t.ProjectItem);
        if (error != null)
            return error;

        int? taskProjectId = taskItem.ProjectId;
        await taskService.PurgeTask(taskItem);
        return RedirectToAction(nameof(Restore), new { projectId = taskProjectId });
    }

    private async Task<(T Item, IActionResult? Error)> LoadAndAuthAsync<T>(Func<Task<T?>> getItem, Func<T, object> getResource) where T : ItemBase
    {
        T? item = await getItem();
        if (item == null)
            return (null, NotFound())!;

        var authResult = await authorizationService.AuthorizeAsync(User, getResource(item), Policies.ProjectOwner);
        if (!authResult.Succeeded)
            return (null, Forbid())!;

        return (item, null);
    }
}