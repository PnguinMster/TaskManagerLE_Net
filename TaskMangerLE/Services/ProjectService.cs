using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    public async Task<ProjectItem?> FindProjectAsync(int? id)
    {
        return await context.ProjectItems.FindAsync(id);
    }

    public async Task UpdateProjectAsync(ProjectItem projectItem)
    {
        context.ProjectItems.Update(projectItem);
        await context.SaveChangesAsync();
    }

    public async Task<List<ProjectItem>> GetAllAsync()
    {
        return await context.ProjectItems.ToListAsync();
    }
}