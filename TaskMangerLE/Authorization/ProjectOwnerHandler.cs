using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TaskMangerLE.Models;

namespace TaskMangerLE.Authorization;

public class ProjectOwnerRequirement : IAuthorizationRequirement
{
}

public class ProjectOwnerHandler(UserManager<ApplicationUser> userManager) : AuthorizationHandler<ProjectOwnerRequirement, ProjectItem>
{
	protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ProjectOwnerRequirement requirement, ProjectItem resource)
	{
		var userId = userManager.GetUserId(context.User);

		if (context.User.IsInRole(Roles.Admin) || resource.OwnerId == userId)
			context.Succeed(requirement);

		return Task.CompletedTask;
	}
}