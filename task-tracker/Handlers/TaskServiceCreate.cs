using task_tracker.Models;
using task_tracker.Store;

namespace task_tracker.Handlers;

/// <summary>
/// Handler for operationId: TaskService_create
/// POST / — Creates a new task and returns it with a 201 status.
/// Validation is handled by OpenApiValidationMiddleware before this runs.
/// </summary>
public static class TaskServiceCreate
{
    public static IResult Handle(TaskCreateRequest request, TaskStore store)
    {
        var task = store.Create(request);
        return Results.Created($"/{task.Id}", task);
    }
}
