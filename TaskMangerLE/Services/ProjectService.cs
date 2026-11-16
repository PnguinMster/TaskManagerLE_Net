using Microsoft.EntityFrameworkCore;
using TaskMangerLE.Data;
using TaskMangerLE.Models;
using TaskStatus = TaskMangerLE.Models.Enums.TaskStatus;

namespace TaskMangerLE.Services;

public class ProjectService(ApplicationDbContext context, TimeProvider timeProvider)
{
    public async Task<List<ProjectCardViewModel>> GetProjectCardsAsync(string? userId = null)
    {
        var query = context.ProjectItems.AsNoTracking();

        if (userId is not null)
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
        ProjectItem projectItem = new ProjectItem
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
        context.ProjectItems.Include(p => p.Tasks).FirstOrDefaultAsync(p => p.Id == projectId);

    public Task<List<ProjectItem>> GetProjectsForUserAsync(string userId) =>
        context.ProjectItems.AsNoTracking().Where(p => p.OwnerId == userId).ToListAsync();

    public Task<List<ProjectItem>> GetAllProjectsAsync() =>
        context.ProjectItems.AsNoTracking().ToListAsync();

    public async Task DeleteProjectAsync(ProjectItem projectItem, string deletedByUserId)
    {
        DateTime deletedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

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