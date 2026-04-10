using task_tracker.Models;

namespace task_tracker.Store;

public class TaskStore
{
    private readonly List<TaskItem> _tasks;

    public TaskStore()
    {
        _tasks = new List<TaskItem>
        {
            new TaskItem
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Design database schema",
                Assignee = "Alice",
                Status = "Pending",
                Hours = 6,
                DueDate = "2026-05-01"
            },
            new TaskItem
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Implement login API",
                Assignee = "Bob",
                Status = "InProgress",
                Hours = 10,
                DueDate = "2026-04-25"
            },
            new TaskItem
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Write unit tests",
                Assignee = "Alice",
                Status = "Pending",
                Hours = 4,
                DueDate = "2026-05-10"
            },
            new TaskItem
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Set up CI/CD pipeline",
                Assignee = "Charlie",
                Status = "Done",
                Hours = 3,
                DueDate = "2026-04-15"
            },
            new TaskItem
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Create user dashboard",
                Assignee = "Bob",
                Status = "InProgress",
                Hours = 12,
                DueDate = "2026-05-20"
            }
        };
    }

    /// <summary>Returns all tasks in the store.</summary>
    public List<TaskItem> GetAll() => _tasks;

    /// <summary>Finds a task by its unique ID, or null if not found.</summary>
    public TaskItem? GetById(string id) =>
        _tasks.FirstOrDefault(t => t.Id == id);

    /// <summary>Creates a new task with an auto-generated ID.</summary>
    public TaskItem Create(TaskCreateRequest request)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid().ToString(),
            Title = request.Title!,
            Assignee = request.Assignee!,
            Status = request.Status!,
            Hours = request.Hours!.Value,
            DueDate = request.DueDate!
        };
        _tasks.Add(task);
        return task;
    }

    /// <summary>
    /// Partially updates an existing task. Only non-null fields in the
    /// request are applied. Returns the updated task or null if not found.
    /// </summary>
    public TaskItem? Update(string id, TaskUpdateRequest request)
    {
        var task = GetById(id);
        if (task == null) return null;

        if (request.Title != null) task.Title = request.Title;
        if (request.Assignee != null) task.Assignee = request.Assignee;
        if (request.Status != null) task.Status = request.Status;
        if (request.Hours.HasValue) task.Hours = request.Hours.Value;
        if (request.DueDate != null) task.DueDate = request.DueDate;

        return task;
    }

    /// <summary>
    /// Removes a task by ID. Returns the removed task or null if not found.
    /// </summary>
    public TaskItem? Delete(string id)
    {
        var task = GetById(id);
        if (task == null) return null;
        _tasks.Remove(task);
        return task;
    }

    /// <summary>
    /// Returns aggregated task statistics: total count, total hours,
    /// average hours, and a breakdown of tasks grouped by status.
    /// </summary>
    public TaskSummaryResponse GetSummary()
    {
        var total = _tasks.Count;
        var totalHours = _tasks.Sum(t => t.Hours);
        var avgHours = total > 0 ? (double)totalHours / total : 0.0;

        var byStatus = _tasks
            .GroupBy(t => t.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        return new TaskSummaryResponse
        {
            TotalTasks = total,
            TotalHours = totalHours,
            AverageHours = Math.Round(avgHours, 2),
            TasksByStatus = byStatus
        };
    }
}
