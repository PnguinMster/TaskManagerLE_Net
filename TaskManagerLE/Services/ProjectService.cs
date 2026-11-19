using Microsoft.EntityFrameworkCore;
using TaskManagerLE.Data;
using TaskManagerLE.Extensions;
using TaskManagerLE.Models;
using TaskStatus = TaskManagerLE.Models.Enums.TaskStatus;

namespace TaskManagerLE.Services;

public class ProjectService(ApplicationDbContext context, TimeProvider timeProvider)
{
    // Admins see every user's projects, same rule as TrashService.
    public async Task<List<ProjectCardViewModel>> GetProjectCardsAsync(string userId, bool isAdmin)
    {
        IQueryable<ProjectItem> query = context.ProjectItems.AsNoTracking();

        if (!isAdmin)
            query = query.Where(p => p.OwnerId == userId);

        // Counted in SQL (like GetProjectDetailAsync), so the tasks themselves are never loaded.
        return await query
            .Select(p => new ProjectCardViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Status = p.Status,
                Description = p.Description,
                // Cancelled tasks will never be done, so they don't count toward progress.
                // Same two lines in GetProjectDetailAsync and DashboardService; kept inline so EF can turn them into SQL.
                CompletedTaskCount = p.Tasks.Count(t => t.Status == TaskStatus.Done),
                TotalTaskCount = p.Tasks.Count(t => t.Status != TaskStatus.Cancelled),
                DueDate = p.DueDate
            })
            .ToListAsync();
    }

    public async Task<ProjectDetailViewModel?> GetProjectDetailAsync(int projectId)
    {
        return await context.ProjectItems
            .AsNoTracking()
            .Where(p => p.Id == projectId)
            .Select(p => new ProjectDetailViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status,
                CreatedAtUtc = p.CreatedAtUtc,
                OwnerName = p.Owner.UserName ?? p.Owner.Email ?? "Unknown",
                // Progress rule: see GetProjectCardsAsync.
                CompletedTaskCount = p.Tasks.Count(t => t.Status == TaskStatus.Done),
                TotalTaskCount = p.Tasks.Count(t => t.Status != TaskStatus.Cancelled),
                DueDate = p.DueDate,
                Tasks = p.Tasks
                    .OrderBy(t => t.DueDate)
                    .Select(t => new ProjectDetailTaskViewModel
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Description = t.Description,
                        Status = t.Status,
                        Priority = t.Priority,
                        DueDate = t.DueDate,
                        CreatedAtUtc = t.CreatedAtUtc
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task AddProjectAsync(ProjectItemCreateViewModel projectItemModel, string userId)
    {
        ProjectItem projectItem = new()
        {
            Name = projectItemModel.Name,
            Description = projectItemModel.Description ?? string.Empty,
            DueDate = projectItemModel.DueDate,
            Status = projectItemModel.Status,
            OwnerId = userId,
        };

        context.ProjectItems.Add(projectItem);
        await context.SaveChangesAsync();
    }

    public async Task UpdateProjectAsync(ProjectItem projectItem, ProjectItemUpdateViewModel projectItemModel)
    {
        projectItem.Name = projectItemModel.Name;
        projectItem.Description = projectItemModel.Description ?? string.Empty;
        projectItem.DueDate = projectItemModel.DueDate;
        projectItem.Status = projectItemModel.Status;

        await context.SaveChangesAsync();
    }

    public Task<ProjectItem?> GetProjectAsync(int projectId) =>
        context.ProjectItems.FirstOrDefaultAsync(p => p.Id == projectId);

    public Task<ProjectItem?> GetProjectWithTasksAsync(int projectId) =>
        context.ProjectItems
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == projectId);

    public async Task DeleteProjectAsync(ProjectItem projectItem, string deletedByUserId)
    {
        DateTime deletedAtUtc = timeProvider.UtcNow();

        projectItem.SoftDelete(deletedByUserId, deletedAtUtc);
        foreach (TaskItem taskItem in projectItem.Tasks)
            taskItem.SoftDelete(deletedByUserId, deletedAtUtc);

        await context.SaveChangesAsync();
    }

    // Restore POST. Tasks that were deleted with the project.
    public Task<ProjectItem?> GetDeletedProjectWithDeletedTasksAsync(int projectId) =>
        DeletedProjects()
            .Include(p => p.Tasks.Where(t => t.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == projectId);

    // Purge needs every task, including the deleted ones.
    public Task<ProjectItem?> GetDeletedProjectWithAllTasksAsync(int projectId) =>
        DeletedProjects()
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == projectId);

    // "Deleted" = the project's own IsDeleted flag, any owner (the controller checks ownership).
    // Not the same as TrashService.TrashedProjects/TrashedTasks, which are what one user's Trash page shows.
    private IQueryable<ProjectItem> DeletedProjects() =>
        context.ProjectItems
            .IgnoreQueryFilters()
            .Where(p => p.IsDeleted);

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
