using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagerLE.Authorization;
using TaskManagerLE.Models;

namespace TaskManagerLE.Controllers;

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

        AuthorizationResult authResult = await authorizationService.AuthorizeAsync(User, getProject(item), Policies.ProjectOwner);
        return authResult.Succeeded ? item : null;
    }

    // The create and edit modals are sent with fetch (see submitModalForm in site.js), so the modal stays open.
    // This answers with 400 and { errors: { "Name": ["..."] } }, which site.js shows under each field.
    protected IActionResult ValidationErrorsResult()
    {
        Dictionary<string, string[]> errors = ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors
                    .Select(error => error.ErrorMessage)
                    .ToArray());

        return BadRequest(new { errors });
    }
}
