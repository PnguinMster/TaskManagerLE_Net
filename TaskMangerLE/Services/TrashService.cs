using Microsoft.EntityFrameworkCore;
using TaskMangerLE.Data;
using TaskMangerLE.Models;

namespace TaskMangerLE.Services;

public class TrashService(ApplicationDbContext context)
{
    public async Task<TrashSummaryViewModel> GetSummaryAsync(string userId, bool isAdmin) =>
        new()
        {
            ProjectCount = await GetTrashedProjectsQuery(userId, isAdmin).CountAsync(),
            TaskCount = await GetTrashedTasksQuery(userId, isAdmin).CountAsync(),
            Items = await GetItemsAsync(userId, isAdmin)
        };

    private async Task<List<TrashItemViewModel>> GetItemsAsync(string userId, bool isAdmin)
    {
        List<TrashItemViewModel> projects = await GetTrashedProjectsQuery(userId, isAdmin)
            .AsNoTracking()
            .Select(p => new TrashItemViewModel
            {
                IsProject = true,
                Id = p.Id,
                Name = p.Name,
                DeletedAtUtc = p.DeletedAtUtc,
                TaskCount = p.Tasks.Count
            })
            .ToListAsync();

        // Only tasks deleted on their own. Tasks of a deleted project come back with the project,
        // and restoring one alone would leave it hidden under a still-deleted project.
        IQueryable<TaskItem> ownTasks = context.TaskItems.IgnoreQueryFilters()
            .Where(t => t.IsDeleted && !t.Project.IsDeleted);
        if (!isAdmin)
            ownTasks = ownTasks.Where(t => t.Project.OwnerId == userId);

        List<TrashItemViewModel> tasks = await ownTasks
            .AsNoTracking()
            .Select(t => new TrashItemViewModel
            {
                IsProject = false,
                Id = t.Id,
                Name = t.Name,
                DeletedAtUtc = t.DeletedAtUtc,
                ProjectId = t.ProjectId,
                ProjectName = t.Project.Name
            })
            .ToListAsync();

        // Two small lists, merged in memory so projects and tasks sort together.
        return projects.Concat(tasks)
            .OrderByDescending(item => item.DeletedAtUtc)
            .ToList();
    }

    // Hard delete (purge) of everything in the user's trash. Only called from the Trash flow.
    public async Task EmptyTrashAsync(string userId, bool isAdmin)
    {
        // One transaction so we never end up with the tasks gone but their projects still there.
        await using var transaction = await context.Database.BeginTransactionAsync();

        // Tasks first: they reference their project.
        await GetTrashedTasksQuery(userId, isAdmin).ExecuteDeleteAsync();
        await GetTrashedProjectsQuery(userId, isAdmin).ExecuteDeleteAsync();

        await transaction.CommitAsync();
    }

    // Brings back everything in the user's trash. Returns how many items were restored.
    public async Task<int> RestoreAllAsync(string userId, bool isAdmin)
    {
        // Loaded and restored one by one (not ExecuteUpdateAsync) so SaveChangesAsync sets UpdatedAtUtc.
        List<ProjectItem> projects = await GetTrashedProjectsQuery(userId, isAdmin).ToListAsync();
        // Only tasks that are deleted themselves; tasks that were only hidden by their project
        // come back when the project does.
        List<TaskItem> tasks = await GetTrashedTasksQuery(userId, isAdmin)
            .Where(t => t.IsDeleted)
            .ToListAsync();

        foreach (ProjectItem project in projects)
            project.Restore();
        foreach (TaskItem task in tasks)
            task.Restore();

        // One SaveChangesAsync is one transaction: either everything comes back or nothing does.
        await context.SaveChangesAsync();
        return projects.Count + tasks.Count;
    }

    private IQueryable<ProjectItem> GetTrashedProjectsQuery(string userId, bool isAdmin)
    {
        IQueryable<ProjectItem> projects = context.ProjectItems.IgnoreQueryFilters().Where(p => p.IsDeleted);
        return isAdmin ? projects : projects.Where(p => p.OwnerId == userId);
    }

    // A task is in the trash when it was deleted itself or when its project was deleted.
    // Ownership comes from the parent project, like everywhere else in the app.
    private IQueryable<TaskItem> GetTrashedTasksQuery(string userId, bool isAdmin)
    {
        IQueryable<TaskItem> tasks = context.TaskItems.IgnoreQueryFilters()
            .Where(t => t.IsDeleted || t.Project.IsDeleted);
        return isAdmin ? tasks : tasks.Where(t => t.Project.OwnerId == userId);
    }
}
