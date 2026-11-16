using TaskMangerLE.Extensions;
using TaskMangerLE.Models.Enums;

namespace TaskMangerLE.Models;

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

    // "today" is passed in (views get it from TimeProvider.Today()) so the whole app agrees on it.
    public ProjectDueGroup GetDueGroup(DateOnly today)
    {
        if (IsFinished) return ProjectDueGroup.Finished;
        if (DueDate < today) return ProjectDueGroup.Overdue;
        if (DueDate == today) return ProjectDueGroup.Today;
        if (DueDate <= today.AddDays(7)) return ProjectDueGroup.ThisWeek;
        return ProjectDueGroup.Later;
    }
}