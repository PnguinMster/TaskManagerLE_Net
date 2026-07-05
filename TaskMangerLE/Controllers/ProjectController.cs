using Microsoft.AspNetCore.Mvc;

namespace TaskMangerLE.Controllers;

public class ProjectController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}