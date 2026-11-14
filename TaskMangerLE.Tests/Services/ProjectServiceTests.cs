using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using TaskMangerLE.Data;
using TaskMangerLE.Models;
using TaskMangerLE.Services;

namespace TaskMangerLE.Tests.Services;

public sealed class ProjectServiceTests : IDisposable
{
	private const string Alice = "alice";

	// The "current time" every test sees. Set to a fixed moment so we can check exact values.
	private static readonly DateTimeOffset Now = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

	private readonly TestDatabase _db = new();
	private readonly FakeTimeProvider _time = new(Now);

	public ProjectServiceTests()
	{
		using ApplicationDbContext context = _db.CreateContext();
		context.Users.Add(new ApplicationUser { Id = Alice, UserName = Alice });
		context.SaveChanges();
	}

	public void Dispose() => _db.Dispose();

	[Fact]
	public async Task DeleteProjectAsync_DeletesProjectAndItsTasks_WithTheSameTime()
	{
		// Arrange
		ProjectItem created = await AddProjectAsync("Write tests", "A", "B");
		using ApplicationDbContext context = _db.CreateContext();
		var service = new ProjectService(context, _time);
		ProjectItem project = (await service.GetProjectWithTasksAsync(created.Id))!;

		// Act
		await service.DeleteProjectAsync(project, Alice);

		// Assert: read it back from a fresh context, ignoring the soft-delete filter.
		using ApplicationDbContext check = _db.CreateContext();
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
	public async Task RestoreProjectAsync_RestoresOnlyTasksDeletedWithTheProject()
	{
		// Arrange
		ProjectItem created = await AddProjectAsync("Project", "A", "B");
		int taskAId = created.Tasks.Single(t => t.Name == "A").Id;
		int taskBId = created.Tasks.Single(t => t.Name == "B").Id;

		// Task A is deleted on its own first.
		using (ApplicationDbContext context = _db.CreateContext()) {
			var taskService = new TaskService(context, _time);
			TaskItem taskA = (await taskService.GetTaskWithProjectAsync(taskAId))!;
			await taskService.DeleteTaskAsync(taskA, Alice);
		}

		// An hour later the whole project is deleted, which takes B with it.
		_time.Advance(TimeSpan.FromHours(1));
		using (ApplicationDbContext context = _db.CreateContext()) {
			var projectService = new ProjectService(context, _time);
			ProjectItem project = (await projectService.GetProjectWithTasksAsync(created.Id))!;
			await projectService.DeleteProjectAsync(project, Alice);
		}


		// Act
		using (ApplicationDbContext context = _db.CreateContext()) {
			var projectService = new ProjectService(context, _time);
			ProjectItem deleted = (await projectService.GetDeletedProjectWithDeletedTasksAsync(created.Id))!;
			await projectService.RestoreProjectAsync(deleted);
		}

		// Assert
		using ApplicationDbContext check = _db.CreateContext();
		Assert.False((await check.ProjectItems.IgnoreQueryFilters().SingleAsync(p => p.Id == created.Id)).IsDeleted);
		Assert.True((await check.TaskItems.IgnoreQueryFilters().SingleAsync(t => t.Id == taskAId)).IsDeleted); // still in trash
		Assert.False((await check.TaskItems.IgnoreQueryFilters().SingleAsync(t => t.Id == taskBId)).IsDeleted); // came back
	}

	private async Task<ProjectItem> AddProjectAsync(string name, params string[] taskNames)
	{
		using ApplicationDbContext context = _db.CreateContext();
		var project = new ProjectItem { Name = name, OwnerId = Alice };
		foreach (string taskName in taskNames)
			project.Tasks.Add(new TaskItem { Name = taskName });


		context.ProjectItems.Add(project);
		await context.SaveChangesAsync();
		return project;
	}
}