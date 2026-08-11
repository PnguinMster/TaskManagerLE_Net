using System.ComponentModel.DataAnnotations;
using TaskMangerLE.Models.Enums;
using TaskStatus = TaskMangerLE.Models.Enums.TaskStatus;

namespace TaskMangerLE.Models;

public abstract class TaskItemViewModelBase
{
    public int ProjectId { get; set; }
    [MaxLength(70)] public string Name { get; set; } = "Untitled Task";
    [MaxLength(700)] public string Description { get; set; } = "No Description";
    public TaskPriority Priority { get; set; }
    public TaskStatus Status { get; set; }
    public DateOnly DueDate { get; set; }
}

public class TaskItemCreateViewModel : TaskItemViewModelBase
{
}

public class TaskItemUpdateViewModel : TaskItemViewModelBase
{
    public int Id { get; set; }
}