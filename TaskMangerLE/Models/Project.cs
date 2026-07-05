namespace TaskMangerLE.Models;

public class Project : Work
{
    public List<Task> Tasks { get; set; } = [];

    public Project()
    {
        Name = "New Project";
    }
}