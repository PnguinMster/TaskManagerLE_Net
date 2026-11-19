using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore.Query.Internal;
using TaskManagerLE.Authorization;
using TaskManagerLE.Models;
using TaskManagerLE.Models.Enums;

namespace TaskManagerLE.Controllers;

public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    TimeProvider timeProvider) : Controller
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

        var userNameChanged = !string.Equals(user.UserName, newUserName, StringComparison.Ordinal);
        var emailChanged = !string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase);

        // Check the email is free before changing anything, so a rejected email doesn't leave
        // the username half-saved. (Identity would only catch this later, when the link is clicked.)
        if (emailChanged) {
            var emailOwner = await userManager.FindByEmailAsync(newEmail);
            if (emailOwner is not null && emailOwner.Id != user.Id) {
                ModelState.AddModelError(nameof(model.NewEmail), "That email address is already in use.");
                model.UserName = user.UserName ?? string.Empty;
                model.Email = user.Email ?? string.Empty;
                return View(model);
            }
        }

        if (userNameChanged) {
            var result = await userManager.SetUserNameAsync(user, newUserName);
            if (!result.Succeeded) {
                AddIdentityErrors(result);
                model.UserName = user.UserName ?? string.Empty;
                model.Email = user.Email ?? string.Empty;
                return View(model);
            }
        }

        if (emailChanged) {
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
                $"Confirm your new email address by <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>clicking here</a>.");

            if (userNameChanged)
                this.ShowToast(new ToastMessage(ToastKind.Success, "Username saved"));

            // The email stays the old one until the link is opened, so say which address is waiting.
            this.ShowToast(new ToastMessage(ToastKind.Info, "Confirmation link sent",
                $"We sent a link to {newEmail}. Your email changes once you open it."));
        }
        else {
            this.ShowToast(new ToastMessage(ToastKind.Success, "Profile saved"));
        }

        await signInManager.RefreshSignInAsync(user);
        return RedirectToAction(nameof(Profile));
    }

    // The password card's fields are named "ChangePassword.NewPassword" etc. (it is nested inside
    // ProfileViewModel on the page). Bind(Prefix) tells the model binder to read them under that name.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        [Bind(Prefix = nameof(ProfileViewModel.ChangePassword))] ChangePasswordViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Challenge();

        // Same idea as Register/ResetPassword: check the rules before the IsValid test so every
        // problem shows together instead of one hiding the other.
        if (!string.IsNullOrEmpty(model.NewPassword)) {
            foreach (var validator in userManager.PasswordValidators)
                AddChangePasswordErrors(await validator.ValidateAsync(userManager, user, model.NewPassword));
        }

        if (!string.IsNullOrEmpty(model.CurrentPassword)
            && !await userManager.CheckPasswordAsync(user, model.CurrentPassword)) {
            AddChangePasswordError(nameof(ChangePasswordViewModel.CurrentPassword), "The current password is incorrect.");
        }

        if (!ModelState.IsValid)
            return ChangePasswordFailed(user, model);

        var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded) {
            AddChangePasswordErrors(result);
            return ChangePasswordFailed(user, model);
        }

        // Changing the password rotates the security stamp; refreshing keeps this user signed in.
        await signInManager.RefreshSignInAsync(user);
        this.ShowToast(new ToastMessage(ToastKind.Success, "Password changed"));
        return RedirectToAction(nameof(Profile));
    }

    // Re-show the Profile page with the errors. The password fields are never sent back to the browser.
    private ViewResult ChangePasswordFailed(ApplicationUser user, ChangePasswordViewModel model)
    {
        return View(nameof(Profile), new ProfileViewModel
        {
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            NewUserName = user.UserName ?? string.Empty,
            NewEmail = user.Email ?? string.Empty,
            MemberSinceUtc = user.CreatedAtUtc,
            ChangePassword = model
        });
    }

    // Attach each Identity error to the field the user can fix, using the form's prefixed field key.
    private void AddChangePasswordErrors(IdentityResult result)
    {
        foreach (var error in result.Errors) {
            var field = error.Code switch
            {
                nameof(IdentityErrorDescriber.PasswordMismatch) => nameof(ChangePasswordViewModel.CurrentPassword),
                var code when code.StartsWith("Password") => nameof(ChangePasswordViewModel.NewPassword),
                _ => string.Empty // unknown errors go to the form-level alert at the top of the card
            };
            AddChangePasswordError(field, error.Description);
        }
    }

    // Form-level errors (no field) use the key "ChangePassword", not string.Empty: the details card on the
    // same page shows string.Empty errors, and a password error must only show on the password card.
    private void AddChangePasswordError(string field, string message)
    {
        var key = field == string.Empty
            ? nameof(ProfileViewModel.ChangePassword)
            : $"{nameof(ProfileViewModel.ChangePassword)}.{field}";
        ModelState.AddModelError(key, message);
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
            this.ShowToast(new ToastMessage(ToastKind.Error, "Email not changed",
                "We couldn't confirm that email change. Please request it again."));
            return RedirectToAction(nameof(Profile));
        }

        // ChangeEmailAsync rotates the security stamp, which would sign this user out. Refresh only if
        // the person opening the link is that same signed-in user, so the link can't sign anyone in.
        if (userManager.GetUserId(User) == user.Id)
            await signInManager.RefreshSignInAsync(user);

        this.ShowToast(new ToastMessage(ToastKind.Success, "Email confirmed", $"Your email is now {email}."));
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
        // Trim so a pasted value with stray spaces still matches.
        var usernameOrEmail = loginModel.UsernameOrEmail.Trim();

        ApplicationUser? user = usernameOrEmail.Contains('@')
            ? await userManager.FindByEmailAsync(usernameOrEmail)
            : await userManager.FindByNameAsync(usernameOrEmail);

        if (user is null)
            return LoginFailed(loginModel, "Invalid username or password. Please try again.");

        var signInResult = await signInManager.PasswordSignInAsync(user.UserName!, loginModel.Password, loginModel.RememberMe, lockoutOnFailure: true);

        if (signInResult.Succeeded)
            return RedirectToAction(nameof(Index), "Home");


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
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
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

        return this.RedirectToLocal(returnUrl, RedirectToAction(nameof(Index), "Home"));
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await userManager.FindByEmailAsync(model.Email.Trim());
        if (user is not null) {
            var code = await userManager.GeneratePasswordResetTokenAsync(user);
            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Action(
                nameof(ResetPassword),
                "Account",
                new { email = user.Email, code = encodedCode },
                protocol: Request.Scheme);

            await emailSender.SendEmailAsync(
                user.Email!,
                "Reset your password",
                $"Reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>clicking here</a>.");
        }

        // Same page whether or not the email exists, so nobody can use this form to find out who has an account.
        return View(nameof(CheckEmail), model.Email);
    }

    [HttpGet]
    public IActionResult CheckEmail(string email) => View(email);

    [HttpGet]
    public IActionResult ResetPassword(string? email, string? code)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            return BadRequest();

        try {
            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        }
        catch (FormatException) {
            return BadRequest();
        }

        // The query string was already bound into ModelState (with the still-encoded code), and tag helpers
        // like asp-for prefer ModelState over the model. Clear it so the hidden field gets the decoded token.
        ModelState.Clear();

        return View(new ResetPasswordViewModel { Email = email, Code = code });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        var user = string.IsNullOrEmpty(model.Email) ? null : await userManager.FindByEmailAsync(model.Email);

        // Same idea as Register: check the password rules before the IsValid test, so a rule error
        // and a "doesn't match" error show together instead of one hiding the other.
        if (user is not null && !string.IsNullOrEmpty(model.Password)) {
            foreach (var validator in userManager.PasswordValidators)
                AddRegisterErrors(await validator.ValidateAsync(userManager, user, model.Password));
        }

        if (!ModelState.IsValid)
            return View(model);

        if (user is null)
            return ResetPasswordDone(); // don't reveal that the email doesn't exist

        var result = await userManager.ResetPasswordAsync(user, model.Code, model.Password);
        if (result.Succeeded)
            return ResetPasswordDone();

        // An old or already-used link gives the same code either way; one friendly message covers both.
        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
            ModelState.AddModelError(string.Empty, "This reset link is invalid or has expired. Please request a new one.");
        else
            AddRegisterErrors(result); // password-rule errors go under the Password field, like on Register

        return View(model);
    }

    private RedirectToActionResult ResetPasswordDone()
    {
        this.ShowToast(new ToastMessage(ToastKind.Success, "Password reset", "You can log in now."));
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied(string? returnUrl = null)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;

        // The cookie handler redirects here with ?ReturnUrl=<the page that was blocked>.
        ViewData["BlockedPath"] = returnUrl;
        return View();
    }
}