using TaskManagerLE.Models.Enums;
using TaskStatus = TaskManagerLE.Models.Enums.TaskStatus;

namespace TaskManagerLE.Models;

public class ProjectDetailViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ProjectStatus Status { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public string OwnerName { get; init; } = string.Empty;
    public DateOnly DueDate { get; init; }
    public IReadOnlyList<ProjectDetailTaskViewModel> Tasks { get; init; } = [];
    public int CompletedTaskCount { get; init; }
    public int TotalTaskCount { get; init; }

    public int RemainingTaskCount =>
        TotalTaskCount - CompletedTaskCount;

    public int ProgressPercent => ProgressMath.Percent(CompletedTaskCount, TotalTaskCount);
}

public class ProjectDetailTaskViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public TaskStatus Status { get; init; }
    public TaskPriority Priority { get; init; }
    public DateOnly DueDate { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}