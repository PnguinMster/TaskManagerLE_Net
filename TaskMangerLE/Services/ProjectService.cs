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
            CreatedDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
        };

        await context.ProjectItems.AddAsync(projectItem);
        await context.SaveChangesAsync();
    }

    public async Task<bool> UpdateProjectAsync(ProjectItemUpdateViewModel projectItemModel)
    {
        ProjectItem? projectItem = await context.ProjectItems.FindAsync(projectItemModel.Id);

        if (projectItem == null) return false;

        projectItem.Name = projectItemModel.Name;
        projectItem.Description = projectItemModel.Description;
        projectItem.Status = projectItemModel.Status;
        projectItem.LastUpdated = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return true;
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
        projectItem.IsDeleted = true;
        projectItem.DeletedAtUtc = DateTime.UtcNow;
        projectItem.DeletedByUserId = deletedByUserId;

        foreach (TaskItem taskItem in projectItem.Tasks) {
            taskItem.IsDeleted = true;
            taskItem.DeletedAtUtc = DateTime.UtcNow;
            taskItem.DeletedByUserId = deletedByUserId;
        }

        await context.SaveChangesAsync();
    }
}