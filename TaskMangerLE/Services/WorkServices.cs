using TaskMangerLE.Models;

namespace TaskMangerLE.Services;

public class WorkServices
{
    private static List<ProjectItem> _projectsList = [];

    public void AddProject(string name)
    {
        _projectsList.Add(new ProjectItem(name));
    }

    public void AddTask(ProjectItem proj, string name, string description)
    {
        proj.Tasks.Add(new TaskItem(name, description));
    }

    public void RenameWork(WorkItem workItem, string newName)
    {
        workItem.Name = newName;
    }

    public void ChangeDescription(TaskItem task, string newDescription)
    {
        task.Description = newDescription;
    }

    public void UpdateTaskStatus(TaskItem task, bool isCompleted)
    {
        task.IsCompleted = isCompleted;
    }

    public void DeleteProject(ProjectItem proj)
    {
        _projectsList.Remove(proj);
    }

    public void DeleteTask(ProjectItem proj, TaskItem task)
    {
        proj.Tasks.Remove(task);
    }
}