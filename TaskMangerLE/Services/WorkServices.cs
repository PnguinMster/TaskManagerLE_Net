using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskMangerLE.Data;
using TaskMangerLE.Models;

namespace TaskMangerLE.Services;

public class WorkServices(ApplicationDbContext context)
{
    // private readonly List<ProjectItem> _projectsList = [];
    private readonly ApplicationDbContext _context = context;
    // private int _nextId = 0;

    // public IReadOnlyList<ProjectItem> GetAll() => _projectsList;

    public async Task AddProjectAsync(ProjectItem projectItem)
    {
        if (string.IsNullOrWhiteSpace(projectItem.Name))
            projectItem.Name = "Untitled Project";
        if (string.IsNullOrWhiteSpace(projectItem.Description))
            projectItem.Description = "No Description";

        projectItem.CreatedDate = DateTime.UtcNow;
        projectItem.LastUpdated = DateTime.UtcNow;

        await _context.ProjectItems.AddAsync(projectItem);
        await _context.SaveChangesAsync();
    }

    /*
    public void AddTask(ProjectItem proj, TaskItem taskItem)
    {
        proj.Tasks.Add(taskItem);
    }
    */

    public async Task<ProjectItem?> FindProjectAsync(int? id)
    {
        return await _context.ProjectItems.FindAsync(id);
    }

    public async Task UpdateProjectAsync(ProjectItem projectItem)
    {
        _context.ProjectItems.Update(projectItem);
        await _context.SaveChangesAsync();
    }

    /*
    public void ChangeDescription(TaskItem task, string newDescription)
    {
        task.Description = newDescription;
    }

    public void UpdateTaskStatus(TaskItem task, bool isCompleted)
    {
        task.IsCompleted = isCompleted;
    }

    public void DeleteProject(ProjectItem proj)
    {
        _context.Remove(proj);
    }

    public void DeleteTask(ProjectItem proj, TaskItem task)
    {
        proj.Tasks.Remove(task);
    }
    */

    public async Task<List<ProjectItem>> GetAllAsync()
    {
        return await _context.ProjectItems.ToListAsync();
    }
}