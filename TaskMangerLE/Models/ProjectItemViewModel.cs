using System.ComponentModel.DataAnnotations;
using TaskMangerLE.Models.Enums;

namespace TaskMangerLE.Models;

public abstract class ProjectItemViewModelBase
{
    [Required, MaxLength(ItemConstraints.NameMaxLength)]
    public string Name { get; set; }
    [Required, MaxLength(ItemConstraints.DescriptionMaxLength)]
    public string Description { get; set; }
    public ProjectStatus Status { get; set; }
}

public class ProjectItemCreateViewModel : ProjectItemViewModelBase
{
}

public class ProjectItemUpdateViewModel : ProjectItemViewModelBase
{
    public int Id { get; set; }
}