using task_tracker.Store;

namespace task_tracker.Handlers;

/// <summary>
/// Handler for operationId: TaskService_list
/// GET / — Returns all tasks in the collection.
/// </summary>
public static class TaskServiceList
{
    public static IResult Handle(TaskStore store)
    {
        var tasks = store.GetAll();
        return Results.Ok(tasks);
    }
}
