using Microsoft.AspNetCore.Mvc;

namespace TaskManagerLE.Controllers;

public static class RedirectExtensions
{
    // Goes back to returnUrl (e.g. the page a modal or toast was on), or to fallback when there is none.
    // Only local URLs are followed, so a crafted link can't send users to another site (open redirect).
    public static IActionResult RedirectToLocal(this Controller controller, string? returnUrl, IActionResult fallback) =>
        controller.Url.IsLocalUrl(returnUrl)
            ? controller.LocalRedirect(returnUrl)
            : fallback;

    // Same rule, but returns the URL itself, for when it's needed twice (e.g. redirect + a toast's Undo link).
    public static string LocalUrlOr(this Controller controller, string? returnUrl, string fallbackUrl) =>
        controller.Url.IsLocalUrl(returnUrl) ? returnUrl : fallbackUrl;
}
