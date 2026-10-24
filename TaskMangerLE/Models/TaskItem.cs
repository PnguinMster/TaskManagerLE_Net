using TaskMangerLE.Models.Enums;
using TaskStatus = TaskMangerLE.Models.Enums.TaskStatus;

namespace TaskMangerLE.Models;

public class TaskItem : ItemBase
{
    public TaskPriority Priority { get; set; }
    public TaskStatus Status { get; set; }
    public DateOnly DueDate { get; set; }

    public int ProjectId { get; set; }
    public ProjectItem ProjectItem { get; set; } = null!;
}