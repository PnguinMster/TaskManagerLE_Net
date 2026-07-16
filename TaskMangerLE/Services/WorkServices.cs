using System.Collections;
using TaskMangerLE.Models;

namespace TaskMangerLE.Services;

public class WorkServices
{
    private readonly List<ProjectItem> _projectsList = [];
    private int _nextId = 0;

    public IReadOnlyList<ProjectItem> GetAll() => _projectsList;

    public void AddProject(ProjectItem projectItem)
    {
        projectItem.Id = _nextId++;
        if (string.IsNullOrWhiteSpace(projectItem.Name))
            projectItem.Name = "Untitled Project";

        _projectsList.Add(projectItem);
    }

    public void AddTask(ProjectItem proj, TaskItem taskItem)
    {
        proj.Tasks.Add(taskItem);
    }

    public void RenameProject(ProjectItem projectItem)
    {
        ProjectItem? item = GetProjectById(projectItem.Id);

        item?.Name = projectItem.Name;
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

    // Temporary solution until database is implemented
    public ProjectItem? GetProjectById(int id)
    {
        return _projectsList.Find(x => x.Id == id);
    }
}