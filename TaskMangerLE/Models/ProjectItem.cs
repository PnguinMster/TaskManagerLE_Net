using System.ComponentModel.DataAnnotations;

namespace TaskMangerLE.Models;

public class ProjectItem
{
    public int Id { get; set; }
    [MaxLength(70)] public string Name { get; set; } = "Untitled Project";
    [MaxLength(700)] public string Description { get; set; } = "No Description";
    public DateTime LastUpdated { get; set; }
    public DateTime CreatedDate { get; set; }
    public ProjectStatus Status { get; set; }
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}

public enum ProjectStatus
{
    Planning,
    Active,
    OnHold,
    Completed,
    Cancelled
}