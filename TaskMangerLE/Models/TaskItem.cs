namespace TaskMangerLE.Models;

public class TaskItem : WorkItem
{
    public string Description { get; set; }
    public bool IsCompleted { get; set; }

    public TaskItem(string name = "New Task", string description = "Task Description", bool isCompleted = false)
    {
        Name = name;
        Description = description;
        IsCompleted = isCompleted;
    }
}