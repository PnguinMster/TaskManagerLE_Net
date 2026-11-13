using System.ComponentModel.DataAnnotations;
using TaskMangerLE.Extensions;
using TaskMangerLE.Models.Enums;

namespace TaskMangerLE.Models;

public abstract class ProjectItemViewModelBase
{
    [Required(ErrorMessage = "Project title is required.")]
    [MaxLength(ItemConstraints.NameMaxLength)]
    public string Name { get; set; } = string.Empty;
    [DataType(DataType.MultilineText)]
    [MaxLength(ItemConstraints.DescriptionMaxLength)]
    // string? (not string) so MVC doesn't treat it as implicitly [Required]; the form says "Optional".
    public string? Description { get; set; }
    public DateOnly DueDate { get; set; }
    [EnumDataType(typeof(ProjectStatus))] public ProjectStatus Status { get; set; }
}

public class ProjectItemCreateViewModel : ProjectItemViewModelBase
{
    // Fallback for when JavaScript is off: pre-fill the server's today so the date input doesn't show
    // DateOnly's default (0001-01-01). site.js replaces it with the browser's date, and model binding
    // overwrites it with whatever the user submits.
    public ProjectItemCreateViewModel()
    {
        DueDate = TimeProvider.System.Today();
    }
}

public class ProjectItemUpdateViewModel : ProjectItemViewModelBase
{
    public int Id { get; set; }
}