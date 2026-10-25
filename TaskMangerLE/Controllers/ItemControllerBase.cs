using Microsoft.AspNetCore.Mvc;
using TaskMangerLE.Authorization;
using TaskMangerLE.Models;
using Microsoft.AspNetCore.Authorization;

namespace TaskMangerLE.Controllers;

public abstract class ItemControllerBase(IAuthorizationService authorizationService) : Controller
{
    protected async Task<T?> LoadAuthorizedAsync<T>(Task<T?> loadItem, Func<T, object>? getResource = null) where T : ItemBase
    {
        T? item = await loadItem;
        if (item == null)
            return null;

        object resource = getResource?.Invoke(item) ?? item;

        var authResult = await authorizationService.AuthorizeAsync(User, resource, Policies.ProjectOwner);
        return authResult.Succeeded ? item : null;
    }
}