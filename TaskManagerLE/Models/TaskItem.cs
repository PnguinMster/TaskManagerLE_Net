using TaskManagerLE.Models.Enums;
using TaskStatus = TaskManagerLE.Models.Enums.TaskStatus;

namespace TaskManagerLE.Models;

public class TaskItem : ItemBase
{
    public TaskPriority Priority { get; set; }
    public TaskStatus Status { get; set; }

    public int ProjectId { get; set; }
    public ProjectItem Project { get; set; } = null!;
}