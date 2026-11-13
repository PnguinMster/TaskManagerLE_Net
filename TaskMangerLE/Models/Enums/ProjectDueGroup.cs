namespace TaskMangerLE.Models.Enums;

// Where a project's due date sits relative to today. Used by the due badge and the Projects page filter.
public enum ProjectDueGroup
{
    Finished,   // Completed, Cancelled or Archived: never overdue, never "due soon"
    Overdue,
    Today,
    ThisWeek,   // after today, up to 7 days ahead (same window as the Tasks page)
    Later
}
