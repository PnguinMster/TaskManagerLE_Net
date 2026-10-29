using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskMangerLE.Authorization;
using TaskMangerLE.Models;

namespace TaskMangerLE.Controllers;

public class AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel loginModel)
    {
        if (!ModelState.IsValid)
            return View(loginModel);

        // Determine if logging using Email or Username
        ApplicationUser? user = loginModel.UsernameOrEmail.Contains('@')
            ? await userManager.FindByEmailAsync(loginModel.UsernameOrEmail)
            : await userManager.FindByNameAsync(loginModel.UsernameOrEmail);

        if (user is null) {
            ModelState.AddModelError(string.Empty, "Invalid login attempt");
            return View(loginModel);
        }

        var signInResult = await signInManager.PasswordSignInAsync(user.UserName!, loginModel.Password, loginModel.RememberMe, lockoutOnFailure: true);

        if (signInResult.Succeeded)
            return RedirectToAction("Index", "Project");


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
    public async Task<IActionResult> Register(RegisterViewModel registerModel)
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

        IdentityResult roleResult = await userManager.AddToRoleAsync(user, Roles.User);
        if (!roleResult.Succeeded) {
            await userManager.DeleteAsync(user);
            ModelState.AddModelError(string.Empty, "We couldn't finish setting up your account. Please try again later.");
            return View(registerModel);
        }

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