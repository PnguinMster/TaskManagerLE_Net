namespace TaskMangerLE.Models;

public class Task : Work
{
    public string Description = "Task Description";
    public bool IsCompleted;

    public Task()
    {
        Name = "New Task";
    }
}