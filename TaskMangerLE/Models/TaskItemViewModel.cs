using System.ComponentModel.DataAnnotations;
using TaskMangerLE.Models.Enums;
using TaskStatus = TaskMangerLE.Models.Enums.TaskStatus;

namespace TaskMangerLE.Models;

public abstract class TaskItemViewModelBase
{
    public int ProjectId { get; set; }
    [Required]
    [MaxLength(ItemConstraints.NameMaxLength)]
    public string Name { get; set; } = string.Empty;
    [DataType(DataType.MultilineText)]
    [MaxLength(ItemConstraints.DescriptionMaxLength)]
    public string Description { get; set; } = string.Empty;
    [EnumDataType(typeof(TaskPriority))] public TaskPriority Priority { get; set; }
    [EnumDataType(typeof(TaskStatus))] public TaskStatus Status { get; set; }
    [Required]
    [Display(Name = "Due Date")]
    public DateOnly? DueDate { get; set; }
}

public class TaskItemCreateViewModel : TaskItemViewModelBase;

public class TaskItemUpdateViewModel : TaskItemViewModelBase
{
    public int Id { get; set; }
}