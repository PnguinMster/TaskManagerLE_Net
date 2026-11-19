using TaskManagerLE.Models.Enums;

namespace TaskManagerLE.Models;

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

    public string Description { get; init; } = string.Empty;
    public int TotalTaskCount { get; init; }
    public int CompletedTaskCount { get; init; }

    public ProjectStatus Status { get; init; }
    public DateTime UpdatedAtUtc { get; init; }


    public int ProgressPercent => ProgressMath.Percent(CompletedTaskCount, TotalTaskCount);
}

public class DashboardTaskViewModel
{
    public int Id { get; init; }
    public int ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public TaskPriority Priority { get; init; }
    public DateOnly DueDate { get; init; }
}