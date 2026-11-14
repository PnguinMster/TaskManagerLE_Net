using TaskMangerLE.Models.Enums;
using TaskStatus = TaskMangerLE.Models.Enums.TaskStatus;

namespace TaskMangerLE.Models;

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

    public int ProgressPercent =>
        TotalTaskCount == 0
            ? 0
            : (int)Math.Round(CompletedTaskCount * 100.0 / TotalTaskCount, MidpointRounding.AwayFromZero);
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