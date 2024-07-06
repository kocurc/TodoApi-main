using System.ComponentModel.DataAnnotations;

namespace Todo.Web.Shared.Models;

public class Todo
{
    public int Id { get; set; }

    public bool IsComplete { get; set; }

    [Required]
    public string Title { get; set; } = default!;

    [Required]
    public string OwnerId { get; set; } = default!;
}
