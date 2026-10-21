using Microsoft.AspNetCore.Mvc;

namespace TaskMangerLE.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Privacy() => View();
}