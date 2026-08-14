using System.ComponentModel.DataAnnotations;
using TaskMangerLE.Models.Enums;

namespace TaskMangerLE.Models;

public class ProjectItem
{
    public int Id { get; set; }
    [MaxLength(70)] public string Name { get; set; } = "Untitled Project";
    [MaxLength(700)] public string Description { get; set; } = "No Description";
    public DateTime LastUpdated { get; set; }
    public DateTime CreatedDate { get; set; }
    public ProjectStatus Status { get; set; }

    [MaxLength(450)] public string OwnerId { get; set; } = string.Empty;
    public ApplicationUser? Owner { get; set; } = null;

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedByUserId { get; set; }
}