using System.ComponentModel.DataAnnotations;
using TaskMangerLE.Models.Enums;
using TaskStatus = TaskMangerLE.Models.Enums.TaskStatus;

namespace TaskMangerLE.Models;

public abstract class TaskItemViewModelBase
{
    public int ProjectId { get; set; }
    // The UI calls this field "Title"; [Display] makes labels and default error messages say so too.
    [Display(Name = "Title")]
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

// Same as ProjectItemCreateViewModel: the rendering page sets DueDate to today
// (see Task/Index.cshtml and Project/Detail.cshtml).
public class TaskItemCreateViewModel : TaskItemViewModelBase;

public class TaskItemUpdateViewModel : TaskItemViewModelBase
{
    public int Id { get; set; }
}