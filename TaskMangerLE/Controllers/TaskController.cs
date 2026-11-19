using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TaskMangerLE.Models;
using TaskMangerLE.Models.Enums;
using TaskMangerLE.Services;
using TaskMangerLE.Authorization;
using TaskStatus = TaskMangerLE.Models.Enums.TaskStatus;

namespace TaskMangerLE.Controllers;

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
	public async Task<IActionResult> Create(TaskItemCreateViewModel taskItemModel)
	{
		ProjectItem? projectItem = await LoadAuthorizedAsync(
			projectService.GetProjectAsync(taskItemModel.ProjectId));

		if (projectItem is null)
			return NotFound();

		if (!ModelState.IsValid)
			return ValidationErrorsResult();

		await taskService.AddTaskAsync(taskItemModel);
		return Ok();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	// Sent with fetch, like Create: errors go back as JSON and on success site.js reloads the page
	// the modal is on (Tasks or a project's board).
	public async Task<IActionResult> Update(TaskItemUpdateViewModel taskItemModel)
	{
		TaskItem? taskItem = await LoadAuthorizedAsync(taskService.GetTaskWithProjectAsync(taskItemModel.Id), t => t.Project);
		if (taskItem is null)
			return NotFound();

		// Moving to another project: that project must be the user's too (same ownership rule as everywhere),
		// or a crafted form could put a task into someone else's project.
		ProjectItem? targetProject = null;
		if (taskItemModel.ProjectId != taskItem.ProjectId) {
			targetProject = await LoadAuthorizedAsync(projectService.GetProjectAsync(taskItemModel.ProjectId));
			if (targetProject is null)
				return NotFound();
		}

		if (!ModelState.IsValid)
			return ValidationErrorsResult();

		await taskService.UpdateTaskAsync(taskItem, taskItemModel);

		// The task leaves the board it was on, so say where it went. TempData is saved with any
		// response, not just redirects, so the toast shows after site.js reloads the page.
		if (targetProject is not null)
			this.ShowToast(new ToastMessage(ToastKind.Success, "Task moved",
				$"{taskItem.Name} is now in {targetProject.Name}."));

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
	// offerUndo comes from a hidden field on the Home page, where a completed task drops off the list,
	// so a stray click would otherwise leave no way back. Tasks and the boards keep showing the task.
	public async Task<IActionResult> ToggleCompletion(int? taskId, string? returnUrl, bool offerUndo = false)
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

		if (offerUndo && taskItem.Status == TaskStatus.Done)
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
	public async Task<IActionResult> PurgeTask(int? taskId, string? returnUrl)
	{
		if (taskId is not { } id)
			return NotFound();

		TaskItem? taskItem = await LoadAuthorizedAsync(taskService.GetDeletedTaskAsync(id), t => t.Project);
		if (taskItem is null)
			return NotFound();

		await taskService.PurgeTaskAsync(taskItem);

		// The Trash page sends a returnUrl.
		return this.RedirectToLocal(returnUrl, RedirectToAction("Index", "Trash"));
	}
}