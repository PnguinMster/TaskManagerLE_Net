using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TaskManagerLE.Models;

namespace TaskManagerLE.Authorization;

public class ProjectOwnerRequirement : IAuthorizationRequirement
{
}

public class ProjectOwnerHandler : AuthorizationHandler<ProjectOwnerRequirement, ProjectItem>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ProjectOwnerRequirement requirement, ProjectItem resource)
    {
        // Read straight from the claims (the same claim UserManager.GetUserId uses by default), so the
        // handler needs no services and unit tests can create it with new ProjectOwnerHandler().
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (context.User.IsInRole(Roles.Admin) || resource.OwnerId == userId)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
