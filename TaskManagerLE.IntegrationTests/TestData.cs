using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagerLE.Data;
using TaskManagerLE.Models;

namespace TaskManagerLE.IntegrationTests;

// Shared setup and checks for the ownership tests. Each one uses its own DI scope, like a request
// would, so reads see what was actually saved instead of an entity cached by an earlier context.
public static class TestData
{
    // Users are real rows because ProjectItem.OwnerId is a foreign key to the users table.
    // The Guid keeps names unique across tests sharing the database.
    public static async Task<string> CreateUserAsync(this TestAppFactory factory, string name)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        string userName = $"{name}_{Guid.NewGuid():N}";
        var user = new ApplicationUser { UserName = userName, Email = $"{userName}@example.com" };
        IdentityResult result = await userManager.CreateAsync(user);
        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(e => e.Description)));

        return user.Id;
    }

    public static async Task<int> CreateProjectAsync(this TestAppFactory factory, string ownerId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var project = new ProjectItem { Name = "Test project", OwnerId = ownerId };
        context.ProjectItems.Add(project);
        await context.SaveChangesAsync();

        return project.Id;
    }

    public static async Task<int> CreateTaskAsync(this TestAppFactory factory, int projectId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var task = new TaskItem { Name = "Test task", ProjectId = projectId };
        context.TaskItems.Add(task);
        await context.SaveChangesAsync();

        return task.Id;
    }

    // IgnoreQueryFilters also finds soft-deleted rows, which the global filter would hide.
    public static async Task<ProjectItem> FindProjectAsync(this TestAppFactory factory, int projectId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.ProjectItems.IgnoreQueryFilters().AsNoTracking().SingleAsync(p => p.Id == projectId);
    }

    public static async Task<TaskItem> FindTaskAsync(this TestAppFactory factory, int taskId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.TaskItems.IgnoreQueryFilters().AsNoTracking().SingleAsync(t => t.Id == taskId);
    }
}
