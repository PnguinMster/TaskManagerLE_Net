namespace TaskMangerLE.Models;

public abstract class WorkItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "New Work";
}