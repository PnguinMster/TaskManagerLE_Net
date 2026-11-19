using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using TaskManagerLE.Data;
using TaskManagerLE.Models;
using TaskManagerLE.Services;
using TaskStatus = TaskManagerLE.Models.Enums.TaskStatus;

namespace TaskManagerLE.Tests.Services;

public sealed class ProjectServiceTests : IDisposable
{
    private const string Alice = "alice";

    // The "current time" every test sees. Set to a fixed moment so we can check exact values.
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _time = new(Now);
    private readonly TestDatabase _db;

    public ProjectServiceTests()
    {
        _db = new TestDatabase(_time);
        using ApplicationDbContext context = _db.CreateContext();
        context.Users.Add(new ApplicationUser { Id = Alice, UserName = Alice });
        context.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task DeleteProjectAsync_ProjectWithTasks_SoftDeletesAllWithSameTimestamp()
    {
        // Arrange
        ProjectItem created = await AddProjectAsync("Write tests", "A", "B");
        await using ApplicationDbContext context = _db.CreateContext();
        var service = new ProjectService(context, _time);
        ProjectItem project = (await service.GetProjectWithTasksAsync(created.Id))!;

        // Act
        await service.DeleteProjectAsync(project, Alice);

        // Assert: read it back from a fresh context, ignoring the soft-delete filter.
        await using ApplicationDbContext check = _db.CreateContext();
        ProjectItem saved = await check.ProjectItems.IgnoreQueryFilters().SingleAsync(p => p.Id == created.Id);
        Assert.True(saved.IsDeleted);
        Assert.Equal(Alice, saved.DeletedByUserId);
        Assert.Equal(Now.UtcDateTime, saved.DeletedAtUtc);

        List<TaskItem> tasks = await check.TaskItems.IgnoreQueryFilters()
            .Where(t => t.ProjectId == created.Id).ToListAsync();
        Assert.Equal(2, tasks.Count); // guards against "empty list passes"
        Assert.All(tasks, t => Assert.Equal(Now.UtcDateTime, t.DeletedAtUtc));
    }

    [Fact]
    public async Task RestoreProjectAsync_TaskDeletedEarlier_StaysInTrash()
    {
        // Arrange
        ProjectItem created = await AddProjectAsync("Project", "A", "B");
        int taskAId = created.Tasks.Single(t => t.Name == "A").Id;
        int taskBId = created.Tasks.Single(t => t.Name == "B").Id;

        // Task A is deleted on its own first.
        await using (ApplicationDbContext context = _db.CreateContext()) {
            var taskService = new TaskService(context, _time);
            TaskItem taskA = (await taskService.GetTaskWithProjectAsync(taskAId))!;
            await taskService.DeleteTaskAsync(taskA, Alice);
        }

        // An hour later the whole project is deleted, which takes B with it.
        _time.Advance(TimeSpan.FromHours(1));
        await using (ApplicationDbContext context = _db.CreateContext()) {
            var projectService = new ProjectService(context, _time);
            ProjectItem project = (await projectService.GetProjectWithTasksAsync(created.Id))!;
            await projectService.DeleteProjectAsync(project, Alice);
        }


        // Act
        await using (ApplicationDbContext context = _db.CreateContext()) {
            var projectService = new ProjectService(context, _time);
            ProjectItem deleted = (await projectService.GetDeletedProjectWithDeletedTasksAsync(created.Id))!;
            await projectService.RestoreProjectAsync(deleted);
        }

        // Assert
        await using ApplicationDbContext check = _db.CreateContext();
        Assert.False((await check.ProjectItems.IgnoreQueryFilters().SingleAsync(p => p.Id == created.Id)).IsDeleted);
        Assert.True((await check.TaskItems.IgnoreQueryFilters().SingleAsync(t => t.Id == taskAId)).IsDeleted); // still in trash
        Assert.False((await check.TaskItems.IgnoreQueryFilters().SingleAsync(t => t.Id == taskBId)).IsDeleted); // came back
    }

    [Theory]
    [InlineData(0, 0, 0, 0)] // no tasks → 0%, no divide-by-zero
    [InlineData(1, 2, 0, 33)] // 1 of 3 done → 33.3 rounds to 33
    [InlineData(2, 1, 0, 67)] // 2 of 3 done → 66.7 rounds to 67
    [InlineData(3, 0, 0, 100)] // all done
    [InlineData(1, 1, 2, 50)] // cancelled don't count → 1 of 2
    [InlineData(0, 0, 2, 0)] // only cancelled → treated as no tasks
    [InlineData(1, 7, 0, 13)] // 1 of 8 = 12.5% → rounds half up to 13
    public async Task GetProjectCardsAsync_MixedStatuses_ReturnsRoundedPercentExcludingCancelled(
        int done, int notDone, int cancelled, int expectedPercent)
    {
        // Arrange
        TaskStatus[] statuses =
        [
            .. Enumerable.Repeat(TaskStatus.Done, done),
            .. Enumerable.Repeat(TaskStatus.ToDo, notDone),
            .. Enumerable.Repeat(TaskStatus.Cancelled, cancelled)
        ];
        await AddProjectWithTasksAsync(statuses);

        // Act
        await using ApplicationDbContext context = _db.CreateContext();
        var service = new ProjectService(context, _time);
        List<ProjectCardViewModel> cards = await service.GetProjectCardsAsync(Alice, isAdmin: false);

        // Assert
        ProjectCardViewModel card = Assert.Single(cards);
        Assert.Equal(expectedPercent, card.ProgressPercent);
        Assert.Equal(done + notDone, card.TotalTaskCount);
    }

    private async Task<ProjectItem> AddProjectWithTasksAsync(params TaskStatus[] statuses)
    {
        await using ApplicationDbContext context = _db.CreateContext();
        var project = new ProjectItem { Name = "Project", OwnerId = Alice };
        foreach (TaskStatus status in statuses)
            project.Tasks.Add(new TaskItem { Name = status.ToString(), Status = status });

        context.ProjectItems.Add(project);
        await context.SaveChangesAsync();
        return project;
    }

    private async Task<ProjectItem> AddProjectAsync(string name, params string[] taskNames)
    {
        await using ApplicationDbContext context = _db.CreateContext();
        var project = new ProjectItem { Name = name, OwnerId = Alice };
        foreach (string taskName in taskNames)
            project.Tasks.Add(new TaskItem { Name = taskName });


        context.ProjectItems.Add(project);
        await context.SaveChangesAsync();
        return project;
    }
}
