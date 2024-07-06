using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Todo.Web.Server.Authorization;
using Todo.Web.Server.Database;
using Todo.Web.Server.Extensions;
using Todo.Web.Shared.Models;

namespace Todo.Web.Server.Services;

public class TodoService(TodoDbContext dbContext)
{
    public async Task<List<TodoItem>> GetTodosAsync(CurrentUser owner)
    {
        return await dbContext.Todos
            .Where(todo => todo.OwnerId == owner.Id)
            .Select(t => t.AsTodoItem())
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<TodoItem?> GetTodoByIdAsync(int id, CurrentUser owner)
    {
        var todo = await dbContext.Todos.FindAsync(id);

        if (todo != null && (todo.OwnerId == owner.Id || owner.IsAdmin))
        {
            return todo.AsTodoItem();
        }
        return null;
    }

    public async Task<TodoItem> CreateTodoAsync(TodoItem newTodo, CurrentUser owner)
    {
        var todo = new Shared.Models.Todo
        {
            Title = newTodo.Title,
            OwnerId = owner.Id
        };

        dbContext.Todos.Add(todo);

        await dbContext.SaveChangesAsync();

        return todo.AsTodoItem();
    }

    public async Task<bool> UpdateTodoAsync(int id, TodoItem todo, CurrentUser owner)
    {
        if (id != todo.Id)
        {
            return false;
        }

        var rowsAffected = await dbContext.Todos
            .Where(t => t.Id == id && (t.OwnerId == owner.Id || owner.IsAdmin))
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(t => t.IsComplete, todo.IsComplete)
                    .SetProperty(t => t.Title, todo.Title));

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteTodoAsync(int id, CurrentUser owner)
    {
        var rowsAffected = await dbContext.Todos
            .Where(t => t.Id == id && (t.OwnerId == owner.Id || owner.IsAdmin))
            .ExecuteDeleteAsync();

        return rowsAffected > 0;
    }
}
