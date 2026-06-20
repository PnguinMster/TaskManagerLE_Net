using System.ComponentModel;

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
    }

    static List<Project> projectsList = new List<Project>();
    private static Project selectedProject;
    private static Task selectedTask;


    private const int ItemsPerPage = 7;

    static void Main()
    {
        // Load Data
        RunProgram();
        // Save Data
    }

    static void RunProgram()
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
                case MenuCode.None:
                default:
                    return;
            }
        }
    }

// return code == ExitCode.Back ? ExitCode.None : code;
    static MenuCode StartMenu()
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

    static MenuCode ListProjectMenu()
    {
        // Check if empty
        if (projectsList.Count == 0) {
            Console.WriteLine("You have no projects to list. Please create a new project");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            Console.WriteLine();
            return MenuCode.Start;
        }

        // List all projects
        for (int i = 0; i < projectsList.Count; i++) {
            Console.WriteLine((i + 1) + ". " + projectsList[i].Name);

            // Limit page
            if (i % ItemsPerPage == 0 || i == projectsList.Count - 1) {
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

                int inputIndex = i - (ItemsPerPage - inputKey.KeyChar);

                if (projectsList[inputIndex].Tasks.Count == 0) {
                    Console.WriteLine("You have no tasks in the project to list. Please create a new task");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    continue;
                }

                selectedProject = projectsList[inputIndex];
                return MenuCode.CurrentProject;
            }
        }

        return MenuCode.None;
    }

    static MenuCode CreateProjectMenu()
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

        projectsList.Add(newProject);

        selectedProject = newProject;
        return MenuCode.CurrentProject;
    }

    static MenuCode CurrentProjectMenu()
    {
        // After creation menu
        Console.WriteLine("Current Project: " + selectedProject.Name);
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
            case ConsoleKey.D0:
            default:
                return MenuCode.None;
        }
    }

    static MenuCode ListTasksMenu()
    {
        // Check if empty
        if (selectedProject.Tasks.Count == 0) {
            Console.WriteLine("You have no projects to list. Please create a new project");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            Console.WriteLine();
            return MenuCode.CurrentProject;
        }

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

                int inputIndex = i - (ItemsPerPage - inputKey.KeyChar);

                selectedTask = selectedProject.Tasks[inputIndex];
                return MenuCode.CurrentTask;
            }
        }

        return MenuCode.None;
    }

    static MenuCode CreateTaskMenu()
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

        selectedProject.Tasks.Add(newTask);

        selectedTask = newTask;
        return MenuCode.CurrentTask;
    }

    static MenuCode CurrentTaskMenu()
    {
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
                Console.WriteLine("Input new task name");
                Console.WriteLine(new string('-', 20));
                Console.WriteLine("Enter nothing, to go back");
                string? newTaskName = Console.ReadLine();

                if (string.IsNullOrEmpty(newTaskName)) return MenuCode.CurrentTask;

                selectedTask.Name = newTaskName;
                return MenuCode.CurrentTask;
            case ConsoleKey.D2:
                Console.WriteLine("Input new task description");
                Console.WriteLine(new string('-', 20));
                Console.WriteLine("Enter nothing, to go back");
                string? newTaskDescription = Console.ReadLine();

                if (string.IsNullOrEmpty(newTaskDescription)) return MenuCode.CurrentTask;

                selectedTask.Description = newTaskDescription;
                return MenuCode.CurrentTask;
            case ConsoleKey.D3:
                Console.WriteLine("1. Completed");
                Console.WriteLine("2. Not Completed");
                inputKey = Console.ReadKey();

                if (inputKey.Key == ConsoleKey.D1)
                    selectedTask.IsCompleted = true;
                else if (inputKey.Key == ConsoleKey.D2)
                    selectedTask.IsCompleted = false;

                return MenuCode.CurrentTask;
            case ConsoleKey.D0:
            default:
                return MenuCode.None;
        }


        return MenuCode.None;
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