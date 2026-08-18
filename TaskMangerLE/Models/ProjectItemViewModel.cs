using System.ComponentModel.DataAnnotations;
using TaskMangerLE.Models.Enums;

namespace TaskMangerLE.Models;

public abstract class ProjectItemViewModelBase
{
    [MaxLength(70)] public string Name { get; set; } = "Untitled Project";
    [MaxLength(700)] public string Description { get; set; } = "No Description";
    public ProjectStatus Status { get; set; }
}

public class ProjectItemCreateViewModel : ProjectItemViewModelBase
{
}

public class ProjectItemUpdateViewModel : ProjectItemViewModelBase
{
    public int Id { get; set; }
    [MaxLength(450)] public string OwnerId { get; set; } = string.Empty;
}