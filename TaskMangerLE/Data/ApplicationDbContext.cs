using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskMangerLE.Models;

namespace TaskMangerLE.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext
{
    public DbSet<ProjectItem> ProjectItems { get; set; }
    public DbSet<TaskItem> TaskItems { get; set; }
}