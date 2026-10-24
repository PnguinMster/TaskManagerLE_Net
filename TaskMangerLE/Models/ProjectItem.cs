using System.ComponentModel.DataAnnotations;
using TaskMangerLE.Models.Enums;

namespace TaskMangerLE.Models;

public class ProjectItem : ItemBase
{
    public ProjectStatus Status { get; set; }

    [MaxLength(450)] public string OwnerId { get; set; } = string.Empty;
    public ApplicationUser? Owner { get; set; } = null;

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}