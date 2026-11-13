using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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

        if (user is null)
            return LoginFailed(loginModel, "Invalid username or password. Please try again.");

        var signInResult = await signInManager.PasswordSignInAsync(user.UserName!, loginModel.Password, loginModel.RememberMe, lockoutOnFailure: true);

        if (signInResult.Succeeded)
            return RedirectToAction("Index", "Project");


        // General invalid login
        var errorMessage = signInResult.IsLockedOut
            ? "Account locked. Try again later."
            : "Invalid username or password. Please try again.";
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

        return LoginFailed(loginModel, errorMessage);
    }

    // string.Empty = an error for the whole form; Login.cshtml shows it above the inputs.
    private ViewResult LoginFailed(LoginViewModel loginModel, string message)
    {
        ModelState.AddModelError(string.Empty, message);
        return View(nameof(Login), loginModel);
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
        // User
        ApplicationUser user = new ApplicationUser
        {
            UserName = registerModel.UserName,
            Email = registerModel.Email,
            CreatedAtUtc = DateTime.UtcNow,
        };

        // Run Identity's checks before the IsValid test so every problem shows at once,
        // not just the ones the form's own attributes caught.
        await AddIdentityValidationErrorsAsync(user, registerModel.Password);

        if (!ModelState.IsValid)
            return View(registerModel);

        // CreateAsync validates again, which still covers two people picking the same name at once.
        var registerResult = await userManager.CreateAsync(user, registerModel.Password);

        if (!registerResult.Succeeded) {
            AddRegisterErrors(registerResult);
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

    // UserManager exposes the same validators CreateAsync uses, so we can run them without saving anything.
    private async Task AddIdentityValidationErrorsAsync(ApplicationUser user, string password)
    {
        foreach (var validator in userManager.UserValidators)
            AddRegisterErrors(await validator.ValidateAsync(userManager, user));

        // An empty password is already reported by [Required]; don't pile rule errors on top.
        if (!string.IsNullOrEmpty(password))
        {
            foreach (var validator in userManager.PasswordValidators)
                AddRegisterErrors(await validator.ValidateAsync(userManager, user, password));
        }
    }

    // Identity reports problems as error codes (e.g. "PasswordTooShort"). Attaching each one to the
    // matching field key lets the asp-validation-for span under that field display it.
    private void AddRegisterErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            var field = error.Code switch
            {
                var code when code.StartsWith("Password") => nameof(RegisterViewModel.Password),
                "DuplicateUserName" or "InvalidUserName" => nameof(RegisterViewModel.UserName),
                "DuplicateEmail" or "InvalidEmail" => nameof(RegisterViewModel.Email),
                _ => string.Empty // unknown errors fall back to the summary at the top
            };

            // The form's attributes already reported this field (e.g. blank or bad format);
            // skip Identity's duplicate of that. Password is exempt: it can have several rule errors.
            if (field != nameof(RegisterViewModel.Password) && field != string.Empty
                && ModelState.GetFieldValidationState(field) == ModelValidationState.Invalid)
                continue;

            ModelState.AddModelError(field, error.Description);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(string? returnUrl = null)
    {
        await signInManager.SignOutAsync();

        // Only follow local URLs, so a crafted link can't bounce users to another site (open redirect).
        if (Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

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
    public IActionResult AccessDenied(string? returnUrl = null)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;

        // The cookie handler redirects here with ?ReturnUrl=<the page that was blocked>.
        ViewData["BlockedPath"] = returnUrl;
        return View();
    }
}