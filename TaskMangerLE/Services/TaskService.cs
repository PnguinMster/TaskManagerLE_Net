using Microsoft.EntityFrameworkCore;
using TaskMangerLE.Data;
using TaskMangerLE.Models;
using TaskMangerLE.Models.Enums;
using TaskStatus = TaskMangerLE.Models.Enums.TaskStatus;

namespace TaskMangerLE.Services;

public class TaskService(ApplicationDbContext context)
{
    public async Task AddTaskAsync(TaskItemCreateViewModel taskItemModel)
    {
        DateOnly dueDate = taskItemModel.DueDate ?? throw new ArgumentException("A due date is required", nameof(taskItemModel));

        TaskItem taskItem = new TaskItem
        {
            Name = taskItemModel.Name,
            Description = taskItemModel.Description,
            Priority = taskItemModel.Priority,
            Status = taskItemModel.Status,
            DueDate = dueDate,
            ProjectId = taskItemModel.ProjectId,
        };

        context.TaskItems.Add(taskItem);
        await context.SaveChangesAsync();
    }

    public async Task UpdateTaskAsync(TaskItem taskItem, TaskItemUpdateViewModel taskItemModel)
    {
        DateOnly dueDate = taskItemModel.DueDate ?? throw new ArgumentException("A due date is required", nameof(taskItemModel));

        taskItem.Name = taskItemModel.Name;
        taskItem.Description = taskItemModel.Description;
        taskItem.Priority = taskItemModel.Priority;
        taskItem.Status = taskItemModel.Status;
        taskItem.DueDate = dueDate;

        await context.SaveChangesAsync();
    }

    public async Task CompleteTaskAsync(TaskItem taskItem)
    {
        taskItem.Status = TaskStatus.Done;
        await context.SaveChangesAsync();
    }

    // Project included for authorization
    public Task<TaskItem?> GetTaskWithProjectAsync(int taskId) =>
        context.TaskItems.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == taskId);

    public async Task DeleteTaskAsync(TaskItem taskItem, string deletedByUserId)
    {
        taskItem.SoftDelete(deletedByUserId, DateTime.UtcNow);
        await context.SaveChangesAsync();
    }

    public Task<TaskItem?> GetDeletedTaskAsync(int taskId) =>
        context.TaskItems.IgnoreQueryFilters().Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.IsDeleted);

    public async Task RestoreTaskAsync(TaskItem taskItem)
    {
        taskItem.Restore();
        await context.SaveChangesAsync();
    }

    public async Task PurgeTaskAsync(TaskItem taskItem)
    {
        context.TaskItems.Remove(taskItem);
        await context.SaveChangesAsync();
    }
}
