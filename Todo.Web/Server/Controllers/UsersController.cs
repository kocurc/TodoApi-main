using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Todo.Web.Server.Services;
using Todo.Web.Shared.Models;

namespace Todo.Web.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController(UserService userService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> CreateUser([FromBody] UserInfo newUser)
    {
        var result = await userService.CreateUserAsync(newUser);
        if (result.Succeeded)
        {
            return Ok();
        }

        var validationProblemDetails = new ValidationProblemDetails(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
        return ValidationProblem(validationProblemDetails);
    }

    [HttpPost("token")]
    public async Task<ActionResult<AuthenticationToken>> GenerateToken([FromBody] UserInfo userInfo)
    {
        var token = await userService.GenerateTokenAsync(userInfo);
        if (token != null)
        {
            return Ok(token);
        }

        return BadRequest();
    }

    [HttpPost("token/{provider}")]
    public async Task<ActionResult<AuthenticationToken>> GenerateExternalToken(string provider, [FromBody] ExternalUserInfo userInfo)
    {
        var (token, result) = await userService.GenerateExternalTokenAsync(provider, userInfo);
        if (token != null)
        {
            return Ok(token);
        }

        var validationProblemDetails = new ValidationProblemDetails(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
        return ValidationProblem(validationProblemDetails);
    }
}