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
            LastUpdated = DateTime.UtcNow,
            ProjectId = taskItemModel.ProjectId
        };

        await context.TaskItems.AddAsync(taskItem);
        await context.SaveChangesAsync();
    }

    public async Task UpdateTaskAsync(TaskItem taskItem, TaskItemUpdateViewModel taskItemModel)
    {
        taskItem.Name = taskItemModel.Name;
        taskItem.Description = taskItemModel.Description;
        taskItem.Priority = taskItemModel.Priority;
        taskItem.Status = taskItemModel.Status;
        taskItem.DueDate = taskItemModel.DueDate;
        taskItem.LastUpdated = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task<TaskItem?> FindTaskAsync(int? taskId)
    {
        return await context.TaskItems.FindAsync(taskId);
    }

    public async Task<TaskItem?> FindTaskWithOwnerAsync(int? taskId)
    {
        return await context.TaskItems.Include(t => t.ProjectItem).FirstOrDefaultAsync(t => t.Id == taskId);
    }

    public async Task<List<TaskItem>> GetAllAsync()
    {
        return await context.TaskItems.ToListAsync();
    }

    public async Task DeleteTask(TaskItem taskItem, string? deletedByUserId)
    {
        taskItem.IsDeleted = true;
        taskItem.DeletedAtUtc = DateTime.UtcNow;
        taskItem.DeletedByUserId = deletedByUserId;
        await context.SaveChangesAsync();
    }

    public async Task<List<TaskItem>> GetDeleted(string? userId, int? projectId)
    {
        return await context.TaskItems.IgnoreQueryFilters().Where(t => t.IsDeleted && !t.ProjectItem!.IsDeleted && t.ProjectItem.OwnerId == userId)
            .Include(t => t.ProjectItem)
            .OrderByDescending(t => t.DeletedAtUtc).ToListAsync();
    }

    public async Task<TaskItem?> GetDeletedTask(int? taskId)
    {
        return await context.TaskItems.IgnoreQueryFilters().Include(t => t.ProjectItem).FirstOrDefaultAsync(t => t.Id == taskId);
    }

    public async Task<TaskItem?> GetIsDeletedTask(int? taskId)
    {
        return await context.TaskItems.IgnoreQueryFilters().Include(t => t.ProjectItem).FirstOrDefaultAsync(t => t.Id == taskId && t.IsDeleted);
    }

    public async Task RestoreTask(TaskItem taskItem)
    {
        taskItem.IsDeleted = false;
        taskItem.DeletedAtUtc = null;
        taskItem.DeletedByUserId = null;
        await context.SaveChangesAsync();
    }

    public async Task PurgeTask(TaskItem taskItem)
    {
        context.TaskItems.Remove(taskItem);
        await context.SaveChangesAsync();
    }
}