/* Shared classes can be referenced by both the Client and Server */

using System.ComponentModel.DataAnnotations;

namespace Todo.Web.Shared
{
    public class TodoItem
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = default!;

        public bool IsComplete { get; set; }
    }
}
