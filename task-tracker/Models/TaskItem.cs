namespace task_tracker.Models;

/// <summary>
/// The full task resource as stored in the system.
/// Maps to components/schemas/Task in openapi.yaml.
/// </summary>
public class TaskItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Assignee { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Hours { get; set; }
    public string DueDate { get; set; } = string.Empty;
}

/// <summary>
/// Schema for creating a new task (all fields required).
/// Maps to components/schemas/TaskCreate in openapi.yaml.
/// </summary>
public class TaskCreateRequest
{
    public string? Title { get; set; }
    public string? Assignee { get; set; }
    public string? Status { get; set; }
    public int? Hours { get; set; }
    public string? DueDate { get; set; }
}

/// <summary>
/// Schema for partially updating a task (all fields optional).
/// Maps to components/schemas/TaskUpdate in openapi.yaml.
/// </summary>
public class TaskUpdateRequest
{
    public string? Title { get; set; }
    public string? Assignee { get; set; }
    public string? Status { get; set; }
    public int? Hours { get; set; }
    public string? DueDate { get; set; }
}

/// <summary>
/// Aggregated task statistics returned by the custom /summary operation.
/// Maps to components/schemas/TaskSummary in openapi.yaml.
/// </summary>
public class TaskSummaryResponse
{
    public int TotalTasks { get; set; }
    public int TotalHours { get; set; }
    public double AverageHours { get; set; }
    public Dictionary<string, int> TasksByStatus { get; set; } = new();
}

/// <summary>
/// Standard error response.
/// Maps to components/schemas/Error in openapi.yaml.
/// </summary>
public class ApiError
{
    public string Message { get; set; } = string.Empty;
}
