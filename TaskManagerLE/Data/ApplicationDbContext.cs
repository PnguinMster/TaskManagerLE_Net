using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskManagerLE.Models;

namespace TaskManagerLE.Data;

// TimeProvider comes from DI like any service: AddDbContext builds the context through the container.
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, TimeProvider timeProvider)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<ProjectItem> ProjectItems { get; set; }
    public DbSet<TaskItem> TaskItems { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ProjectItem>().HasQueryFilter(p => !p.IsDeleted);
        builder.Entity<TaskItem>().HasQueryFilter(t => !t.IsDeleted);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var entityEntry in ChangeTracker.Entries<ItemBase>()) {
            if (entityEntry.State == EntityState.Added) {
                entityEntry.Entity.CreatedAtUtc = now;
                entityEntry.Entity.UpdatedAtUtc = now;
            }
            else if (entityEntry.State == EntityState.Modified)
                entityEntry.Entity.UpdatedAtUtc = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}