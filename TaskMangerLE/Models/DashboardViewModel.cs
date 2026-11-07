using TaskMangerLE.Models.Enums;

namespace TaskMangerLE.Models;

public class DashboardViewModel
{
    public bool IsAuthenticated { get; init; }

    public int ActiveProjectsCount { get; init; }
    public int PendingTasksCount { get; init; }
    public int OverdueTasksCount { get; init; }
    public int CompletedProjectsCount { get; init; }

    public IReadOnlyList<DashboardProjectViewModel> RecentProjects { get; init; } = [];
    public IReadOnlyList<DashboardTaskViewModel> UpcomingTasks { get; init; } = [];
}

public class DashboardProjectViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;

    public string DueTaskName { get; init; } = string.Empty;
    public int TotalTasksCount { get; init; }
    public int CompletedTasksCount { get; init; }

    public ProjectStatus Status { get; init; }
    public DateTime UpdatedAtUtc { get; init; }


    public int CompletedPercent => TotalTasksCount == 0 ? 0 : (int)Math.Round(CompletedTasksCount * 100.0 / TotalTasksCount);
}

public class DashboardTaskViewModel
{
    public int Id { get; init; }
    public int ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public TaskPriority Priority { get; init; }
    public DateTime? DueDateUtc { get; init; }
}
