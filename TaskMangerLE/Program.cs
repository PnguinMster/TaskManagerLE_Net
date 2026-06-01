namespace TaskMangerLE;

class Program
{
    static void Main()
    {
        List<Project> projects = [];

        ConsoleKeyInfo inputKey = new ConsoleKeyInfo();

        while (inputKey.Key != ConsoleKey.D0) {
            // Initial Menu
            Console.WriteLine("Task Manager");
            Console.WriteLine(new string('-', 20));
            Console.WriteLine("1. List all projects");
            Console.WriteLine("2. Create new project");
            Console.WriteLine("0. Exit");

            // Get Input
            inputKey = Console.ReadKey();

            // Option logic
            if (inputKey.Key == ConsoleKey.D1) {
                // Selected list all projects

                if (projects.Count == 0) {
                    Console.WriteLine("You have no projects to list. Please create a new one");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    continue;
                }

                // List all projects
                for (int i = 0; i < projects.Count; i++) {
                    Console.WriteLine(i + ". " + projects[i].Name);

                    // Limit page
                    if (i % 8 == 0 || i == projects.Count - 1) {
                        if (i / 8 > 1)
                            Console.WriteLine("9. Back");
                        Console.WriteLine("0. Exit");
                    }
                }
            }
            else if (inputKey.Key == ConsoleKey.D2) {
                // Selected create new project 
                Console.WriteLine("Input new project name");
                Console.WriteLine(new string('-', 20));
                Console.WriteLine("Enter nothing, to go back");
                string? newProjectName = Console.ReadLine();

                if (newProjectName == "")  continue; 

                // Create new project item
                Project newProject = new Project
                {
                    Name = newProjectName
                };

                projects.Add(newProject);

                // After creation menu
                Console.WriteLine("Current Project: " + newProjectName);
                Console.WriteLine(new string('-', 20));
                Console.WriteLine("1. Add new task");
                Console.WriteLine("2. Go Back");
                Console.WriteLine("0. Exit");

                inputKey = Console.ReadKey();
            }
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