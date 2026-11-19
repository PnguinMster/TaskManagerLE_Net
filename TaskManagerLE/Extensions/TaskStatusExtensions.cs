using TaskStatus = TaskManagerLE.Models.Enums.TaskStatus;

namespace TaskManagerLE.Extensions;

public static class TaskStatusExtensions
{
    // Finished = no more work expected: never overdue or "due soon", and left out of the default ("Open")
    // views. Same idea as ProjectStatusExtensions.IsFinished. Every page and query uses this one rule.
    // It's an array so EF Core queries can use it too: FinishedStatuses.Contains(t.Status) becomes SQL
    // "IN (...)", whereas a call to IsFinished() can't be translated to SQL.
    public static readonly TaskStatus[] FinishedStatuses = [TaskStatus.Done, TaskStatus.Cancelled];

    public static bool IsFinished(this TaskStatus status) => FinishedStatuses.Contains(status);
}
