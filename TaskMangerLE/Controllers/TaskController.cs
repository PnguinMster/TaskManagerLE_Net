using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TaskMangerLE.Extensions;
using TaskMangerLE.Models;
using TaskMangerLE.Models.Enums;
using TaskMangerLE.Services;

namespace TaskMangerLE.Controllers;

[Authorize]
public class TaskController(
	UserManager<ApplicationUser> userManager,
	TaskService taskService,
	ProjectService projectService,
	IAuthorizationService authorizationService,
	TimeProvider timeProvider) : ItemControllerBase(authorizationService)
{
	[HttpGet]
	// showArchived comes from the query string (?showArchived=true); missing means false, so archived
	// projects' tasks are hidden by default.
	public async Task<IActionResult> Index(bool showArchived = false)
	{
		string? userId = userManager.GetUserId(User);
		if (userId is null)
			return Challenge();

		TaskListViewModel model = await taskService.GetTaskListAsync(userId, showArchived);
		ViewData["AvailableProjects"] =
			await projectService.GetProjectCardsAsync(userId);

		return View(model);
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
			DueDate = timeProvider.Today()
		});
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(TaskItemCreateViewModel taskItemModel, string? returnUrl)
	{
		ProjectItem? projectItem = await LoadAuthorizedAsync(
			projectService.GetProjectAsync(taskItemModel.ProjectId));

		if (projectItem is null)
			return NotFound();

		if (!ModelState.IsValid) {
			string? userId = userManager.GetUserId(User);
			if (userId is null)
				return Challenge();

			ViewData["AvailableProjects"] =
				await projectService.GetProjectCardsAsync(userId);

			ViewData["CreateTaskModel"] = taskItemModel;
			// Keep the original page, so fixing the errors and saving still goes back there.
			ViewData["ReturnUrl"] = returnUrl;
			return View(nameof(Index), await taskService.GetTaskListAsync(userId, showArchived: false));
		}

		await taskService.AddTaskAsync(taskItemModel);

		return this.RedirectToLocal(returnUrl, RedirectToAction(nameof(Index)));
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
	public async Task<IActionResult> Update(TaskItemUpdateViewModel taskItemModel, string? returnUrl)
	{
		TaskItem? taskItem = await LoadAuthorizedAsync(taskService.GetTaskWithProjectAsync(taskItemModel.Id), t => t.Project);
		if (taskItem is null)
			return NotFound();

		if (!ModelState.IsValid)
			return View(taskItemModel);

		await taskService.UpdateTaskAsync(taskItem, taskItemModel);

		return this.RedirectToLocal(returnUrl, RedirectToAction("Detail", "Project", new { projectId = taskItem.ProjectId }));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Complete(int? taskId)
	{
		if (taskId is not { } id)
			return NotFound();

		TaskItem? taskItem = await LoadAuthorizedAsync(taskService.GetTaskWithProjectAsync(id), t => t.Project);
		if (taskItem is null)
			return NotFound();

		await taskService.CompleteTaskAsync(taskItem);
		return RedirectToAction("Index", "Home");
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
		// Checked once here (same open-redirect rule as RedirectToLocal), then used for both.
		string backUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(Index))!;

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
	public async Task<IActionResult> RestoreConfirmed(int? taskId, string? returnUrl)
	{
		if (taskId is not { } id)
			return NotFound();

		TaskItem? taskItem = await LoadAuthorizedAsync(taskService.GetDeletedTaskAsync(id), t => t.Project);
		if (taskItem is null)
			return NotFound();

		await taskService.RestoreTaskAsync(taskItem);
		this.ShowToast(new ToastMessage(ToastKind.Success, "Task restored", $"{taskItem.Name} is back."));

		return this.RedirectToLocal(returnUrl, RedirectToAction(nameof(Restore), new { projectId = taskItem.ProjectId }));
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

		int taskProjectId = taskItem.ProjectId;
		await taskService.PurgeTaskAsync(taskItem);

		// The Trash page sends a returnUrl.
		return this.RedirectToLocal(returnUrl, RedirectToAction(nameof(Restore), new { projectId = taskProjectId }));
	}
}