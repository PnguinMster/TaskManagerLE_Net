namespace TaskMangerLE;

class Program
{
    static List<Project> projectsList = new List<Project>();

    static void Main()
    {
        while (true) {
            StartMenu();
        }
    }

    static void StartMenu()
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
        if (inputKey.Key == ConsoleKey.D1)
            ListProjectMenu();
        else
            CreateProjectMenu();
    }

    static void ListProjectMenu()
    {
        // Check if empty
        if (projectsList.Count == 0) {
            Console.WriteLine("You have no projects to list. Please create a new project");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            Console.WriteLine();
            return;
        }

        // List all projects
        for (int i = 0; i < projectsList.Count; i++) {
            Console.WriteLine((i + 1) + ". " + projectsList[i].Name);

            // Limit page
            if (i % 8 == 0 || i == projectsList.Count - 1) {
                if (i / 8 > 1)
                    Console.WriteLine("9. Back");
                Console.WriteLine("0. Exit");

                ConsoleKeyInfo inputKey = Console.ReadKey();

                if (inputKey.Key == ConsoleKey.D9) i -= 8;

                int inputIndex = inputKey.KeyChar - 1;

                if (projectsList[inputIndex].Tasks.Count == 0) {
                    Console.WriteLine("You have no tasks in the project to list. Please create a new task");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    continue;
                }

                CurrentProjectMenu(projectsList[inputIndex]);
            }
        }
    }

    static void CreateProjectMenu()
    {
        // Selected create new project 
        Console.WriteLine("Input new project name");
        Console.WriteLine(new string('-', 20));
        Console.WriteLine("Enter nothing, to go back");
        string? newProjectName = Console.ReadLine();

        if (newProjectName == "") return;

        // Create new project item
        Project newProject = new Project
        {
            Name = newProjectName
        };

        projectsList.Add(newProject);

        CurrentProjectMenu(newProject);
    }

    static void CurrentProjectMenu(Project projectSelected)
    {
        // After creation menu
        Console.WriteLine("Current Project: " + projectSelected);
        Console.WriteLine(new string('-', 20));
        Console.WriteLine("1. Lists Tasks");
        Console.WriteLine("2. Add new task");
        Console.WriteLine("9. Go Back");
        Console.WriteLine("0. Exit");

        ConsoleKeyInfo inputKey = Console.ReadKey();

        if (inputKey.Key == ConsoleKey.D1) {
            // List all tasks
        }
        else if (inputKey.Key == ConsoleKey.D2) {
            // Add new task
        }
        else if (inputKey.Key == ConsoleKey.D9) {
            ListProjectMenu();
        }
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