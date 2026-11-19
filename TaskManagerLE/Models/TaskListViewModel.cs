using TaskManagerLE.Extensions;
using TaskManagerLE.Models.Enums;
using TaskStatus = TaskManagerLE.Models.Enums.TaskStatus;

namespace TaskManagerLE.Models;

public class TaskListViewModel
{
    public int TotalTaskCount { get; init; }
    public int ProjectCount { get; init; }
    public bool ShowArchived { get; init; }

    public IReadOnlyList<TaskListItemViewModel> Tasks { get; init; } = [];
}

public class TaskListItemViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public int ProjectId { get; init; }
    public TaskStatus Status { get; init; }
    public TaskPriority Priority { get; init; }
    public DateOnly DueDate { get; init; }

    // Same rule as ProjectCardViewModel.GetDueGroup.
    public DueGroup GetDueGroup(DateOnly today) => DueDate.GetDueGroup(today, Status.IsFinished());
}
