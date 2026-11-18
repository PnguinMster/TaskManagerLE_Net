using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TaskMangerLE.Models;
using TaskMangerLE.Models.Enums;
using TaskMangerLE.Services;
using TaskMangerLE.Authorization;

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
	public async Task<IActionResult> Update(TaskItemUpdateViewModel taskItemModel, string? returnUrl)
	{
		TaskItem? taskItem = await LoadAuthorizedAsync(taskService.GetTaskWithProjectAsync(taskItemModel.Id), t => t.Project);
		if (taskItem is null)
			return NotFound();

		// Back to the page the modal was on (Tasks or a project's board), whether or not it saved.
		IActionResult goBack = this.RedirectToLocal(returnUrl,
			RedirectToAction("Detail", "Project", new { projectId = taskItem.ProjectId }));

		// Moving to another project: that project must be the user's too (same ownership rule as everywhere),
		// or a crafted form could put a task into someone else's project.
		ProjectItem? targetProject = null;
		if (taskItemModel.ProjectId != taskItem.ProjectId) {
			targetProject = await LoadAuthorizedAsync(projectService.GetProjectAsync(taskItemModel.ProjectId));
			if (targetProject is null)
				return NotFound();
		}

		if (!ModelState.IsValid) {
			ShowValidationErrorToast();
			return goBack;
		}

		await taskService.UpdateTaskAsync(taskItem, taskItemModel);

		// The task leaves the board it was on, so say where it went.
		if (targetProject is not null)
			this.ShowToast(new ToastMessage(ToastKind.Success, "Task moved",
				$"{taskItem.Name} is now in {targetProject.Name}."));

		return goBack;
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

		await taskService.ToggleCompletionAsync(taskItem);

		return this.RedirectToLocal(returnUrl, RedirectToAction("Detail", "Project", new { projectId = taskItem.ProjectId }));
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