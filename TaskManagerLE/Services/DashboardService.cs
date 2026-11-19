using Microsoft.EntityFrameworkCore;
using TaskManagerLE.Data;
using TaskManagerLE.Extensions;
using TaskManagerLE.Models;
using TaskManagerLE.Models.Enums;
using TaskStatus = TaskManagerLE.Models.Enums.TaskStatus;

namespace TaskManagerLE.Services;

public class DashboardService(ApplicationDbContext context, TimeProvider timeProvider)
{
    private const int RecentProjectsLimit = 3;
    private const int UpcomingTasksLimit = 3;

    // Always the signed-in user's own work, admins included (list pages show admins everything).
    public async Task<DashboardViewModel> GetDashboardAsync(string userId)
    {
        DateOnly today = timeProvider.Today();

        // Every status, archived included, so it also tells us whether the user has any projects at all.
        var projectCounts = await context.ProjectItems
            .AsNoTracking()
            .Where(p => p.OwnerId == userId)
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        // Archived projects are put away on purpose, so neither they nor their tasks show on the dashboard.
        IQueryable<TaskItem> openTasks = context.TaskItems
            .AsNoTracking()
            .Where(t =>
                t.Project.OwnerId == userId &&
                t.Project.Status != ProjectStatus.Archived &&
                !TaskStatusExtensions.FinishedStatuses.Contains(t.Status));

        int pendingTasks = await openTasks.CountAsync();

        int overdueTasks = await openTasks.CountAsync(t => t.DueDate < today);

        List<DashboardProjectViewModel> recentProjects = await context.ProjectItems
            .AsNoTracking()
            .Where(p => p.OwnerId == userId && p.Status != ProjectStatus.Archived)
            .OrderByDescending(p => p.UpdatedAtUtc)
            .Take(RecentProjectsLimit)
            .Select(p => new DashboardProjectViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status,
                UpdatedAtUtc = p.UpdatedAtUtc,
                // Progress rule: see ProjectService.GetProjectCardsAsync.
                CompletedTaskCount = p.Tasks.Count(t => t.Status == TaskStatus.Done),
                TotalTaskCount = p.Tasks.Count(t => t.Status != TaskStatus.Cancelled),
            })
            .ToListAsync();

        List<DashboardTaskViewModel> upcomingTasks = await openTasks
            .Where(t => t.DueDate >= today)
            .OrderBy(t => t.DueDate)
            .Take(UpcomingTasksLimit)
            .Select(t => new DashboardTaskViewModel
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
            HasAnyProjects = projectCounts.Sum(c => c.Count) > 0,
            ActiveProjectCount = projectCounts.FirstOrDefault(c => c.Status == ProjectStatus.Active)?.Count ?? 0,
            CompletedProjectCount = projectCounts.FirstOrDefault(c => c.Status == ProjectStatus.Completed)?.Count ?? 0,
            PendingTaskCount = pendingTasks,
            OverdueTaskCount = overdueTasks,
            RecentProjects = recentProjects,
            UpcomingTasks = upcomingTasks,
        };
    }
}
