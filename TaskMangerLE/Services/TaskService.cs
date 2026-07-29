using Microsoft.EntityFrameworkCore;
using TaskMangerLE.Data;
using TaskMangerLE.Models;

namespace TaskMangerLE.Services;

public class TaskService(ApplicationDbContext context)
{
    public async Task AddTaskAsync(TaskItem taskItem)
    {
        if (string.IsNullOrWhiteSpace(taskItem.Name))
            taskItem.Name = "Untitled Task";
        if (string.IsNullOrWhiteSpace(taskItem.Description))
            taskItem.Description = "No Description";

        taskItem.CreatedDate = DateTime.UtcNow;
        taskItem.LastUpdated = DateTime.UtcNow;

        await context.TaskItems.AddAsync(taskItem);
        await context.SaveChangesAsync();
    }

    public async Task UpdateTaskAsync(TaskItem taskItem)
    {
        context.TaskItems.Update(taskItem);
        await context.SaveChangesAsync();
    }

    public async Task<TaskItem?> FindTaskAsync(int? taskId)
    {
        return await context.TaskItems.FindAsync(taskId);
    }

    public async Task<List<TaskItem>> GetAllAsync()
    {
        return await context.TaskItems.ToListAsync();
    }
}