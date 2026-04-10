using task_tracker.Models;
using task_tracker.Store;

namespace task_tracker.Handlers;

/// <summary>
/// Handler for operationId: TaskService_update
/// PATCH /{id} — Partially updates a task, or returns 404 if not found.
/// Validation is handled by OpenApiValidationMiddleware before this runs.
/// </summary>
public static class TaskServiceUpdate
{
    public static IResult Handle(string id, TaskUpdateRequest request, TaskStore store)
    {
        var task = store.Update(id, request);
        if (task == null)
        {
            return Results.NotFound(new ApiError { Message = $"Task with id '{id}' not found." });
        }
        return Results.Ok(task);
    }
}
