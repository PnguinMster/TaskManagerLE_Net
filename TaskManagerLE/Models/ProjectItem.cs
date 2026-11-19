using System.ComponentModel.DataAnnotations;
using TaskManagerLE.Models.Enums;

namespace TaskManagerLE.Models;

public class ProjectItem : ItemBase
{
    public ProjectStatus Status { get; set; }

    [MaxLength(ItemConstraints.UserIdMaxLength)]
    public string OwnerId { get; set; } = string.Empty;
    public ApplicationUser Owner { get; set; } = null!;

    public ICollection<TaskItem> Tasks { get; } = new List<TaskItem>();
}
