using Microsoft.EntityFrameworkCore;
using TaskMangerLE.Data;
using TaskMangerLE.Models;

namespace TaskMangerLE.Services;

public class TaskService(ApplicationDbContext context)
{
    public async Task AddTaskAsync(TaskItemCreateViewModel taskItemModel)
    {
        if (string.IsNullOrWhiteSpace(taskItemModel.Name))
            taskItemModel.Name = "Untitled Task";
        if (string.IsNullOrWhiteSpace(taskItemModel.Description))
            taskItemModel.Description = "No Description";

        TaskItem taskItem = new TaskItem
        {
            Name = taskItemModel.Name,
            Description = taskItemModel.Description,
            Priority = taskItemModel.Priority,
            Status = taskItemModel.Status,
            DueDate = taskItemModel.DueDate,
            CreatedDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        await context.TaskItems.AddAsync(taskItem);
        await context.SaveChangesAsync();
    }

    public async Task<bool> UpdateTaskAsync(TaskItemUpdateViewModel taskItemModel)
    {
        TaskItem? taskItem = await context.TaskItems.FindAsync(taskItemModel.Id);
        
        if(taskItem == null) return false;

        taskItem.Name = taskItemModel.Name;
        taskItem.Description = taskItemModel.Description;
        taskItem.Priority = taskItemModel.Priority;
        taskItem.Status = taskItemModel.Status;
        taskItem.DueDate = taskItemModel.DueDate;
        taskItem.LastUpdated = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        return true;
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