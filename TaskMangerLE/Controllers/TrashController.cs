using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskMangerLE.Authorization;
using TaskMangerLE.Models;
using TaskMangerLE.Services;

namespace TaskMangerLE.Controllers;

[Authorize]
public class TrashController(TrashService trashService, UserManager<ApplicationUser> userManager) : Controller
{
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
        return RedirectToAction(nameof(Index));
    }
}
