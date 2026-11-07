using Microsoft.EntityFrameworkCore;
using TaskMangerLE.Data;
using TaskMangerLE.Models;
using TaskMangerLE.Models.Enums;
using TaskStatus = TaskMangerLE.Models.Enums.TaskStatus;

namespace TaskMangerLE.Services;

public class DashboardServices(ApplicationDbContext context, TimeProvider timeProvider)
{
    private const int RecentProjectsLimit = 3;
    private const int UpcomingTasksLimit = 3;

    public async Task<DashboardViewModel> GetDashboardAsync(string userId)
    {
        DateOnly today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var projectCounts = await context.ProjectItems.AsNoTracking().Where(p => p.OwnerId == userId).GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();

        int pendingTasks = await context.TaskItems.AsNoTracking()
            .CountAsync(t => t.Project.OwnerId == userId && t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled);

        int overdueTasks = await context.TaskItems.AsNoTracking().CountAsync(t =>
            t.Project.OwnerId == userId && t.DueDate < today && t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled);

        var recentProjects = await context.ProjectItems.AsNoTracking().Where(p => p.OwnerId == userId).OrderByDescending(p => p.UpdatedAtUtc)
            .Take(RecentProjectsLimit).Select(p => new DashboardProjectViewModel
            {
                Id = p.Id,
                Name = p.Name,
                DueTaskName = p.Tasks.Where(t => t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled).OrderBy(t => t.DueDate).Select(t => t.Name)
                    .FirstOrDefault() ?? string.Empty,
                Status = p.Status,
                UpdatedAtUtc = p.UpdatedAtUtc,
                TotalTasksCount = p.Tasks.Count,
                CompletedTasksCount = p.Tasks.Count(t => t.Status == TaskStatus.Done),
            }).ToListAsync();

        var upcomingTasks = await context.TaskItems.AsNoTracking()
            .Where(t => t.Project.OwnerId == userId && t.DueDate >= today && t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled)
            .OrderBy(t => t.DueDate).Take(UpcomingTasksLimit).Select(t => new DashboardTaskViewModel
            {
                Id = t.Id,
                ProjectId = t.ProjectId,
                Name = t.Name,
                ProjectName = t.Project.Name,
                Priority = t.Priority,
                DueDateUtc = t.DueDate.ToDateTime(TimeOnly.MinValue),
            })
            .ToListAsync();

        return new DashboardViewModel
        {
            IsAuthenticated = true,
            ActiveProjectsCount = projectCounts.FirstOrDefault(c => c.Status == ProjectStatus.Active)?.Count ?? 0,
            CompletedProjectsCount = projectCounts.FirstOrDefault(c => c.Status == ProjectStatus.Completed)?.Count ?? 0,
            PendingTasksCount = pendingTasks,
            OverdueTasksCount = overdueTasks,
            RecentProjects = recentProjects,
            UpcomingTasks = upcomingTasks,
        };
    }
}
