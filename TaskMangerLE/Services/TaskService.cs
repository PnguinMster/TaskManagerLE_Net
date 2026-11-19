using Microsoft.EntityFrameworkCore;
using TaskMangerLE.Data;
using TaskMangerLE.Models;
using TaskMangerLE.Models.Enums;
using TaskStatus = TaskMangerLE.Models.Enums.TaskStatus;

namespace TaskMangerLE.Services;

public class TaskService(ApplicationDbContext context, TimeProvider timeProvider)
{
    public async Task AddTaskAsync(TaskItemCreateViewModel taskItemModel)
    {
        DateOnly dueDate = taskItemModel.DueDate ?? throw new ArgumentException("A due date is required", nameof(taskItemModel));

        TaskItem taskItem = new TaskItem
        {
            Name = taskItemModel.Name,
            Description = taskItemModel.Description ?? string.Empty,
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
        taskItem.Description = taskItemModel.Description ?? string.Empty;
        taskItem.Priority = taskItemModel.Priority;
        taskItem.Status = taskItemModel.Status;
        taskItem.DueDate = dueDate;
        // The controller has already checked the user owns this project.
        taskItem.ProjectId = taskItemModel.ProjectId;

        await context.SaveChangesAsync();
    }

    public async Task ToggleCompletionAsync(TaskItem taskItem)
    {
        taskItem.Status = taskItem.Status == TaskStatus.Done
            ? TaskStatus.ToDo
            : TaskStatus.Done;

        await context.SaveChangesAsync();
    }

    // Undo for a completion: puts back the status the task had before (e.g. In progress), which
    // toggling again wouldn't do, since that always goes to To do.
    public async Task UndoCompletionAsync(TaskItem taskItem, TaskStatus previousStatus)
    {
        taskItem.Status = previousStatus;
        await context.SaveChangesAsync();
    }

    // Admins see every user's tasks, same rule as ProjectService.GetProjectCardsAsync.
    public async Task<TaskListViewModel> GetTaskListAsync(string userId, bool isAdmin, bool showArchived)
    {
        var tasks = await context.TaskItems
            .AsNoTracking()
            .Where(t => isAdmin || t.Project.OwnerId == userId)
            .Where(t => showArchived || t.Project.Status != ProjectStatus.Archived)
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.Name)
            .Select(t => new TaskListItemViewModel
            {
                Id = t.Id,
                Name = t.Name,
                ProjectId = t.ProjectId,
                Description = t.Description,
                ProjectName = t.Project.Name,
                Status = t.Status,
                Priority = t.Priority,
                DueDate = t.DueDate
            })
            .ToListAsync();

        int projectCount = await context.ProjectItems
            .AsNoTracking()
            .CountAsync(p => (isAdmin || p.OwnerId == userId) && (showArchived || p.Status != ProjectStatus.Archived));

        return new TaskListViewModel
        {
            TotalTaskCount = tasks.Count,
            ProjectCount = projectCount,
            ShowArchived = showArchived,
            Tasks = tasks
        };
    }

    // Project included for authorization
    public Task<TaskItem?> GetTaskWithProjectAsync(int taskId) =>
        context.TaskItems.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == taskId);

    public async Task DeleteTaskAsync(TaskItem taskItem, string deletedByUserId)
    {
        taskItem.SoftDelete(deletedByUserId, timeProvider.GetUtcNow().UtcDateTime);
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