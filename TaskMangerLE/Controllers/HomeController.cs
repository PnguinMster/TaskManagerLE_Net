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
        return View();
    }
}