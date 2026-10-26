using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskMangerLE.Models;

namespace TaskMangerLE.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<ProjectItem> ProjectItems { get; set; }
    public DbSet<TaskItem> TaskItems { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ProjectItem>().HasQueryFilter(p => !p.IsDeleted);
        builder.Entity<TaskItem>().HasQueryFilter(t => !t.IsDeleted);
        builder.Entity<TaskItem>().HasOne(t => t.ProjectItem).WithMany(p => p.Tasks).HasForeignKey(t => t.ProjectId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        foreach (var entityEntry in ChangeTracker.Entries<ItemBase>()) {
            if (entityEntry.State == EntityState.Added) {
                entityEntry.Entity.CreatedAtUtc = now;
                entityEntry.Entity.UpdatedAttUtc = now;
            }
            else if (entityEntry.State == EntityState.Modified)
                entityEntry.Entity.UpdatedAttUtc = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}