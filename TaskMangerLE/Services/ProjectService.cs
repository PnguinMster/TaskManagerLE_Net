using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using TaskMangerLE.Data;
using TaskMangerLE.Models;

namespace TaskMangerLE.Services;

public class ProjectService(ApplicationDbContext context)
{
    public async Task AddProjectAsync(ProjectItemCreateViewModel projectItemModel, string? userId)
    {
        // Check for empty strings
        if (string.IsNullOrWhiteSpace(projectItemModel.Name))
            projectItemModel.Name = "Untitled Project";
        if (string.IsNullOrWhiteSpace(projectItemModel.Description))
            projectItemModel.Description = "No Description";

        ProjectItem projectItem = new ProjectItem
        {
            Name = projectItemModel.Name,
            Description = projectItemModel.Description,
            Status = projectItemModel.Status,
            OwnerId = userId!,
        };

        await context.ProjectItems.AddAsync(projectItem);
        await context.SaveChangesAsync();
    }

    public async Task UpdateProjectAsync(ProjectItem projectItem, ProjectItemUpdateViewModel projectItemModel)
    {
        projectItem.Name = projectItemModel.Name;
        projectItem.Description = projectItemModel.Description;
        projectItem.Status = projectItemModel.Status;

        await context.SaveChangesAsync();
    }

    public async Task<ProjectItem?> FindProjectAsync(int? id)
    {
        return await context.ProjectItems.FindAsync(id);
    }

    public async Task<ProjectItem?> GetByIdWithTasksAsync(int? projectId)
    {
        return await context.ProjectItems.Include(p => p.Tasks).FirstOrDefaultAsync(p => p.Id == projectId);
    }

    public async Task<List<TaskItem>> GetByProjectIdAsync(int? projectId)
    {
        return await context.TaskItems.Where(t => t.ProjectId == projectId).ToListAsync();
    }

    public async Task<List<ProjectItem>> GetUserAllAsync(string? userId)
    {
        return await context.ProjectItems.Where(p => p.OwnerId == userId).ToListAsync();
        // return await context.ProjectItems.Where(p => p.OwnerId == userId).Include(p => p.Tasks).ToListAsync();
    }


    [Authorize(Roles = "Admin")]
    public async Task<List<ProjectItem>> GetAllAsync()
    {
        return await context.ProjectItems.ToListAsync();
    }

    public async Task DeleteProject(ProjectItem projectItem, string? deletedByUserId)
    {
        DateTime deletedTime = DateTime.UtcNow;

        projectItem.IsDeleted = true;
        projectItem.DeletedAtUtc = deletedTime;
        projectItem.DeletedByUserId = deletedByUserId;

        foreach (TaskItem taskItem in projectItem.Tasks) {
            taskItem.IsDeleted = true;
            taskItem.DeletedAtUtc = deletedTime;
            taskItem.DeletedByUserId = deletedByUserId;
        }

        await context.SaveChangesAsync();
    }

    public Task<List<ProjectItem>> GetAllDeletedAsync() => GetDeletedQuery().ToListAsync();

    public Task<List<ProjectItem>> GetAllDeletedForUserAsync(string? userid) =>
        GetDeletedQuery().Where(p => p.OwnerId == userid).ToListAsync();


    private IQueryable<ProjectItem> GetDeletedQuery() =>
        context.ProjectItems.IgnoreQueryFilters().Where(p => p.IsDeleted)
            .Include(p => p.Tasks.Where(t => t.IsDeleted))
            .OrderByDescending(p => p.DeletedAtUtc);

    public async Task<ProjectItem?> GetDeletedProjectWithDeletedTasks(int? projectId)
    {
        return await context.ProjectItems.IgnoreQueryFilters().Include(p => p.Tasks.Where(t => t.IsDeleted)).FirstOrDefaultAsync(p => p.Id == projectId);
    }

    public async Task<ProjectItem?> GetDeletedProjectAllTasks(int? projectId)
    {
        return await context.ProjectItems.IgnoreQueryFilters().Include(p => p.Tasks).FirstOrDefaultAsync(p => p.Id == projectId && p.IsDeleted);
    }

    public async Task RestoreProject(ProjectItem projectItem)
    {
        DateTime? projectDeletedAt = projectItem.DeletedAtUtc;
        projectItem.IsDeleted = false;
        projectItem.DeletedAtUtc = null;
        projectItem.DeletedByUserId = null;

        foreach (TaskItem taskItem in projectItem.Tasks.Where(t => t.DeletedAtUtc == projectDeletedAt)) {
            taskItem.IsDeleted = false;
            taskItem.DeletedAtUtc = null;
            taskItem.DeletedByUserId = null;
        }

        await context.SaveChangesAsync();
    }

    public async Task PurgeProject(ProjectItem projectItem)
    {
        context.TaskItems.RemoveRange(projectItem.Tasks);
        context.ProjectItems.Remove(projectItem);

        await context.SaveChangesAsync();
    }
}