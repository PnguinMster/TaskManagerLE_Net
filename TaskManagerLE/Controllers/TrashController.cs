using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskManagerLE.Authorization;
using TaskManagerLE.Models;
using TaskManagerLE.Models.Enums;
using TaskManagerLE.Services;

namespace TaskManagerLE.Controllers;

[Authorize]
public class TrashController(TrashService trashService, UserManager<ApplicationUser> userManager) : Controller
{
    // Where RestoreAll keeps the list of restored items until the toast's Undo is clicked.
    private const string UndoRestoreAllKey = "UndoRestoreAll";

    // About 100 bytes per item in the cookie; 50 items stays well under browser and server limits.
    private const int MaxUndoItems = 50;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        string? userId = userManager.GetUserId(User);
        if (userId is null)
            return Challenge();

        return View(await trashService.GetSummaryAsync(userId, User.IsInRole(Roles.Admin)));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EmptyTrash(bool confirmed)
    {
        // The dialog keeps the button disabled until the box is ticked; this is the server-side backstop.
        if (!confirmed)
            return BadRequest();

        string? userId = userManager.GetUserId(User);
        if (userId is null)
            return Challenge();

        await trashService.EmptyTrashAsync(userId, User.IsInRole(Roles.Admin));
        this.ShowToast(new ToastMessage(ToastKind.Info, "Trash emptied",
            "Everything in it was permanently deleted."));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreAll()
    {
        string? userId = userManager.GetUserId(User);
        if (userId is null)
            return Challenge();

        RestoreAllResult restored = await trashService.RestoreAllAsync(userId, User.IsInRole(Roles.Admin));
        int projects = restored.Projects.Count;
        int tasks = restored.Tasks.Count;
        if (projects + tasks == 0)
            return RedirectToAction(nameof(Index));

        // TempData lives in a cookie, so a very large restore gets no Undo rather than an oversized cookie.
        bool canUndo = projects + tasks <= MaxUndoItems;
        if (canUndo)
            TempData[UndoRestoreAllKey] = JsonSerializer.Serialize(restored);

        this.ShowToast(new ToastMessage(ToastKind.Success, "Trash restored",
            CountMessage(projects, tasks, "back."),
            ActionLabel: canUndo ? "Undo" : null,
            ActionUrl: canUndo ? Url.Action(nameof(UndoRestoreAll)) : null));

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UndoRestoreAll()
    {
        string? userId = userManager.GetUserId(User);
        if (userId is null)
            return Challenge();

        // Reading TempData removes the entry, so clicking Undo twice can't run it twice.
        // The cookie is encrypted, so the list can't be edited; the service still checks ownership.
        if (TempData[UndoRestoreAllKey] is not string json
            || JsonSerializer.Deserialize<RestoreAllResult>(json) is not { } restored)
            return RedirectToAction(nameof(Index));

        var (projects, tasks) = await trashService.UndoRestoreAllAsync(restored, userId, User.IsInRole(Roles.Admin));
        if (projects + tasks > 0)
            this.ShowToast(new ToastMessage(ToastKind.Info, "Restore undone",
                CountMessage(projects, tasks, "back in the Trash.")));

        return RedirectToAction(nameof(Index));
    }

    // e.g. "1 project is back.", "2 projects and 1 task are back in the Trash."
    private static string CountMessage(int projects, int tasks, string ending)
    {
        var parts = new List<string>();
        if (projects > 0)
            parts.Add(projects == 1 ? "1 project" : $"{projects} projects");
        if (tasks > 0)
            parts.Add(tasks == 1 ? "1 task" : $"{tasks} tasks");

        string verb = projects + tasks == 1 ? "is" : "are";
        return $"{string.Join(" and ", parts)} {verb} {ending}";
    }
}
