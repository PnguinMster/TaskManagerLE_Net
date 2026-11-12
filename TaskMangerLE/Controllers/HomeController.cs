using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskMangerLE.Models;
using TaskMangerLE.Services;

namespace TaskMangerLE.Controllers;

public class HomeController(DashboardServices services, UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        string? userId = userManager.GetUserId(User);

        if (userId is null)
            return View(new DashboardViewModel { IsAuthenticated = false });

        DashboardViewModel model = await services.GetDashboardAsync(userId);
        return View(model);
    }

    [HttpGet]
    public IActionResult Privacy() => View();

    public IActionResult PageNotFound()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;

        // When UseStatusCodePagesWithReExecute sends us here, this feature holds the URL the user actually asked for.
        ViewData["OriginalPath"] = HttpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalPath;
        return View();
    }

    // Never cache an error response, or users could keep seeing it after the problem is fixed.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        // The trace id also appears in the logs, so a user can quote it and you can find the exception.
        ViewData["Reference"] = Activity.Current?.TraceId.ToString() ?? HttpContext.TraceIdentifier;

        // The page that threw, so "Try again" can go back to it.
        ViewData["FailedPath"] = HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Path;

        // The exception handler re-runs with the original method, so POST here means a form submit failed.
        ViewData["FailedOnPost"] = HttpMethods.IsPost(Request.Method);
        return View();
    }
}