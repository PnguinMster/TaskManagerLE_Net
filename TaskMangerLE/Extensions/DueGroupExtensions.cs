using TaskMangerLE.Models.Enums;

namespace TaskMangerLE.Extensions;

public static class DueGroupExtensions
{
    // The one due-date rule for projects and tasks. "today" is passed in (views get it from
    // TimeProvider.Today()) so the whole app agrees on it.
    public static DueGroup GetDueGroup(this DateOnly dueDate, DateOnly today, bool isFinished)
    {
        if (isFinished) return DueGroup.Finished;
        if (dueDate < today) return DueGroup.Overdue;
        if (dueDate == today) return DueGroup.Today;
        if (dueDate <= today.AddDays(7)) return DueGroup.ThisWeek;
        return DueGroup.Later;
    }
}
