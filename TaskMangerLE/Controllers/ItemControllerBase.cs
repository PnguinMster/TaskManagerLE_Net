using Microsoft.AspNetCore.Mvc;
using TaskMangerLE.Authorization;
using TaskMangerLE.Models;
using Microsoft.AspNetCore.Authorization;

namespace TaskMangerLE.Controllers;

public abstract class ItemControllerBase(IAuthorizationService authorizationService) : Controller
{
    protected async Task<T?> LoadAuthorizedAsync<T>(Task<T?> loadItem, Func<T, object>? getResource = null) where T : class
    {
        T? item = await loadItem;
        if (item is null)
            return null;

        object resource = getResource?.Invoke(item) ?? item;

        var authResult = await authorizationService.AuthorizeAsync(User, resource, Policies.ProjectOwner);
        return authResult.Succeeded ? item : null;
    }
}