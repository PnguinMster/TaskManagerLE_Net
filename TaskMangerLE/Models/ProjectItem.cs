using System.ComponentModel.DataAnnotations;
using TaskMangerLE.Models.Enums;

namespace TaskMangerLE.Models;

public class ProjectItem : ItemBase
{
    public ProjectStatus Status { get; set; }

    [MaxLength(ItemConstraints.UserIdMaxLength)]
    public required string OwnerId { get; init; }
    public ApplicationUser Owner { get; set; } = null!;

    public ICollection<TaskItem> Tasks { get; } = new List<TaskItem>();
}