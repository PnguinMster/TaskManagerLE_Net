using Microsoft.AspNetCore.Mvc;

namespace TaskMangerLE.Controllers;

public class HomeController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }
}