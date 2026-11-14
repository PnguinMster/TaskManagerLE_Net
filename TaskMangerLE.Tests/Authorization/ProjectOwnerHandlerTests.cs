using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TaskMangerLE.Authorization;
using TaskMangerLE.Models;

namespace TaskMangerLE.Tests.Authorization;

public class ProjectOwnerHandlerTests
{
	private const string Alice = "alice";
	private const string Bob = "bob";

	[Fact]
	public async Task HandleAsync_UserIsOwner_Succeeds()
	{
		// Arrange
		ClaimsPrincipal alice = CreateUser(Alice);
		var project = new ProjectItem { OwnerId = Alice };

		// Act
		bool authorized = await IsAuthorizedAsync(alice, project);

		// Assert
		Assert.True(authorized);
	}

	[Fact]
	public async Task HandleAsync_UserIsNotOwner_DoesNotSucceed()
	{
		// Arrange
		ClaimsPrincipal bob = CreateUser(Bob);
		var project = new ProjectItem { OwnerId = Alice };

		// Act
		bool authorized = await IsAuthorizedAsync(bob, project);

		// Assert
		Assert.False(authorized);
	}

	[Fact]
	public async Task HandleAsync_UserHasNoIdClaim_DoesNotSucceed()
	{
		// Arrange
		ClaimsPrincipal noId = new(new ClaimsIdentity());
		var project = new ProjectItem { OwnerId = Alice };

		// Act
		bool authorized = await IsAuthorizedAsync(noId, project);

		// Assert
		Assert.False(authorized);
	}

	[Fact]
	public async Task HandleAsync_ProjectHasNoOwner_DoesNotSucceed()
	{
		// Arrange
		ClaimsPrincipal alice = CreateUser(Alice);
		var project = new ProjectItem();

		// Act
		bool authorized = await IsAuthorizedAsync(alice, project);

		// Assert
		Assert.False(authorized);
	}

	[Theory]
	[InlineData(Roles.Admin, true)]
	[InlineData(Roles.User, false)]
	public async Task HandleAsync_NonOwnerWithRole_SucceedsOnlyForAdmin(string role, bool expected)
	{
		// Arrange
		ClaimsPrincipal bob = CreateUser(Bob, role);
		var project = new ProjectItem { OwnerId = Alice };

		// Act
		bool authorized = await IsAuthorizedAsync(bob, project);

		// Assert
		Assert.Equal(expected, authorized);
	}

	// Asks the handler: may this user access this project?
	private static async Task<bool> IsAuthorizedAsync(ClaimsPrincipal user, ProjectItem project)
	{
		var requirement = new ProjectOwnerRequirement();
		var context = new AuthorizationHandlerContext([requirement], user, project);

		await new ProjectOwnerHandler().HandleAsync(context);

		return context.HasSucceeded;
	}

	// A signed-in user with an id and optional roles.
	private static ClaimsPrincipal CreateUser(string userId, params string[] roles)
	{
		List<Claim> claims = [new Claim(ClaimTypes.NameIdentifier, userId)];
		foreach (string role in roles)
			claims.Add(new Claim(ClaimTypes.Role, role));

		// The "Test" authentication type marks the identity as signed in.
		return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
	}
}