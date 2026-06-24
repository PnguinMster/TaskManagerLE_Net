using System.Text.Json;

namespace TaskMangerLE;

class Program
{
    enum MenuCode : byte
    {
        None,
        Start,
        ListProjects,
        CreateProject,
        CurrentProject,
        ListTasks,
        CreateTask,
        CurrentTask,
        RenameTask,
        TaskDescription,
        TaskStatus
    }

    private static List<Project> _projectsList = [];
    private static int _selectedProjectIndex;
    private static int _selectedTaskIndex;

    private const int ItemsPerPage = 7;
    private const string FileName = "projects.json";

    private static void Main()
    {
        LoadData();
        RunProgram();
        SaveDate();
    }

    private static void RunProgram()
    {
        MenuCode menuCode = MenuCode.Start;

        while (true) {
            switch (menuCode) {
                case MenuCode.Start:
                    menuCode = StartMenu();
                    break;
                case MenuCode.ListProjects:
                    menuCode = ListProjectMenu();
                    break;
                case MenuCode.CreateProject:
                    menuCode = CreateProjectMenu();
                    break;
                case MenuCode.CurrentProject:
                    menuCode = CurrentProjectMenu();
                    break;
                case MenuCode.ListTasks:
                    menuCode = ListTasksMenu();
                    break;
                case MenuCode.CreateTask:
                    menuCode = CreateTaskMenu();
                    break;
                case MenuCode.CurrentTask:
                    menuCode = CurrentTaskMenu();
                    break;
                case MenuCode.RenameTask:
                    menuCode = RenameTask();
                    break;
                case MenuCode.TaskDescription:
                    menuCode = ChangeTaskDescription();
                    break;
                case MenuCode.TaskStatus:
                    menuCode = UpdateTaskStatus();
                    break;
                case MenuCode.None:
                default:
                    return;
            }

            Console.Clear();
        }
    }

    private static void SaveDate()
    {
        if (_projectsList.Count == 0) return;

        string jsonString = JsonSerializer.Serialize(_projectsList);
        File.WriteAllText(FileName, jsonString);
    }

    private static void LoadData()
    {
        if (_projectsList.Count == 0) return;

        string jsonString = File.ReadAllText(FileName);
        _projectsList = JsonSerializer.Deserialize<List<Project>>(jsonString) ?? [];
    }

    private static MenuCode StartMenu()
    {
        // Initial Menu
        Console.WriteLine("Task Manager");
        Console.WriteLine(new string('-', 20));
        Console.WriteLine("1. List all projects");
        Console.WriteLine("2. Create new project");
        Console.WriteLine("0. Exit");

        // Get Input
        ConsoleKeyInfo inputKey = Console.ReadKey();
        Console.WriteLine();

        // Option logic
        if (inputKey.Key == ConsoleKey.D1) return MenuCode.ListProjects;
        if (inputKey.Key == ConsoleKey.D2) return MenuCode.CreateProject;
        return MenuCode.None;
    }

    private static MenuCode ListProjectMenu()
    {
        // Check if empty
        if (_projectsList.Count == 0) {
            Console.WriteLine("You have no projects to list. Please create a new project");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            Console.WriteLine();
            return MenuCode.Start;
        }

        Console.WriteLine("Projects List");
        Console.WriteLine(new string('-', 20));

        // List all projects
        for (int i = 0; i < _projectsList.Count; i++) {
            Console.WriteLine((i + 1) + ". " + _projectsList[i].Name);

            // Limit page
            if (i % ItemsPerPage == 0 || i == _projectsList.Count - 1) {
                if (i / ItemsPerPage > 1 && i > ItemsPerPage)
                    Console.WriteLine("8. Previous Page");
                Console.WriteLine("9. Back");
                Console.WriteLine("0. Exit");

                ConsoleKeyInfo inputKey = Console.ReadKey();

                if (inputKey.Key == ConsoleKey.D0) return MenuCode.None;

                if (inputKey.Key == ConsoleKey.D8) {
                    i -= ItemsPerPage;
                    continue;
                }

                if (inputKey.Key == ConsoleKey.D9) return MenuCode.Start;

                // (i / ItemsPerPage * ItemsPerPage) round down to nearest page step
                // (inputKey.KeyChar - 1) convert to index range
                int inputIndex = (i / ItemsPerPage * ItemsPerPage) + (inputKey.KeyChar - 1);

                if (_projectsList[inputIndex].Tasks.Count == 0) {
                    Console.WriteLine("You have no tasks in the project to list. Please create a new task");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    continue;
                }

                _selectedProjectIndex = inputIndex;
                return MenuCode.CurrentProject;
            }
        }

        return MenuCode.None;
    }

    private static MenuCode CreateProjectMenu()
    {
        // Selected create new project 
        Console.WriteLine("Input new project name");
        Console.WriteLine(new string('-', 20));
        Console.WriteLine("Enter nothing, to go back");
        string? newProjectName = Console.ReadLine();

        if (string.IsNullOrEmpty(newProjectName)) return MenuCode.Start;

        // Create new project item
        Project newProject = new Project
        {
            Name = newProjectName
        };

        _selectedProjectIndex = _projectsList.Count;
        _projectsList.Add(newProject);

        return MenuCode.CurrentProject;
    }

    private static MenuCode CurrentProjectMenu()
    {
        // After creation menu
        Console.WriteLine("Current Project: " + _projectsList[_selectedProjectIndex].Name);
        Console.WriteLine(new string('-', 20));
        Console.WriteLine("1. Lists Tasks");
        Console.WriteLine("2. Add new task");
        Console.WriteLine("9. Back");
        Console.WriteLine("0. Exit");

        ConsoleKeyInfo inputKey = Console.ReadKey();
        Console.WriteLine();

        // Decide menu
        switch (inputKey.Key) {
            case ConsoleKey.D1:
                return MenuCode.ListTasks;
            case ConsoleKey.D2:
                return MenuCode.CreateTask;
            case ConsoleKey.D9:
                return MenuCode.Start;
            case ConsoleKey.D0:
            default:
                return MenuCode.None;
        }
    }

    private static MenuCode ListTasksMenu()
    {
        Project selectedProject = _projectsList[_selectedProjectIndex];

        // Check if empty
        if (selectedProject.Tasks.Count == 0) {
            Console.WriteLine("You have no projects to list. Please create a new project");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            Console.WriteLine();
            return MenuCode.CurrentProject;
        }

        Console.WriteLine("Tasks List");
        Console.WriteLine(new string('-', 20));

        // List tasks
        for (int i = 0; i < selectedProject.Tasks.Count; i++) {
            Console.WriteLine((i + 1) + ". " + selectedProject.Tasks[i].Name);

            // Limit page
            if (i % ItemsPerPage == 0 || i == selectedProject.Tasks.Count - 1) {
                if (i / ItemsPerPage > 1)
                    Console.WriteLine("9. Back");
                Console.WriteLine("0. Exit");

                ConsoleKeyInfo inputKey = Console.ReadKey();

                if (inputKey.Key == ConsoleKey.D0) return MenuCode.None;

                if (inputKey.Key == ConsoleKey.D9) {
                    i -= ItemsPerPage;
                    continue;
                }

                // (i / ItemsPerPage * ItemsPerPage) round down to nearest page step
                // (inputKey.KeyChar - 1) convert to index range
                int inputIndex = (i / ItemsPerPage * ItemsPerPage) + (inputKey.KeyChar - 1);

                _selectedTaskIndex = inputIndex;
                return MenuCode.CurrentTask;
            }
        }

        return MenuCode.None;
    }

    private static MenuCode CreateTaskMenu()
    {
        // create new task
        // get task name
        Console.WriteLine("Input new task name");
        Console.WriteLine(new string('-', 20));
        Console.WriteLine("Enter nothing, to go back");
        string? newTaskName = Console.ReadLine();

        // get task description
        Console.WriteLine("Input new task description");
        Console.WriteLine(new string('-', 20));
        string? newTaskDescription = Console.ReadLine();

        if (string.IsNullOrEmpty(newTaskName)) return MenuCode.CurrentProject;
        newTaskDescription ??= "";

        // Create new project item
        Task newTask = new Task()
        {
            Name = newTaskName,
            Description = newTaskDescription
        };

        Project selectedProject = _projectsList[_selectedProjectIndex];
        _selectedTaskIndex = selectedProject.Tasks.Count;
        selectedProject.Tasks.Add(newTask);

        return MenuCode.CurrentTask;
    }

    private static MenuCode CurrentTaskMenu()
    {
        Task selectedTask = _projectsList[_selectedProjectIndex].Tasks[_selectedTaskIndex];

        // Task info
        Console.WriteLine("Task: " + selectedTask.Name);
        Console.WriteLine("Description: " + selectedTask.Description);
        Console.WriteLine("Completed: " + selectedTask.IsCompleted);
        Console.WriteLine(new string('-', 20));

        //  Options
        Console.WriteLine("1. Rename Task");
        Console.WriteLine("2. Change Description");
        Console.WriteLine("3. Change Status");
        Console.WriteLine("9. Back");
        Console.WriteLine("0. Exit");

        ConsoleKeyInfo inputKey = Console.ReadKey();
        Console.WriteLine();

        switch (inputKey.Key) {
            case ConsoleKey.D1:
                return MenuCode.RenameTask;
            case ConsoleKey.D2:
                return MenuCode.TaskDescription;
            case ConsoleKey.D3:
                return MenuCode.TaskStatus;
            case ConsoleKey.D9:
                return MenuCode.CurrentTask;
            case ConsoleKey.D0:
            default:
                return MenuCode.None;
        }
    }

    private static MenuCode RenameTask()
    {
        Task selectedTask = _projectsList[_selectedProjectIndex].Tasks[_selectedTaskIndex];

        // Print
        Console.WriteLine("Input new task name");
        Console.WriteLine(new string('-', 20));
        Console.WriteLine("Enter nothing, to go back");
        string? newTaskName = Console.ReadLine();

        if (string.IsNullOrEmpty(newTaskName)) return MenuCode.CurrentTask;

        selectedTask.Name = newTaskName;
        return MenuCode.CurrentTask;
    }

    private static MenuCode ChangeTaskDescription()
    {
        Task selectedTask = _projectsList[_selectedProjectIndex].Tasks[_selectedTaskIndex];

        // Print
        Console.WriteLine("Input new task description");
        Console.WriteLine(new string('-', 20));
        Console.WriteLine("Enter nothing, to go back");
        string? newTaskDescription = Console.ReadLine();

        if (string.IsNullOrEmpty(newTaskDescription)) return MenuCode.CurrentTask;

        selectedTask.Description = newTaskDescription;
        return MenuCode.CurrentTask;
    }

    private static MenuCode UpdateTaskStatus()
    {
        Task selectedTask = _projectsList[_selectedProjectIndex].Tasks[_selectedTaskIndex];

        //Print
        Console.WriteLine("Select task new status");
        Console.WriteLine(new string('-', 20));
        Console.WriteLine("1. Completed");
        Console.WriteLine("2. Not Completed");
        ConsoleKeyInfo inputKey = Console.ReadKey();

        if (inputKey.Key == ConsoleKey.D1)
            selectedTask.IsCompleted = true;
        else if (inputKey.Key == ConsoleKey.D2)
            selectedTask.IsCompleted = false;

        return MenuCode.CurrentTask;
    }
}

class Project
{
    public string Name = "New Project";
    public List<Task> Tasks = [];
}

class Task
{
    public string Name = "New Task";
    public string Description = "Task Description";
    public bool IsCompleted;
}