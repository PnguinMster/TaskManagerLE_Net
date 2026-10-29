using Microsoft.AspNetCore.Mvc;

namespace TaskMangerLE.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult Privacy() => View();

    public IActionResult PageNotFound()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View();
    }
}