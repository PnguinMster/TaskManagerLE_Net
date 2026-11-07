using Microsoft.AspNetCore.Mvc;

namespace TaskMangerLE.Controllers;

public class TrashController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}