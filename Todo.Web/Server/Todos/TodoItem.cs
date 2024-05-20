using System.ComponentModel.DataAnnotations;

namespace Todo.Web.Server.Todos;

public class TodoItem
{
    public int Id { get; set; }

    public bool IsComplete { get; set; }

    [Required]
    public string Title { get; set; } = default!;
}
