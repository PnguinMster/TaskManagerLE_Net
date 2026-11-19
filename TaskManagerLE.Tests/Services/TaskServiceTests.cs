using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using TaskManagerLE.Data;
using TaskManagerLE.Models;
using TaskManagerLE.Services;

namespace TaskManagerLE.Tests.Services;

public sealed class TaskServiceTests : IDisposable
{
    private const string Alice = "alice";

    // The "current time" every test sees. Set to a fixed moment so we can check exact values.
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _time = new(Now);
    private readonly TestDatabase _db;

    public TaskServiceTests()
    {
        _db = new TestDatabase(_time);
        using ApplicationDbContext context = _db.CreateContext();
        context.Users.Add(new ApplicationUser { Id = Alice, UserName = Alice });
        context.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task DeleteTaskAsync_ActiveTask_SetsDeletedFieldsToCurrentTime()
    {
        // Arrange
        TaskItem created = await AddTaskAsync("Write tests");
        using ApplicationDbContext context = _db.CreateContext();
        var service = new TaskService(context, _time);
        TaskItem task = (await service.GetTaskWithProjectAsync(created.Id))!;

        // Act
        await service.DeleteTaskAsync(task, Alice);

        // Assert: read it back from a fresh context, ignoring the soft-delete filter.
        using ApplicationDbContext check = _db.CreateContext();
        TaskItem saved = await check.TaskItems.IgnoreQueryFilters().SingleAsync(t => t.Id == created.Id);
        Assert.True(saved.IsDeleted);
        Assert.Equal(Alice, saved.DeletedByUserId);
        Assert.Equal(Now.UtcDateTime, saved.DeletedAtUtc);
    }

    private async Task<TaskItem> AddTaskAsync(string name)
    {
        using ApplicationDbContext context = _db.CreateContext();
        var project = new ProjectItem { Name = "Project", OwnerId = Alice };
        var task = new TaskItem { Name = name, Project = project };
        context.TaskItems.Add(task);
        await context.SaveChangesAsync();
        return task;
    }
}
