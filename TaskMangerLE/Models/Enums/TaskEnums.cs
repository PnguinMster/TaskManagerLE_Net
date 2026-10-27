using System.ComponentModel.DataAnnotations;

namespace TaskMangerLE.Models.Enums;

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum TaskStatus
{
    ToDo = 0,
    [Display(Name = "In Progress")]
    InProgress = 1,
    Review = 2,
    Done = 3,
    Cancelled = 4
}