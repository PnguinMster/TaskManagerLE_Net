using Microsoft.EntityFrameworkCore;
using TaskManagerLE.Data;
using TaskManagerLE.Extensions;
using TaskManagerLE.Models;
using TaskManagerLE.Models.Enums;
using TaskStatus = TaskManagerLE.Models.Enums.TaskStatus;

namespace TaskManagerLE.Services;

public class DashboardServices(ApplicationDbContext context, TimeProvider timeProvider)
{
    private const int RecentProjectsLimit = 3;
    private const int UpcomingTasksLimit = 3;

    // Always the signed-in user's own work, admins included (list pages show admins everything).
    public async Task<DashboardViewModel> GetDashboardAsync(string userId)
    {
        DateOnly today = timeProvider.Today();

        var projectCounts = await context.ProjectItems.AsNoTracking().Where(p => p.OwnerId == userId).GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();

        // Archived projects are put away on purpose, so neither they nor their tasks show on the dashboard.
        var openTasks = context.TaskItems.AsNoTracking().Where(t =>
            t.Project.OwnerId == userId &&
            t.Project.Status != ProjectStatus.Archived &&
            !TaskStatusExtensions.FinishedStatuses.Contains(t.Status));

        int pendingTasks = await openTasks.CountAsync();

        int overdueTasks = await openTasks.CountAsync(t => t.DueDate < today);

        var recentProjects = await context.ProjectItems.AsNoTracking()
            .Where(p => p.OwnerId == userId && p.Status != ProjectStatus.Archived)
            .OrderByDescending(p => p.UpdatedAtUtc)
            .Take(RecentProjectsLimit).Select(p => new DashboardProjectViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status,
                UpdatedAtUtc = p.UpdatedAtUtc,
                TotalTaskCount = p.Tasks.Count(t => t.Status != TaskStatus.Cancelled),
                CompletedTaskCount = p.Tasks.Count(t => t.Status == TaskStatus.Done),
            }).ToListAsync();

        var upcomingTasks = await openTasks
            .Where(t => t.DueDate >= today)
            .OrderBy(t => t.DueDate).Take(UpcomingTasksLimit).Select(t => new DashboardTaskViewModel
            {
                Id = t.Id,
                ProjectId = t.ProjectId,
                Name = t.Name,
                ProjectName = t.Project.Name,
                Priority = t.Priority,
                DueDate = t.DueDate,
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
