using System.ComponentModel.DataAnnotations;
using TaskMangerLE.Models.Enums;
using TaskStatus = TaskMangerLE.Models.Enums.TaskStatus;

namespace TaskMangerLE.Models;

public abstract class TaskItemViewModelBase
{
    public int ProjectId { get; set; }
    [Required, MaxLength(ItemConstraints.NameMaxLength)]
    public string Name { get; set; }
    [Required, MaxLength(ItemConstraints.DescriptionMaxLength)]
    public string Description { get; set; }
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