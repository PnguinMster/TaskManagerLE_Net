using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskMangerLE.Authorization;
using TaskMangerLE.Models;

namespace TaskMangerLE.Controllers;

public class AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel loginModel)
    {
        if (!ModelState.IsValid)
            return View(loginModel);

        // Determine if logging using Email or Username
        ApplicationUser? user = loginModel.UserLogin.Contains('@')
            ? await userManager.FindByEmailAsync(loginModel.UserLogin)
            : await userManager.FindByNameAsync(loginModel.UserLogin);

        if (user == null) {
            ModelState.AddModelError(string.Empty, "Invalid login attempt");
            return View(loginModel);
        }

        var signInResult = await signInManager.PasswordSignInAsync(user.UserName!, loginModel.Password, loginModel.RememberMe, lockoutOnFailure: true);

        if (signInResult.Succeeded) {
            if (!string.IsNullOrEmpty(loginModel.ReturnUrl) && Url.IsLocalUrl(loginModel.ReturnUrl))
                return Redirect(loginModel.ReturnUrl);

            return RedirectToAction("Index", "Project");
        }

        // General invalid login
        ModelState.AddModelError(string.Empty, signInResult.IsLockedOut ? "Account locked. Try again later." : "Invalid login attempt");
        /*
         // Testing invalid login
        if (result.IsLockedOut)
            ModelState.AddModelError(string.Empty, "Account locked. Try again later.");
        else if (result.IsNotAllowed)
            ModelState.AddModelError(string.Empty, "Not allowed: email not confirmed!");
        else if (result.RequiresTwoFactor)
            ModelState.AddModelError(string.Empty, "Two-factor required.");
        else
            ModelState.AddModelError(string.Empty, "Wrong username or password.");
            */

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

        // User 
        ApplicationUser user = new ApplicationUser
        {
            UserName = registerModel.UserName,
            Email = registerModel.Email,
        };

        var registerResult = await userManager.CreateAsync(user, registerModel.Password);

        if (!registerResult.Succeeded) {
            foreach (IdentityError error in registerResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(registerModel);
        }

        await userManager.AddToRoleAsync(user, Roles.User);
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
    public IActionResult AccessDenied() => View();
}