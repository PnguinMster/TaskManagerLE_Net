using Microsoft.AspNetCore.Mvc;
using TaskMangerLE.Authorization;
using Microsoft.AspNetCore.Authorization;
using TaskMangerLE.Models;

namespace TaskMangerLE.Controllers;

public abstract class ItemControllerBase(IAuthorizationService authorizationService) : Controller
{
    protected Task<ProjectItem?> LoadAuthorizedAsync(Task<ProjectItem?> loadItem) =>
        LoadAuthorizedAsync(loadItem, static project => project);

    protected async Task<T?> LoadAuthorizedAsync<T>(Task<T?> loadItem, Func<T, ProjectItem> getProject)
        where T : class
    {
        T? item = await loadItem;
        if (item is null)
            return null;

        var authResult = await authorizationService.AuthorizeAsync(User, getProject(item), Policies.ProjectOwner);
        return authResult.Succeeded ? item : null;
    }

    // The create and edit modals are sent with fetch (see submitModalForm in site.js), so the modal stays open.
    // This answers with 400 and { errors: { "Name": ["..."] } }, which site.js shows under each field.
    protected IActionResult ValidationErrorsResult()
    {
        var errors = ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors
                    .Select(error => error.ErrorMessage)
                    .ToArray());

        return BadRequest(new { errors });
    }
}