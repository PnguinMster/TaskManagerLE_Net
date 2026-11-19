using System.Net;

namespace TaskManagerLE.IntegrationTests;

// The whole request path for ownership: routing, login, controller, ProjectOwnerHandler and the database.
// All tests in this class share one app and database, so each test creates its own users and projects.
public class ProjectOwnershipTests(TestAppFactory factory) : IClassFixture<TestAppFactory>
{
    [Fact]
    public async Task Detail_OwnerRequestsOwnProject_ReturnsOk()
    {
        // Arrange
        string aliceId = await factory.CreateUserAsync("alice");
        int projectId = await factory.CreateProjectAsync(aliceId);
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
        string aliceId = await factory.CreateUserAsync("alice");
        string bobId = await factory.CreateUserAsync("bob");
        int aliceProjectId = await factory.CreateProjectAsync(aliceId);
        HttpClient bob = factory.CreateClientFor(bobId);

        // Act
        HttpResponseMessage response = await bob.GetAsync($"/Project/Detail?projectId={aliceProjectId}");

        // Assert: not found, not forbidden, so Bob can't even tell the project exists.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_OwnProject_RedirectsAndMovesProjectToTrash()
    {
        // Arrange
        string aliceId = await factory.CreateUserAsync("alice");
        int projectId = await factory.CreateProjectAsync(aliceId);
        HttpClient alice = factory.CreateClientFor(aliceId);

        // Act
        HttpResponseMessage response = await alice.PostFormAsync("/Project/Delete",
            new() { ["projectId"] = projectId.ToString() });

        // Assert: also proves the antiforgery helper works (a missing token gives 400 Bad Request).
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True((await factory.FindProjectAsync(projectId)).IsDeleted);
    }

    [Fact]
    public async Task Delete_OtherUsersProject_ReturnsNotFoundAndProjectIsUnchanged()
    {
        // Arrange
        string aliceId = await factory.CreateUserAsync("alice");
        string bobId = await factory.CreateUserAsync("bob");
        int aliceProjectId = await factory.CreateProjectAsync(aliceId);
        HttpClient bob = factory.CreateClientFor(bobId);

        // Act
        HttpResponseMessage response = await bob.PostFormAsync("/Project/Delete",
            new() { ["projectId"] = aliceProjectId.ToString() });

        // Assert: the status code alone isn't enough, so check the database too.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False((await factory.FindProjectAsync(aliceProjectId)).IsDeleted);
    }
}
