namespace TaskMangerLE.Models;

public class ProjectItem : WorkItem
{
    public List<TaskItem> Tasks { get; set; } = [];
}