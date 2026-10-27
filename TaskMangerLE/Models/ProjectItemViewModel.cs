using System.ComponentModel.DataAnnotations;
using TaskMangerLE.Models.Enums;

namespace TaskMangerLE.Models;

public abstract class ProjectItemViewModelBase
{
    [Required]
    [MaxLength(ItemConstraints.NameMaxLength)]
    public string Name { get; set; } = string.Empty;
    [DataType(DataType.MultilineText)]
    [MaxLength(ItemConstraints.DescriptionMaxLength)]
    public string Description { get; set; } = string.Empty;
    [EnumDataType(typeof(ProjectStatus))] public ProjectStatus Status { get; set; }
}

public class ProjectItemCreateViewModel : ProjectItemViewModelBase;

public class ProjectItemUpdateViewModel : ProjectItemViewModelBase
{
    public int Id { get; set; }
}