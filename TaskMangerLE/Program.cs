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

        if (inputKey.Key == ConsoleKey.D0) return MenuCode.None;

        if (inputKey.Key == ConsoleKey.D1) {
            // List all tasks
            return MenuCode.ListTasks;
        }

        if (inputKey.Key == ConsoleKey.D2) {
            // Add new task
        }

        return MenuCode.None;
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

    static MenuCode CurrentTaskMenu()
    {
        Console.WriteLine("Task: " + selectedTask.Name);
        Console.WriteLine("Description: " + selectedTask.Description);
        Console.WriteLine("Completed: " + selectedTask.IsCompleted);
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