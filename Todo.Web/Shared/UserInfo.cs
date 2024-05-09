using System.ComponentModel.DataAnnotations;

namespace Todo.Web.Shared
{
    public class UserInfo
    {
        [Required]
        public string Username { get; set; } = default!;

        [Required]
        public string Password { get; set; } = default!;
    }
}
