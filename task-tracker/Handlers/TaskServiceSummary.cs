using task_tracker.Store;

namespace task_tracker.Handlers;

/// <summary>
/// Handler for operationId: TaskService_summary
/// GET /summary — Returns aggregated task statistics including total count,
/// total hours, average hours, and a per-status breakdown.
/// </summary>
public static class TaskServiceSummary
{
    public static IResult Handle(TaskStore store)
    {
        var summary = store.GetSummary();
        return Results.Ok(summary);
    }
}
