using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskMangerLE.Models;

namespace TaskMangerLE.Controllers;

public class AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public IActionResult Index(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LoginViewModel loginModel)
    {
        if (!ModelState.IsValid)
            return View(loginModel);

        ApplicationUser? user = loginModel.UserLogin.Contains('@')
            ? await userManager.FindByEmailAsync(loginModel.UserLogin)
            : await userManager.FindByNameAsync(loginModel.UserLogin);

        if (user == null) {
            ModelState.AddModelError(string.Empty, "Invalid login attempt");
            return View(loginModel);
        }

        var result = await signInManager.PasswordSignInAsync(user.UserName!, loginModel.Password, loginModel.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded) {
            if (!string.IsNullOrEmpty(loginModel.ReturnUrl) && Url.IsLocalUrl(loginModel.ReturnUrl))
                return Redirect(loginModel.ReturnUrl);

            return RedirectToAction("Index", "Project");
        }

        if (result.IsNotAllowed)
            ModelState.AddModelError(string.Empty, "Not allowed: email not confirmed!");
        else if (result.IsLockedOut)
            ModelState.AddModelError(string.Empty, "Account locked. Try again later.");
        else if (result.RequiresTwoFactor)
            ModelState.AddModelError(string.Empty, "Two-factor required.");
        else
            ModelState.AddModelError(string.Empty, "Wrong username or password.");

        return View(loginModel);
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterUser registerModel)
    {
        if (!ModelState.IsValid)
            return View(registerModel);

        ApplicationUser user = new ApplicationUser
        {
            UserName = registerModel.UserName,
            Email = registerModel.Email,
        };

        var result = await userManager.CreateAsync(user, registerModel.Password);
        if (!result.Succeeded) {
            foreach (IdentityError error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(registerModel);
        }

        await userManager.AddToRoleAsync(user, "User");
        await signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction(nameof(Index), "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Index), "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}