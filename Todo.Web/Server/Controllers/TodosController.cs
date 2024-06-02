using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Todo.Web.Server.Authorization;
using Todo.Web.Server.Services;
using Todo.Web.Shared.Models;

namespace Todo.Web.Server.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class TodosController(TodoService todoService, CurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TodoItem>>> GetTodos()
    {
        var todos = await todoService.GetTodosAsync(currentUser);
        return Ok(todos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TodoItem>> GetTodoById(int id)
    {
        var todo = await todoService.GetTodoByIdAsync(id, currentUser);
        if (todo == null)
        {
            return NotFound();
        }
        return Ok(todo);
    }

    [HttpPost]
    public async Task<ActionResult<TodoItem>> CreateTodo([FromBody] TodoItem newTodo)
    {
        var todo = await todoService.CreateTodoAsync(newTodo, currentUser);
        return CreatedAtAction(nameof(GetTodoById), new { id = todo.Id }, todo);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTodo(int id, [FromBody] TodoItem todo)
    {
        if (!await todoService.UpdateTodoAsync(id, todo, currentUser))
        {
            return NotFound();
        }
        return Ok();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTodo(int id)
    {
        if (!await todoService.DeleteTodoAsync(id, currentUser))
        {
            return NotFound();
        }
        return Ok();
    }
}
