using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TaskManagerLE.Data;
using TaskManagerLE.Models;

namespace TaskManagerLE.IntegrationTests;

// The whole request path for ownership: routing, login, controller, ProjectOwnerHandler and the database.
// All tests in this class share one app and database, so each test creates its own users and projects.
public class ProjectOwnershipTests(TestAppFactory factory) : IClassFixture<TestAppFactory>
{
    [Fact]
    public async Task Detail_OwnerRequestsOwnProject_ReturnsOk()
    {
        // Arrange
        string aliceId = await CreateUserAsync("alice");
        int projectId = await CreateProjectAsync(aliceId);
        HttpClient alice = factory.CreateClientFor(aliceId);

        // Act
        HttpResponseMessage response = await alice.GetAsync($"/Project/Detail?projectId={projectId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Detail_OtherUsersProject_ReturnsNotFound()
    {
        // Arrange
        string aliceId = await CreateUserAsync("alice");
        string bobId = await CreateUserAsync("bob");
        int aliceProjectId = await CreateProjectAsync(aliceId);
        HttpClient bob = factory.CreateClientFor(bobId);

        // Act
        HttpResponseMessage response = await bob.GetAsync($"/Project/Detail?projectId={aliceProjectId}");

        // Assert: not found, not forbidden, so Bob can't even tell the project exists.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Users are real rows because ProjectItem.OwnerId is a foreign key to the users table.
    // The Guid keeps names unique across tests sharing the database.
    private async Task<string> CreateUserAsync(string name)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        string userName = $"{name}_{Guid.NewGuid():N}";
        var user = new ApplicationUser { UserName = userName, Email = $"{userName}@example.com" };
        IdentityResult result = await userManager.CreateAsync(user);
        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(e => e.Description)));

        return user.Id;
    }

    private async Task<int> CreateProjectAsync(string ownerId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var project = new ProjectItem { Name = "Test project", OwnerId = ownerId };
        context.ProjectItems.Add(project);
        await context.SaveChangesAsync();

        return project.Id;
    }
}
