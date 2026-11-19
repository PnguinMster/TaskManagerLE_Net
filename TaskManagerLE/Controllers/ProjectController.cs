using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TaskManagerLE.Models;
using TaskManagerLE.Models.Enums;
using TaskManagerLE.Services;
using TaskManagerLE.Authorization;

namespace TaskManagerLE.Controllers;

[Authorize]
public class ProjectController(ProjectService services, UserManager<ApplicationUser> userManager, IAuthorizationService authorizationService)
    : ItemControllerBase(authorizationService)
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        string? userId = userManager.GetUserId(User);
        if (userId is null)
            return Challenge();

        return View(await services.GetProjectCardsAsync(userId, User.IsInRole(Roles.Admin)));
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int? projectId)
    {
        if (projectId is not { } id)
            return NotFound();

        ProjectItem? projectItem = await LoadAuthorizedAsync(services.GetProjectAsync(id));
        if (projectItem is null)
            return NotFound();

        ProjectDetailViewModel? project = await services.GetProjectDetailAsync(id);
        if (project is null)
            return NotFound();

        string? userId = userManager.GetUserId(User);
        if (userId is null)
            return Challenge();

        ViewData["AvailableProjects"] = await services.GetProjectCardsAsync(userId, User.IsInRole(Roles.Admin));

        return View(project);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectItemCreateViewModel projectItemModel)
    {
        if (!ModelState.IsValid)
            return ValidationErrorsResult();

        string? userId = userManager.GetUserId(User);
        if (userId is null)
            return Challenge();

        await services.AddProjectAsync(projectItemModel, userId);

        // TempData is saved with any response, so the toast shows after site.js reloads the page.
        this.ShowToast(new ToastMessage(ToastKind.Success, "Project created",
            $"{projectItemModel.Name} was added."));
        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // Sent with fetch, like Create: errors go back as JSON and on success site.js reloads the page
    // the modal is on.
    public async Task<IActionResult> Update(ProjectItemUpdateViewModel projectItemModel)
    {
        ProjectItem? projectItem = await LoadAuthorizedAsync(services.GetProjectAsync(projectItemModel.Id));
        if (projectItem is null)
            return NotFound();

        if (!ModelState.IsValid)
            return ValidationErrorsResult();

        await services.UpdateProjectAsync(projectItem, projectItemModel);
        this.ShowToast(new ToastMessage(ToastKind.Success, "Project saved", $"{projectItem.Name} was updated."));
        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int? projectId, string? returnUrl)
    {
        if (projectId is not { } id)
            return NotFound();

        ProjectItem? projectItem = await LoadAuthorizedAsync(services.GetProjectWithTasksAsync(id));
        if (projectItem is null)
            return NotFound();

        string? deletedByUserId = userManager.GetUserId(User);
        if (deletedByUserId is null)
            return Challenge();

        await services.DeleteProjectAsync(projectItem, deletedByUserId);

        // Back to the page the delete came from, and Undo returns there too (same as TaskController.Delete).
        // The Detail page sends no returnUrl: that project's page is gone now, so it falls back to Index.
        string backUrl = this.LocalUrlOr(returnUrl, Url.Action(nameof(Index))!);

        this.ShowToast(new ToastMessage(ToastKind.Info, "Project moved to Trash",
            $"{projectItem.Name} and its tasks can be restored from the Trash.",
            ActionLabel: "Undo",
            ActionUrl: Url.Action(nameof(Restore), new { projectId = id, returnUrl = backUrl })));
        return LocalRedirect(backUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int? projectId, string? returnUrl)
    {
        if (projectId is not { } id)
            return NotFound();

        ProjectItem? projectItem = await LoadAuthorizedAsync(services.GetDeletedProjectWithDeletedTasksAsync(id));
        if (projectItem is null)
            return NotFound();

        await services.RestoreProjectAsync(projectItem);
        this.ShowToast(new ToastMessage(ToastKind.Success, "Project restored",
            $"{projectItem.Name} and its tasks are back."));

        // Undo from a toast sends the user back where they were.
        return this.RedirectToLocal(returnUrl, RedirectToAction("Index", "Trash"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PurgeProject(int? projectId, string? returnUrl)
    {
        if (projectId is not { } id)
            return NotFound();

        ProjectItem? projectItem = await LoadAuthorizedAsync(services.GetDeletedProjectWithAllTasksAsync(id));
        if (projectItem is null)
            return NotFound();

        await services.PurgeProjectAsync(projectItem);

        // No Undo: the rows are gone from the database.
        this.ShowToast(new ToastMessage(ToastKind.Info, "Project deleted",
            $"{projectItem.Name} and its tasks were permanently deleted."));

        // The Trash page sends a returnUrl.
        return this.RedirectToLocal(returnUrl, RedirectToAction("Index", "Trash"));
    }
}