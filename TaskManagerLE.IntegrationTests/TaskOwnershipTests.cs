using System.Net;

namespace TaskManagerLE.IntegrationTests;

// Tasks have no owner of their own: TaskController authorizes them through their parent project.
// All tests in this class share one app and database, so each test creates its own users and data.
public class TaskOwnershipTests(TestAppFactory factory) : IClassFixture<TestAppFactory>
{
    [Fact]
    public async Task Delete_OwnTask_RedirectsAndMovesTaskToTrash()
    {
        // Arrange
        string aliceId = await factory.CreateUserAsync("alice");
        int projectId = await factory.CreateProjectAsync(aliceId);
        int taskId = await factory.CreateTaskAsync(projectId);
        HttpClient alice = factory.CreateClientFor(aliceId);

        // Act
        HttpResponseMessage response = await alice.PostFormAsync("/Task/Delete",
            new() { ["taskId"] = taskId.ToString() });

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True((await factory.FindTaskAsync(taskId)).IsDeleted);
    }

    [Fact]
    public async Task Delete_TaskInOtherUsersProject_ReturnsNotFoundAndTaskIsUnchanged()
    {
        // Arrange
        string aliceId = await factory.CreateUserAsync("alice");
        string bobId = await factory.CreateUserAsync("bob");
        int aliceProjectId = await factory.CreateProjectAsync(aliceId);
        int aliceTaskId = await factory.CreateTaskAsync(aliceProjectId);
        HttpClient bob = factory.CreateClientFor(bobId);

        // Act
        HttpResponseMessage response = await bob.PostFormAsync("/Task/Delete",
            new() { ["taskId"] = aliceTaskId.ToString() });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False((await factory.FindTaskAsync(aliceTaskId)).IsDeleted);
    }

    [Fact]
    public async Task Update_MoveOwnTaskToOwnOtherProject_ReturnsOkAndMovesTask()
    {
        // Arrange
        string bobId = await factory.CreateUserAsync("bob");
        int bobFirstProjectId = await factory.CreateProjectAsync(bobId);
        int bobSecondProjectId = await factory.CreateProjectAsync(bobId);
        int bobTaskId = await factory.CreateTaskAsync(bobFirstProjectId);
        HttpClient bob = factory.CreateClientFor(bobId);

        // Act
        HttpResponseMessage response = await bob.PostFormAsync("/Task/Update",
            UpdateForm(bobTaskId, bobSecondProjectId));

        // Assert: proves the form is valid, so the NotFound in the next test comes from the ownership check.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(bobSecondProjectId, (await factory.FindTaskAsync(bobTaskId)).ProjectId);
    }

    [Fact]
    public async Task Update_MoveOwnTaskToOtherUsersProject_ReturnsNotFoundAndTaskStaysPut()
    {
        // Arrange: Bob owns the task, so the first check passes. Only the target project check can stop him.
        string aliceId = await factory.CreateUserAsync("alice");
        string bobId = await factory.CreateUserAsync("bob");
        int aliceProjectId = await factory.CreateProjectAsync(aliceId);
        int bobProjectId = await factory.CreateProjectAsync(bobId);
        int bobTaskId = await factory.CreateTaskAsync(bobProjectId);
        HttpClient bob = factory.CreateClientFor(bobId);

        // Act
        HttpResponseMessage response = await bob.PostFormAsync("/Task/Update",
            UpdateForm(bobTaskId, aliceProjectId));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(bobProjectId, (await factory.FindTaskAsync(bobTaskId)).ProjectId);
    }

    // The fields TaskItemUpdateViewModel needs to pass validation. Priority and Status are left out,
    // so they bind to their defaults (Low, ToDo).
    private static Dictionary<string, string> UpdateForm(int taskId, int projectId) => new()
    {
        ["Id"] = taskId.ToString(),
        ["ProjectId"] = projectId.ToString(),
        ["Name"] = "Updated task",
        ["DueDate"] = "2026-03-01",
    };
}
