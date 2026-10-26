using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;
// using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// DI Container for interface class
builder.Services.AddSingleton<ITaskService>(new InMemoryTaskService());

var app = builder.Build();

// our URL Rewriter middleware - (.*) means whatever number
app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));

// todos All List
var todos = new List<Todo>();

// Get All API
app.MapGet("/todos", (ITaskService service) => service.GetTodos());

// GET by id API
app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id, ITaskService service) =>
{
    // var targetTodo = todos.SingleOrDefault(t => t.Id == id); -- old code before implementing ITaskService, Still correct.

    var targetTodo = service.GetTodoById(id);
    return targetTodo is null ? TypedResults.NotFound() : TypedResults.Ok(targetTodo);
});

// POST API
app.MapPost("/todos", (Todo task, ITaskService service) =>
{
    // todos.Add(task);

    service.AddTodo(task);
    return TypedResults.Created($"/todos/{task.Id}", task);
})
.AddEndpointFilter(async (context, next) =>
{
    var task = context.GetArgument<Todo>(0);

    return task.Name.Length > 2 
        ? await next(context) 
        : Results.BadRequest("Name must be greater than 2 characters.");
});


// Delete API
app.MapDelete("/todos/{id}", (int id, ITaskService service) =>
{
    // todos.RemoveAll(t => t.Id == id);

    service.DeleteTodoById(id);
    return TypedResults.NoContent();
});


app.Run();


public record Todo(int Id, string Name);

// Dependency Injection
interface ITaskService
    {
        Todo? GetTodoById(int id);
        List<Todo> GetTodos();
        void DeleteTodoById(int id);
        Todo AddTodo(Todo task);
    }


class InMemoryTaskService : ITaskService
{
    private readonly List<Todo> _todos = [];

    public Todo AddTodo(Todo task)
    {
        _todos.Add(task);
        return task;
    }

    public void DeleteTodoById(int id)
    {
        _todos.RemoveAll(task => id == task.Id);
    }

    public Todo? GetTodoById(int id)
    {
        return _todos.SingleOrDefault(t => id == t.Id);
    }

    public List<Todo> GetTodos()
    {
        return _todos;
    }

}