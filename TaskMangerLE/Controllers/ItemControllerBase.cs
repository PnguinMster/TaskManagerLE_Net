using Microsoft.AspNetCore.Mvc;
using TaskMangerLE.Authorization;
using Microsoft.AspNetCore.Authorization;
using TaskMangerLE.Models;

namespace TaskMangerLE.Controllers;

public abstract class ItemControllerBase(IAuthorizationService authorizationService) : Controller
{
    protected Task<ProjectItem?> LoadAuthorizedAsync(Task<ProjectItem?> loadItem) =>
    LoadAuthorizedAsync(loadItem, project => project);
    
    protected async Task<T?> LoadAuthorizedAsync<T>(Task<T?> loadItem, Func<T, ProjectItem> getProject) where T : class
    {
        T? item = await loadItem;
        if (item is null)
            return null;

        var authResult = await authorizationService.AuthorizeAsync(User, getProject(item), Policies.ProjectOwner);
        return authResult.Succeeded ? item : null;
    }
}