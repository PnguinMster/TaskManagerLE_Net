using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore.Query.Internal;
using TaskMangerLE.Authorization;
using TaskMangerLE.Models;

namespace TaskMangerLE.Controllers;

public class AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IEmailSender emailSender) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Challenge();

        return View(new ProfileViewModel
        {
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            NewUserName = user.UserName ?? string.Empty,
            NewEmail = user.Email ?? string.Empty,
            MemberSinceUtc = user.CreatedAtUtc // See note below if this property has a different name.
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Challenge();

        if (!ModelState.IsValid) {
            model.UserName = user.UserName ?? string.Empty;
            model.Email = user.Email ?? string.Empty;
            return View(model);
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, model.CurrentPassword);
        if (!passwordValid) {
            ModelState.AddModelError(nameof(model.CurrentPassword), "The current password is incorrect.");
            model.UserName = user.UserName ?? string.Empty;
            model.Email = user.Email ?? string.Empty;
            return View(model);
        }

        var newUserName = model.NewUserName.Trim();
        var newEmail = model.NewEmail.Trim();

        if (!string.Equals(user.UserName, newUserName, StringComparison.Ordinal)) {
            var result = await userManager.SetUserNameAsync(user, newUserName);
            if (!result.Succeeded) {
                AddIdentityErrors(result);
                model.UserName = user.UserName ?? string.Empty;
                model.Email = user.Email ?? string.Empty;
                return View(model);
            }
        }

        if (!string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase)) {
            var code = await userManager.GenerateChangeEmailTokenAsync(user, newEmail);
            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Action(
                nameof(ConfirmEmailChange),
                "Account",
                new { userId = user.Id, email = newEmail, code = encodedCode },
                protocol: Request.Scheme);

            await emailSender.SendEmailAsync(
                newEmail,
                "Confirm your new email address",
                $"Confirm your new email address by <a href='{callbackUrl}'>clicking here</a>.");

            TempData["StatusMessage"] = "Username saved. Check your new email address to confirm the email change.";
        }
        else {
            TempData["StatusMessage"] = "Profile changes saved.";
        }

        await signInManager.RefreshSignInAsync(user);
        return RedirectToAction(nameof(Profile));
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmailChange(
        string userId,
        string email,
        string code)
    {
        if (string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(code)) {
            return BadRequest();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound();

        try {
            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        }
        catch (FormatException) {
            return BadRequest();
        }

        var result = await userManager.ChangeEmailAsync(user, email, code);
        if (!result.Succeeded) {
            TempData["StatusMessage"] = "We couldn't confirm that email change. Please request it again.";
            return RedirectToAction(nameof(Profile));
        }

        // Keep the username used for email sign-in aligned with the confirmed email, if that's your policy.
        if (string.Equals(user.UserName, user.Email, StringComparison.OrdinalIgnoreCase)) {
            var usernameResult = await userManager.SetUserNameAsync(user, email);
            if (!usernameResult.Succeeded)
                AddIdentityErrors(usernameResult);
        }

        await signInManager.RefreshSignInAsync(user);
        TempData["StatusMessage"] = "Your email address has been confirmed.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction(nameof(Index), "Home");

        return View(new LoginViewModel());
    }

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
            ModelState.AddModelError(string.Empty, "Invalid username or password. Please try again");
            return View(loginModel);
        }

        var signInResult = await signInManager.PasswordSignInAsync(user.UserName!, loginModel.Password, loginModel.RememberMe, lockoutOnFailure: true);

        if (signInResult.Succeeded)
            return RedirectToAction("Index", "Project");


        // General invalid login
        ModelState.AddModelError(string.Empty,
            signInResult.IsLockedOut ? "Account locked. Try again later." : "Invalid username or password. Please try again");
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
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction(nameof(Index), "Home");

        return View();
    }

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
            CreatedAtUtc = DateTime.UtcNow,
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
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        return View(nameof(CheckEmail), model.Email);
    }

    [HttpGet]
    public IActionResult CheckEmail(string email) => View(email);

    [HttpGet]
    public IActionResult ResetPassword() => View();

    [HttpGet]
    public IActionResult AccessDenied() => View();
}