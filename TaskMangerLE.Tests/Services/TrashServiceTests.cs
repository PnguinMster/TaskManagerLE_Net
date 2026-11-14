using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using TaskMangerLE.Data;
using TaskMangerLE.Models;
using TaskMangerLE.Services;

namespace TaskMangerLE.Tests.Services;

// Service tests for TrashService against a real (SQLite, in-memory) database.
// xUnit creates a new instance of this class for every test, so each test
// gets its own empty database from the constructor and loses it in Dispose.
public sealed class TrashServiceTests : IDisposable
{
    private const string Alice = "alice";
    private const string Bob = "bob";
    private static readonly DateTime Jan1 = new(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Jan2 = new(2026, 1, 2, 9, 0, 0, DateTimeKind.Utc);

    private readonly TestDatabase _db = new();
    private readonly FakeTimeProvider _time = new();

    public TrashServiceTests()
    {
        // Projects need a real owner row: SQLite enforces the foreign key to AspNetUsers.
        using ApplicationDbContext context = _db.CreateContext();
        context.Users.AddRange(
            new ApplicationUser { Id = Alice, UserName = Alice },
            new ApplicationUser { Id = Bob, UserName = Bob });
        context.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    // ---------- GetSummaryAsync ----------

    [Fact]
    public async Task GetSummaryAsync_RegularUser_ReturnsOwnDeletedItemsNewestFirst()
    {
        // Arrange
        await AddProjectAsync(Alice, "Active");
        await AddProjectAsync(Alice, "Old", deletedAt: Jan1);
        ProjectItem kept = await AddProjectAsync(Alice, "Kept");
        await AddTaskAsync(kept, "Gone", deletedAt: Jan2);
        await AddProjectAsync(Bob, "Bob's", deletedAt: Jan1);

        // Act
        TrashSummaryViewModel summary = await CreateService().GetSummaryAsync(Alice, isAdmin: false);

        // Assert: Alice's two deleted items, Bob's is not shown, newest deletion first.
        Assert.Equal(1, summary.ProjectCount);
        Assert.Equal(1, summary.TaskCount);
        Assert.Equal(["Gone", "Old"], summary.Items.Select(i => i.Name));
    }

    [Fact]
    public async Task GetSummaryAsync_TaskInsideDeletedProject_IsCountedButListedUnderItsProject()
    {
        // Arrange: the task itself is not deleted, only its project is.
        ProjectItem old = await AddProjectAsync(Alice, "Old", deletedAt: Jan1);
        await AddTaskAsync(old, "Inside");

        // Act
        TrashSummaryViewModel summary = await CreateService().GetSummaryAsync(Alice, isAdmin: false);

        // Assert: it counts toward what Empty trash would delete,
        // but only the project gets its own row in the list.
        Assert.Equal(1, summary.TaskCount);
        TrashItemViewModel item = Assert.Single(summary.Items);
        Assert.True(item.IsProject);
        Assert.Equal(1, item.TaskCount);
    }

    [Fact]
    public async Task GetSummaryAsync_Admin_SeesEveryonesDeletedItems()
    {
        // Arrange
        await AddProjectAsync(Alice, "Alice's", deletedAt: Jan1);
        await AddProjectAsync(Bob, "Bob's", deletedAt: Jan2);

        // Act
        TrashSummaryViewModel summary = await CreateService().GetSummaryAsync(Alice, isAdmin: true);

        // Assert
        Assert.Equal(2, summary.ProjectCount);
    }

    // ---------- EmptyTrashAsync ----------

    [Fact]
    public async Task EmptyTrashAsync_MixedItems_RemovesOnlyUsersTrash()
    {
        // Arrange
        ProjectItem active = await AddProjectAsync(Alice, "Active");
        await AddTaskAsync(active, "Gone", deletedAt: Jan1);
        ProjectItem old = await AddProjectAsync(Alice, "Old", deletedAt: Jan1);
        await AddTaskAsync(old, "Inside");
        await AddProjectAsync(Bob, "Bob's", deletedAt: Jan1);

        // Act
        await CreateService().EmptyTrashAsync(Alice, isAdmin: false);

        // Assert: read with IgnoreQueryFilters so deleted rows would show up if they were still there.
        using ApplicationDbContext check = _db.CreateContext();
        List<string> projectNames = await check.ProjectItems.IgnoreQueryFilters()
            .OrderBy(p => p.Name).Select(p => p.Name).ToListAsync();
        bool anyTasksLeft = await check.TaskItems.IgnoreQueryFilters().AnyAsync();

        Assert.Equal(["Active", "Bob's"], projectNames);
        Assert.False(anyTasksLeft);
    }

    // ---------- RestoreAllAsync ----------

    [Fact]
    public async Task RestoreAllAsync_ItemsInTrash_RestoresAllAndReturnsThem()
    {
        // Arrange
        ProjectItem old = await AddProjectAsync(Alice, "Old", deletedAt: Jan1);
        await AddTaskAsync(old, "Inside");
        ProjectItem active = await AddProjectAsync(Alice, "Active");
        TaskItem gone = await AddTaskAsync(active, "Gone", deletedAt: Jan2);

        // Act
        RestoreAllResult result = await CreateService().RestoreAllAsync(Alice, isAdmin: false);

        // Assert: the snapshots hold the original deletion info (needed for Undo).
        TrashedItemSnapshot projectSnapshot = Assert.Single(result.Projects);
        Assert.Equal(old.Id, projectSnapshot.Id);
        Assert.Equal(Jan1, projectSnapshot.DeletedAtUtc);
        Assert.Equal(Alice, projectSnapshot.DeletedByUserId);

        // "Inside" was never deleted itself, so only "Gone" is in the task snapshots.
        TrashedItemSnapshot taskSnapshot = Assert.Single(result.Tasks);
        Assert.Equal(gone.Id, taskSnapshot.Id);

        // Everything is visible again through the normal query filters.
        using ApplicationDbContext check = _db.CreateContext();
        Assert.Equal(2, await check.ProjectItems.CountAsync());
        Assert.Equal(2, await check.TaskItems.CountAsync());
    }

    // ---------- UndoRestoreAllAsync ----------

    [Fact]
    public async Task UndoRestoreAllAsync_AfterRestoreAll_RestoresOriginalDeletionInfo()
    {
        // Arrange: delete, then restore (each step in its own context, like separate requests).
        ProjectItem old = await AddProjectAsync(Alice, "Old", deletedAt: Jan1);
        RestoreAllResult restored = await CreateService().RestoreAllAsync(Alice, isAdmin: false);

        // Act
        (int projects, int tasks) = await CreateService().UndoRestoreAllAsync(restored, Alice, isAdmin: false);

        // Assert
        Assert.Equal(1, projects);
        Assert.Equal(0, tasks);

        using ApplicationDbContext check = _db.CreateContext();
        ProjectItem reloaded = await check.ProjectItems.IgnoreQueryFilters().SingleAsync(p => p.Id == old.Id);
        Assert.True(reloaded.IsDeleted);
        Assert.Equal(Jan1, reloaded.DeletedAtUtc);
        Assert.Equal(Alice, reloaded.DeletedByUserId);
    }

    // ---------- Helpers ----------

    // A new context per call, so every service call works like a fresh web request.
    private TrashService CreateService() => new(_db.CreateContext(), _time);

    private async Task<ProjectItem> AddProjectAsync(string ownerId, string name, DateTime? deletedAt = null)
    {
        var project = new ProjectItem { Name = name, OwnerId = ownerId };
        if (deletedAt is not null)
            project.SoftDelete(ownerId, deletedAt.Value);

        using ApplicationDbContext context = _db.CreateContext();
        context.ProjectItems.Add(project);
        await context.SaveChangesAsync();
        return project;
    }

    private async Task<TaskItem> AddTaskAsync(ProjectItem project, string name, DateTime? deletedAt = null)
    {
        var task = new TaskItem { Name = name, ProjectId = project.Id };
        if (deletedAt is not null)
            task.SoftDelete(project.OwnerId, deletedAt.Value);

        using ApplicationDbContext context = _db.CreateContext();
        context.TaskItems.Add(task);
        await context.SaveChangesAsync();
        return task;
    }
}
