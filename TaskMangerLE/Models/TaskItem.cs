namespace TaskMangerLE.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsCompleted { get; set; }

    public int ProjectId { get; set; }
    public ProjectItem ProjectItem { get; set; } = null!;
}