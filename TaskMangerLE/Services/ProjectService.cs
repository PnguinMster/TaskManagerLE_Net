using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using TaskMangerLE.Data;
using TaskMangerLE.Models;

namespace TaskMangerLE.Services;

public class ProjectService(ApplicationDbContext context)
{
    public async Task AddProjectAsync(ProjectItem projectItem)
    {
        if (string.IsNullOrWhiteSpace(projectItem.Name))
            projectItem.Name = "Untitled Project";
        if (string.IsNullOrWhiteSpace(projectItem.Description))
            projectItem.Description = "No Description";

        projectItem.CreatedDate = DateTime.UtcNow;
        projectItem.LastUpdated = DateTime.UtcNow;

        await context.ProjectItems.AddAsync(projectItem);
        await context.SaveChangesAsync();
    }

    public async Task UpdateProjectAsync(ProjectItem projectItem)
    {
        context.ProjectItems.Update(projectItem);
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
}