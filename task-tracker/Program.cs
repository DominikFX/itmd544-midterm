using System.Text.Json;
using YamlDotNet.Serialization;
using task_tracker.Handlers;
using task_tracker.Middleware;
using task_tracker.Models;
using task_tracker.Store;

//Build the application
var builder = WebApplication.CreateBuilder(args);

//Register the in-memory store as a singleton so all handlers share state
builder.Services.AddSingleton<TaskStore>();

// Configure JSON serialization to use camelCase (matches OpenAPI convention)
builder.Services.ConfigureHttpJsonOptions(options =>
{
  options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

// Locate the OpenAPI YAML spec
var specPath = Path.Combine(AppContext.BaseDirectory, "openapi.yaml");
if (!File.Exists(specPath))
{
  specPath = Path.Combine(app.Environment.ContentRootPath, "..", "openapi.yaml");
}

Console.WriteLine($"Spec loaded: {specPath}");

// Register the spec-driven validation middleware
app.UseMiddleware<OpenApiValidationMiddleware>(specPath);

// YAML
app.MapGet("/openapi.yaml", () =>
{
    var yaml = File.ReadAllText(specPath);
    return Results.Text(yaml, "text/yaml");
}).ExcludeFromDescription();

// Serve the JSON representation of the spec (YAML → JSON via YamlDotNet)
app.MapGet("/openapi.json", () =>
{
    var yamlText = File.ReadAllText(specPath);
    var deserializer = new DeserializerBuilder().Build();
    var yamlObject = deserializer.Deserialize<object>(yamlText);
    var serializer = new SerializerBuilder()
        .JsonCompatible()
        .Build();
    var json = serializer.Serialize(yamlObject);
    return Results.Text(json, "application/json");
}).ExcludeFromDescription();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi.yaml", "Task Tracker API v1");
    options.RoutePrefix = "docs";
});

// API routes — each maps to an operationId from the spec

// operationId: TaskService_summary — GET /summary
app.MapGet("/summary", (TaskStore store) => TaskServiceSummary.Handle(store))
    .WithName("TaskService_summary");

// operationId: TaskService_list — GET /
app.MapGet("/", (TaskStore store) => TaskServiceList.Handle(store))
    .WithName("TaskService_list");

// operationId: TaskService_create — POST /
app.MapPost("/", (TaskCreateRequest request, TaskStore store) =>
    TaskServiceCreate.Handle(request, store))
    .WithName("TaskService_create");

// operationId: TaskService_get — GET /{id}
app.MapGet("/{id}", (string id, TaskStore store) => TaskServiceGet.Handle(id, store))
    .WithName("TaskService_get");

// operationId: TaskService_update — PATCH /{id}
app.MapPatch("/{id}", (string id, TaskUpdateRequest request, TaskStore store) =>
    TaskServiceUpdate.Handle(id, request, store))
    .WithName("TaskService_update");

// operationId: TaskService_delete — DELETE /{id}
app.MapDelete("/{id}", (string id, TaskStore store) =>
    TaskServiceDelete.Handle(id, store))
    .WithName("TaskService_delete");

// Print registered routes and start
Console.WriteLine();
Console.WriteLine("Task Tracker API running");
Console.WriteLine();
Console.WriteLine("Registered operationIds:");
Console.WriteLine("  GET    /          → TaskService_list");
Console.WriteLine("  POST   /          → TaskService_create");
Console.WriteLine("  GET    /summary   → TaskService_summary");
Console.WriteLine("  GET    /{id}      → TaskService_get");
Console.WriteLine("  PATCH  /{id}      → TaskService_update");
Console.WriteLine("  DELETE /{id}      → TaskService_delete");
Console.WriteLine();
Console.WriteLine("Documentation:");
Console.WriteLine("  /docs          → Swagger UI");
Console.WriteLine("  /openapi.yaml  → Raw YAML spec");
Console.WriteLine("  /openapi.json  → JSON spec");

app.Run();
