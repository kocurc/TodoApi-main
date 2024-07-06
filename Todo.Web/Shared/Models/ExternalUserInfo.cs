using System.ComponentModel.DataAnnotations;

namespace Todo.Web.Shared.Models;

public class ExternalUserInfo
{
    [Required]
    public string Username { get; set; } = default!;

    [Required]
    public string KeyProvider { get; set; } = default!;
}
