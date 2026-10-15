using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        b.Entity<User>().Property(u => u.Email).HasMaxLength(256);
        b.Entity<Project>().Property(p => p.Name).HasMaxLength(100);
        b.Entity<Project>().HasOne(p => p.Owner).WithMany(u => u.Projects).HasForeignKey(p => p.OwnerId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<TaskItem>().Property(t => t.Title).HasMaxLength(200);
        b.Entity<TaskItem>().HasOne(t => t.Project).WithMany(p => p.Tasks).HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}

public static class DbSeeder
{
    public const string DemoEmail = "demo@taskflow.dev";
    public const string DemoPassword = "Demo@12345";

    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        await db.Database.EnsureCreatedAsync();
        if (await db.Users.AnyAsync()) return;

        var user = new User { Email = DemoEmail, DisplayName = "Demo User" };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, DemoPassword);
        var p1 = new Project { Name = "Portfolio Website", Description = "Personal site revamp", Owner = user };
        var p2 = new Project { Name = "Learning .NET", Description = "Study plan", Owner = user };
        p1.Tasks.AddRange([
            new TaskItem { Title = "Design landing page", Priority = TaskPriority.High, Status = TaskState.InProgress, DueDate = DateTime.UtcNow.AddDays(3) },
            new TaskItem { Title = "Write About section", Priority = TaskPriority.Medium },
            new TaskItem { Title = "Buy domain", Priority = TaskPriority.Low, Status = TaskState.Done }
        ]);
        p2.Tasks.AddRange([
            new TaskItem { Title = "Read EF Core docs", Priority = TaskPriority.Medium, DueDate = DateTime.UtcNow.AddDays(7) },
            new TaskItem { Title = "Build TaskFlow API", Priority = TaskPriority.High, Status = TaskState.InProgress }
        ]);
        db.AddRange(user, p1, p2);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded demo user {Email}", DemoEmail);
    }
}
