using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    public DateTimeOffset DueDate { get; set; }

    public int ProjectId { get; set; }
    public ProjectItem ProjectItem { get; set; } = null!;
}

public enum TaskPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum TaskStatus
{
    ToDo,
    InProgress,
    Review,
    Done,
    Cancelled
}