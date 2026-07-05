namespace TaskMangerLE.Models;

public class ProjectItem : WorkItem
{
    public List<TaskItem> Tasks { get; set; } = [];

    public ProjectItem(string name = "New Project")
    {
        Name = name;
    }
}