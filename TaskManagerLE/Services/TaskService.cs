using Microsoft.EntityFrameworkCore;
using TaskManagerLE.Data;
using TaskManagerLE.Extensions;
using TaskManagerLE.Models;
using TaskManagerLE.Models.Enums;
using TaskStatus = TaskManagerLE.Models.Enums.TaskStatus;

namespace TaskManagerLE.Services;

public class TaskService(ApplicationDbContext context, TimeProvider timeProvider)
{
    public async Task AddTaskAsync(TaskItemCreateViewModel taskItemModel)
    {
        TaskItem taskItem = new()
        {
            Name = taskItemModel.Name,
            Description = taskItemModel.Description ?? string.Empty,
            Priority = taskItemModel.Priority,
            Status = taskItemModel.Status,
            DueDate = taskItemModel.DueDate,
            ProjectId = taskItemModel.ProjectId,
        };

        context.TaskItems.Add(taskItem);
        await context.SaveChangesAsync();
    }

    public async Task UpdateTaskAsync(TaskItem taskItem, TaskItemUpdateViewModel taskItemModel)
    {
        taskItem.Name = taskItemModel.Name;
        taskItem.Description = taskItemModel.Description ?? string.Empty;
        taskItem.Priority = taskItemModel.Priority;
        taskItem.Status = taskItemModel.Status;
        taskItem.DueDate = taskItemModel.DueDate;
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
        IQueryable<TaskItem> taskQuery = context.TaskItems.AsNoTracking();
        IQueryable<ProjectItem> projectQuery = context.ProjectItems.AsNoTracking();

        if (!isAdmin) {
            taskQuery = taskQuery.Where(t => t.Project.OwnerId == userId);
            projectQuery = projectQuery.Where(p => p.OwnerId == userId);
        }

        List<TaskListItemViewModel> tasks = await taskQuery
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

        int projectCount = await projectQuery
            .CountAsync(p => showArchived || p.Status != ProjectStatus.Archived);

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
        context.TaskItems
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == taskId);

    public async Task DeleteTaskAsync(TaskItem taskItem, string deletedByUserId)
    {
        taskItem.SoftDelete(deletedByUserId, timeProvider.UtcNow());
        await context.SaveChangesAsync();
    }

    // "Deleted" = the task's own IsDeleted flag, same meaning as ProjectService.DeletedProjects.
    // Project included for authorization, like GetTaskWithProjectAsync.
    public Task<TaskItem?> GetDeletedTaskAsync(int taskId) =>
        context.TaskItems
            .IgnoreQueryFilters()
            .Include(t => t.Project)
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
