using task_tracker.Models;
using task_tracker.Store;

namespace task_tracker.Handlers;

/// <summary>
/// Handler for operationId: TaskService_get
/// GET /{id} — Returns a single task by its unique ID, or 404 if not found.
/// </summary>
public static class TaskServiceGet
{
    public static IResult Handle(string id, TaskStore store)
    {
        var task = store.GetById(id);
        if (task == null)
        {
            return Results.NotFound(new ApiError { Message = $"Task with id '{id}' not found." });
        }
        return Results.Ok(task);
    }
}
