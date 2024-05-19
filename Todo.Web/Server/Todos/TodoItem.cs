using System.ComponentModel.DataAnnotations;

namespace Todo.Web.Server.Todos;

// The DTO that excludes the OwnerId (we don't want that exposed to clients)
public class TodoItem
{
    public int Id { get; set; }

    public bool IsComplete { get; set; }

    [Required]
    public string Title { get; set; } = default!;

}
