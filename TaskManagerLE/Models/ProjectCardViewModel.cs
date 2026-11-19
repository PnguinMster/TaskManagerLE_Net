using TaskManagerLE.Extensions;
using TaskManagerLE.Models.Enums;

namespace TaskManagerLE.Models;

public class ProjectCardViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public ProjectStatus Status { get; init; }
    public string Description { get; init; } = string.Empty;
    public int CompletedTaskCount { get; init; }
    public int TotalTaskCount { get; init; }
    public DateOnly DueDate { get; init; }

    public int ProgressPercent => ProgressMath.Percent(CompletedTaskCount, TotalTaskCount);

    public bool IsFinished => Status.IsFinished();

    public DueGroup GetDueGroup(DateOnly today) => DueDate.GetDueGroup(today, IsFinished);
}