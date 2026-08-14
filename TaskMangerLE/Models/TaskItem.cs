using System.ComponentModel.DataAnnotations;
using TaskMangerLE.Models.Enums;
using TaskStatus = TaskMangerLE.Models.Enums.TaskStatus;

namespace TaskMangerLE.Models;

public class TaskItem
{
    public int Id { get; set; }
    [MaxLength(70)] public string Name { get; set; } = "Untitled Task";
    [MaxLength(700)] public string Description { get; set; } = "No Description";
    public TaskPriority Priority { get; set; }
    public TaskStatus Status { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateOnly DueDate { get; set; }

    public int ProjectId { get; set; }
    public ProjectItem? ProjectItem { get; set; } = null!;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedByUserId { get; set; }
}