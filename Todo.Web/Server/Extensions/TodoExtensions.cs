using Todo.Web.Shared.Models;

namespace Todo.Web.Server.Extensions;

public static class TodoExtensions
{
    public static TodoItem AsTodoItem(this Shared.Models.Todo todo)
    {
        return new TodoItem
        {
            Id = todo.Id,
            Title = todo.Title,
            IsComplete = todo.IsComplete,
        };
    }
}
