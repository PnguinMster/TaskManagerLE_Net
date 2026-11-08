using Microsoft.EntityFrameworkCore;
using TaskMangerLE.Data;
using TaskMangerLE.Models;
using TaskStatus = TaskMangerLE.Models.Enums.TaskStatus;

namespace TaskMangerLE.Services;

public class ProjectService(ApplicationDbContext context)
{
    public async Task<List<ProjectCardViewModel>> GetProjectCardsAsync(string? userId = null)
    {
        var query = context.ProjectItems.AsNoTracking();

        if (userId is not null)
            query = query.Where(p => p.OwnerId == userId);

        var projects = await query.Include(p => p.Tasks).ToListAsync();

        return projects.Select(project =>
        {
            var tasks = project.Tasks.ToList();
            var activeTasks = tasks
                .Where(t => t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled)
                .ToList();
            var completedCount = tasks.Count(t => t.Status == TaskStatus.Done);

            return new ProjectCardViewModel
            {
                Id = project.Id,
                Name = project.Name,
                Status = project.Status,
                NextDueTaskName = activeTasks
                    .OrderBy(t => t.DueDate)
                    .Select(t => t.Name)
                    .FirstOrDefault(),
                CompletedTaskCount = completedCount,
                TotalTaskCount = tasks.Count,
                ProgressPercent = tasks.Count == 0
                    ? 0
                    : (int)Math.Round(completedCount * 100.0 / tasks.Count),
                NextDueDate = activeTasks
                    .Select(t => (DateOnly?)t.DueDate)
                    .Min()
            };
        }).ToList();
    }

    public async Task AddProjectAsync(ProjectItemCreateViewModel projectItemModel, string userId)
    {
        ProjectItem projectItem = new ProjectItem
        {
            Name = projectItemModel.Name,
            Description = projectItemModel.Description,
            Status = projectItemModel.Status,
            OwnerId = userId,
        };

        context.ProjectItems.Add(projectItem);
        await context.SaveChangesAsync();
    }

    public async Task UpdateProjectAsync(ProjectItem projectItem, ProjectItemUpdateViewModel projectItemModel)
    {
        projectItem.Name = projectItemModel.Name;
        projectItem.Description = projectItemModel.Description;
        projectItem.Status = projectItemModel.Status;

        await context.SaveChangesAsync();
    }

    public Task<ProjectItem?> GetProjectAsync(int projectId) =>
        context.ProjectItems.FirstOrDefaultAsync(p => p.Id == projectId);

    public Task<ProjectItem?> GetProjectWithTasksAsync(int projectId) =>
        context.ProjectItems.Include(p => p.Tasks).FirstOrDefaultAsync(p => p.Id == projectId);

    public Task<List<ProjectItem>> GetProjectsForUserAsync(string userId) =>
        context.ProjectItems.AsNoTracking().Where(p => p.OwnerId == userId).ToListAsync();

    public Task<List<ProjectItem>> GetAllProjectsAsync() =>
        context.ProjectItems.AsNoTracking().ToListAsync();

    public async Task DeleteProjectAsync(ProjectItem projectItem, string deletedByUserId)
    {
        DateTime deletedAtUtc = DateTime.UtcNow;

        projectItem.SoftDelete(deletedByUserId, deletedAtUtc);
        foreach (TaskItem taskItem in projectItem.Tasks)
            taskItem.SoftDelete(deletedByUserId, deletedAtUtc);

        await context.SaveChangesAsync();
    }

    public Task<List<ProjectItem>> GetDeletedProjectsAsync() =>
        GetDeletedProjectsQuery().AsNoTracking().OrderByDescending(p => p.DeletedAtUtc).ToListAsync();

    public Task<List<ProjectItem>> GetDeletedProjectsForUserAsync(string userId) =>
        GetDeletedProjectsQuery().AsNoTracking().Where(p => p.OwnerId == userId)
            .OrderByDescending(p => p.DeletedAtUtc).ToListAsync();

// Restore page and restore POST. Tasks that were deleted with the project.
    public Task<ProjectItem?> GetDeletedProjectWithDeletedTasksAsync(int projectId) =>
        GetDeletedProjectsQuery().Include(p => p.Tasks.Where(t => t.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == projectId);

    public Task<ProjectItem?> GetProjectWithDeletedTasksAsync(int projectId) =>
        context.ProjectItems.IgnoreQueryFilters().Where(p => !p.IsDeleted)
            .Include(p => p.Tasks.Where(t => t.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == projectId);

// Purge needs every task, including the deleted ones.
    public Task<ProjectItem?> GetDeletedProjectWithAllTasksAsync(int projectId) =>
        GetDeletedProjectsQuery().Include(p => p.Tasks).FirstOrDefaultAsync(p => p.Id == projectId);

    private IQueryable<ProjectItem> GetDeletedProjectsQuery() =>
        context.ProjectItems.IgnoreQueryFilters().Where(p => p.IsDeleted);

    public async Task RestoreProjectAsync(ProjectItem projectItem)
    {
        DateTime? projectDeletedAt = projectItem.DeletedAtUtc;

        projectItem.Restore();
        foreach (TaskItem taskItem in projectItem.Tasks.Where(t => t.DeletedAtUtc == projectDeletedAt))
            taskItem.Restore();

        await context.SaveChangesAsync();
    }

    public async Task PurgeProjectAsync(ProjectItem projectItem)
    {
        context.TaskItems.RemoveRange(projectItem.Tasks);
        context.ProjectItems.Remove(projectItem);

        await context.SaveChangesAsync();
    }
}