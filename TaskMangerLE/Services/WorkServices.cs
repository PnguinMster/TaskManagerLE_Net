using TaskMangerLE.Models;

namespace TaskMangerLE.Services;

public class WorkServices
{
    private readonly List<ProjectItem> _projectsList = [];

    public IReadOnlyList<ProjectItem> GetAll() => _projectsList;

    public void AddProject(ProjectItem projectItem)
    {
        if (string.IsNullOrWhiteSpace(projectItem.Name))
            projectItem.Name = "Untitled Project";

        _projectsList.Add(projectItem);
    }

    public void AddTask(ProjectItem proj, TaskItem taskItem)
    {
        proj.Tasks.Add(taskItem);
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