using Microsoft.AspNetCore.Mvc;
using TaskMangerLE.Models;
using TaskMangerLE.Services;

namespace TaskMangerLE.Controllers;

public class TaskController(TaskService services) : Controller
{
    // GET
    public async Task<IActionResult> Index()
    {
        return View(await services.GetAllAsync());
    }

    public IActionResult Create()
    {
        return View(new TaskItem());
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(TaskItem taskItem)
    {
        if (!ModelState.IsValid)
            return View(taskItem);

        await services.AddProjectAsync(taskItem);
        return RedirectToAction(nameof(Index));
    }
}