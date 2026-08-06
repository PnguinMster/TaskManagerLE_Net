using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TaskMangerLE.Models;

namespace TaskMangerLE.Services;

public class ProjectOwnerRequirement : IAuthorizationRequirement
{
}

public class ProjectOwnerHandler : AuthorizationHandler<ProjectOwnerRequirement, ProjectItem>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ProjectOwnerRequirement requirement, ProjectItem resource)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (context.User.IsInRole("Admin") || resource.OwnerId == userId)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}