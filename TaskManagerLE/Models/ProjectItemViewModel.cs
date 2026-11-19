using System.ComponentModel.DataAnnotations;
using TaskManagerLE.Models.Enums;

namespace TaskManagerLE.Models;

public abstract class ProjectItemViewModelBase
{
    // The UI calls this field "Title"; [Display] makes labels and default error messages say so too.
    [Display(Name = "Title")]
    [Required(ErrorMessage = "Project title is required.")]
    [MaxLength(ItemConstraints.NameMaxLength)]
    public string Name { get; set; } = string.Empty;
    [DataType(DataType.MultilineText)]
    [MaxLength(ItemConstraints.DescriptionMaxLength)]
    // string? (not string) so MVC doesn't treat it as implicitly [Required]; the form says "Optional".
    public string? Description { get; set; }
    [Display(Name = "Due date")]
    public DateOnly DueDate { get; set; }
    [EnumDataType(typeof(ProjectStatus))] public ProjectStatus Status { get; set; }
}

// The page that renders the create modal sets DueDate to the injected TimeProvider's today
// (see Project/Index.cshtml), so the date input doesn't start at DateOnly's default (0001-01-01).
public class ProjectItemCreateViewModel : ProjectItemViewModelBase;

public class ProjectItemUpdateViewModel : ProjectItemViewModelBase
{
    public int Id { get; set; }
}
