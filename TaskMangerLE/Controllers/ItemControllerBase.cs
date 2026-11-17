using Microsoft.AspNetCore.Mvc;
using TaskMangerLE.Authorization;
using Microsoft.AspNetCore.Authorization;
using TaskMangerLE.Models;
using TaskMangerLE.Models.Enums;

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

    // Edit forms live in modals, so there's no page to re-show with the errors. Call this before
    // redirecting back: the toast says why nothing was saved. Rarely seen, because the modals'
    // required/maxlength checks stop most bad input in the browser first.
    protected void ShowValidationErrorToast()
    {
        string? firstError = ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(message => !string.IsNullOrEmpty(message));

        this.ShowToast(new ToastMessage(ToastKind.Error, "Changes not saved", firstError));
    }
}