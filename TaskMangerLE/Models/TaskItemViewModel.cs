using System.ComponentModel.DataAnnotations;
using TaskMangerLE.Extensions;
using TaskMangerLE.Models.Enums;
using TaskStatus = TaskMangerLE.Models.Enums.TaskStatus;

namespace TaskMangerLE.Models;

public abstract class TaskItemViewModelBase
{
    public int ProjectId { get; set; }
    [Required(ErrorMessage = "Task title is required.")]
    [MaxLength(ItemConstraints.NameMaxLength)]
    public string Name { get; set; } = string.Empty;
    [DataType(DataType.MultilineText)]
    [MaxLength(ItemConstraints.DescriptionMaxLength)]
    // string? (not string) so MVC doesn't treat it as implicitly [Required]; the form says "Optional".
    public string? Description { get; set; }
    [EnumDataType(typeof(TaskPriority))] public TaskPriority Priority { get; set; }
    [EnumDataType(typeof(TaskStatus))] public TaskStatus Status { get; set; }
    [Display(Name = "Due date")]
    public DateOnly DueDate { get; set; }
}

public class TaskItemCreateViewModel : TaskItemViewModelBase
{
    // Same as ProjectItemCreateViewModel: the server's today is a fallback for when JavaScript is off.
    // site.js replaces it with the browser's date, and model binding overwrites it with what the user submits.
    public TaskItemCreateViewModel()
    {
        DueDate = TimeProvider.System.Today();
    }
}

public class TaskItemUpdateViewModel : TaskItemViewModelBase
{
    public int Id { get; set; }
}