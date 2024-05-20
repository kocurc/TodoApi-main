using Todo.Web.Server.Todos;
using Todo.Web.Shared.SharedClasses;

namespace Todo.Web.Server.Extensions;

public static class TodoExtensions
{
    public static TodoItem AsTodoItem(this Todos.Todo todo)
    {
        return new TodoItem
        {
            Id = todo.Id,
            Title = todo.Title,
            IsComplete = todo.IsComplete,
        };
    }
}
