using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TaskManagerLE.Models;
using TaskManagerLE.Models.Enums;
using TaskManagerLE.Services;
using TaskManagerLE.Authorization;
using TaskStatus = TaskManagerLE.Models.Enums.TaskStatus;

namespace TaskManagerLE.Controllers;

[Authorize]
public class TaskController(
    UserManager<ApplicationUser> userManager,
    TaskService taskService,
    ProjectService projectService,
    IAuthorizationService authorizationService) : ItemControllerBase(authorizationService)
{
    [HttpGet]
    // showArchived comes from the query string (?showArchived=true); missing means false, so archived
    // projects' tasks are hidden by default.
    public async Task<IActionResult> Index(bool showArchived = false)
    {
        string? userId = userManager.GetUserId(User);
        if (userId is null)
            return Challenge();

        bool isAdmin = User.IsInRole(Roles.Admin);
        TaskListViewModel model = await taskService.GetTaskListAsync(userId, isAdmin, showArchived);
        ViewData["AvailableProjects"] =
            await projectService.GetProjectCardsAsync(userId, isAdmin);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // Sent with fetch, like ProjectController.Create: errors go back as JSON so the modal stays open,
    // and on success site.js reloads the page the modal is on (Tasks or a project's board).
    public async Task<IActionResult> Create(TaskItemCreateViewModel model)
    {
        ProjectItem? projectItem = await LoadAuthorizedAsync(
            projectService.GetProjectAsync(model.ProjectId));

        if (projectItem is null)
            return NotFound();

        if (!ModelState.IsValid)
            return ValidationErrorsResult();

        await taskService.AddTaskAsync(model);
        this.ShowToast(new ToastMessage(ToastKind.Success, "Task created",
            $"{model.Name} was added to {projectItem.Name}."));
        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // Sent with fetch, like Create: errors go back as JSON and on success site.js reloads the page
    // the modal is on (Tasks or a project's board).
    public async Task<IActionResult> Update(TaskItemUpdateViewModel model)
    {
        TaskItem? taskItem = await LoadAuthorizedAsync(taskService.GetTaskWithProjectAsync(model.Id), t => t.Project);
        if (taskItem is null)
            return NotFound();

        // Moving to another project: that project must be the user's too (same ownership rule as everywhere),
        // or a crafted form could put a task into someone else's project.
        ProjectItem? targetProject = null;
        if (model.ProjectId != taskItem.ProjectId) {
            targetProject = await LoadAuthorizedAsync(projectService.GetProjectAsync(model.ProjectId));
            if (targetProject is null)
                return NotFound();
        }

        if (!ModelState.IsValid)
            return ValidationErrorsResult();

        await taskService.UpdateTaskAsync(taskItem, model);

        // The task leaves the board it was on, so say where it went. TempData is saved with any
        // response, not just redirects, so the toast shows after site.js reloads the page.
        if (targetProject is not null)
            this.ShowToast(new ToastMessage(ToastKind.Success, "Task moved",
                $"{taskItem.Name} is now in {targetProject.Name}."));
        else
            this.ShowToast(new ToastMessage(ToastKind.Success, "Task saved", $"{taskItem.Name} was updated."));

        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int? taskId, string? returnUrl)
    {
        if (taskId is not { } id)
            return NotFound();

        TaskItem? taskItem = await LoadAuthorizedAsync(taskService.GetTaskWithProjectAsync(id), t => t.Project);
        if (taskItem is null)
            return NotFound();

        string? deletedByUserId = userManager.GetUserId(User);
        if (deletedByUserId is null)
            return Challenge();

        await taskService.DeleteTaskAsync(taskItem, deletedByUserId);

        // Back to the page the delete came from (Tasks or a project's board), and Undo returns there too.
        string backUrl = this.LocalUrlOr(returnUrl, Url.Action(nameof(Index))!);

        this.ShowToast(new ToastMessage(ToastKind.Info, "Task moved to Trash",
            $"{taskItem.Name} can be restored from the Trash.",
            ActionLabel: "Undo",
            ActionUrl: Url.Action(nameof(Restore), new { taskId = id, returnUrl = backUrl })));
        return LocalRedirect(backUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCompletion(int? taskId, string? returnUrl)
    {
        if (taskId is not { } id)
            return NotFound();

        TaskItem? taskItem = await LoadAuthorizedAsync(
            taskService.GetTaskWithProjectAsync(id),
            t => t.Project);

        if (taskItem is null)
            return NotFound();

        TaskStatus previousStatus = taskItem.Status;
        await taskService.ToggleCompletionAsync(taskItem);

        // Checked once here, then used for both the redirect and Undo (same as Delete).
        string backUrl = Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Action("Detail", "Project", new { projectId = taskItem.ProjectId })!;

        // Completing offers Undo on every page, so a stray click is easy to take back (on Home the
        // task also drops off the list). Un-completing needs no Undo: just tick the box again.
        if (taskItem.Status == TaskStatus.Done)
            this.ShowToast(new ToastMessage(ToastKind.Success, "Task completed",
                $"{taskItem.Name} is done.",
                ActionLabel: "Undo",
                ActionUrl: Url.Action(nameof(UndoCompletion), new { taskId = id, previousStatus, returnUrl = backUrl })));

        return LocalRedirect(backUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UndoCompletion(int? taskId, TaskStatus previousStatus, string? returnUrl)
    {
        if (taskId is not { } id)
            return NotFound();

        // Model binding accepts any number for an enum, so check it's a real status.
        if (!Enum.IsDefined(previousStatus))
            return BadRequest();

        TaskItem? taskItem = await LoadAuthorizedAsync(taskService.GetTaskWithProjectAsync(id), t => t.Project);
        if (taskItem is null)
            return NotFound();

        // Only undo if it is still done. If it was changed since (e.g. edited in another tab),
        // that newer change wins.
        if (taskItem.Status == TaskStatus.Done)
            await taskService.UndoCompletionAsync(taskItem, previousStatus);

        return this.RedirectToLocal(returnUrl, RedirectToAction("Index", "Home"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int? taskId, string? returnUrl)
    {
        if (taskId is not { } id)
            return NotFound();

        TaskItem? taskItem = await LoadAuthorizedAsync(taskService.GetDeletedTaskAsync(id), t => t.Project);
        if (taskItem is null)
            return NotFound();

        await taskService.RestoreTaskAsync(taskItem);
        this.ShowToast(new ToastMessage(ToastKind.Success, "Task restored", $"{taskItem.Name} is back."));

        return this.RedirectToLocal(returnUrl, RedirectToAction("Index", "Trash"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Purge(int? taskId, string? returnUrl)
    {
        if (taskId is not { } id)
            return NotFound();

        TaskItem? taskItem = await LoadAuthorizedAsync(taskService.GetDeletedTaskAsync(id), t => t.Project);
        if (taskItem is null)
            return NotFound();

        await taskService.PurgeTaskAsync(taskItem);

        // No Undo: the row is gone from the database.
        this.ShowToast(new ToastMessage(ToastKind.Info, "Task deleted",
            $"{taskItem.Name} was permanently deleted."));

        // The Trash page sends a returnUrl.
        return this.RedirectToLocal(returnUrl, RedirectToAction("Index", "Trash"));
    }
}
