namespace TaskManagerLE.Models.Enums;

// Where a project's or task's due date sits relative to today (see DueGroupExtensions.GetDueGroup).
// Used by the due badges, the Tasks page groups and the Due filters on the Projects and Tasks pages.
// site.js gets it lowercased (data-project-due / data-task-due), e.g. "thisweek".
public enum DueGroup
{
    Finished,   // no more work expected (see IsFinished): never overdue, never "due soon"
    Overdue,
    Today,
    ThisWeek,   // after today, up to 7 days ahead
    Later
}
