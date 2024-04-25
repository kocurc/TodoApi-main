using System.ComponentModel.DataAnnotations;

namespace Todo.Web.Shared;

public class ExternalUserInfo
{
    [Required]
    public string Username { get; set; } = default!;

    [Required]
    public string ProviderKey { get; set; } = default!;
}
