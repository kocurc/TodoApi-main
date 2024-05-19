using System.ComponentModel.DataAnnotations;

namespace Todo.Web.Shared.SharedClasses;

public class TodoItem
{
    [Required]
    public string Title { get; set; } = default!;

    public int Id { get; set; }

    public bool IsComplete { get; set; }
}
