using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskMangerLE.Models;

namespace TaskMangerLE.Controllers;

public class AccountController(SignInManager<ApplicationUser> signInManager) : Controller
{
    [HttpGet]
    public IActionResult Index(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded) {
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Project");
        }

        if (result.IsLockedOut) {
            ModelState.AddModelError(string.Empty, "Account locked. Try again later.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}