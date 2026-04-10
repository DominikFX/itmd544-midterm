using task_tracker.Models;
using task_tracker.Store;

namespace task_tracker.Handlers;

/// <summary>
/// Handler for operationId: TaskService_delete
/// DELETE /{id} — Removes a task, or returns 404 if not found.
/// </summary>
public static class TaskServiceDelete
{
    public static IResult Handle(string id, TaskStore store)
    {
        var task = store.Delete(id);
        if (task == null)
        {
            return Results.NotFound(new ApiError { Message = $"Task with id '{id}' not found." });
        }
        return Results.Ok(task);
    }
}
