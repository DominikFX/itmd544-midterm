using Microsoft.EntityFrameworkCore;
using task_tracker.Models;

namespace task_tracker.Store;

/// <summary>
/// Entity Framework Core database context for the Task Tracker.
/// Maps TaskItem entities to the "Tasks" table in Azure SQL.
/// </summary>
public class TaskDbContext : DbContext
{
    public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options) { }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("Tasks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Assignee).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.DueDate).IsRequired().HasMaxLength(10);
        });
    }
}
