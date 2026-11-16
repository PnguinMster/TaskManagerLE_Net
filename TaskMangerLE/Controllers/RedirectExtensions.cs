using Microsoft.AspNetCore.Mvc;

namespace TaskMangerLE.Controllers;

public static class RedirectExtensions
{
    // Goes back to returnUrl (e.g. the page a modal or toast was on), or to fallback when there is none.
    // Only local URLs are followed, so a crafted link can't send users to another site (open redirect).
    public static IActionResult RedirectToLocal(this Controller controller, string? returnUrl, IActionResult fallback) =>
        controller.Url.IsLocalUrl(returnUrl)
            ? controller.LocalRedirect(returnUrl)
            : fallback;
}
