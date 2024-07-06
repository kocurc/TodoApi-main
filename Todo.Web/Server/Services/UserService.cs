using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Todo.Web.Server.Authentication;
using Todo.Web.Shared.Models;

namespace Todo.Web.Server.Services;

public class UserService(UserManager<IdentityUser> userManager, ITokenService tokenService)
{
    public async Task<IdentityResult> CreateUserAsync(UserInfo newUser)
    {
        var user = new IdentityUser { UserName = newUser.Username };
        return await userManager.CreateAsync(user, newUser.Password);
    }

    public async Task<AuthenticationToken?> GenerateTokenAsync(UserInfo userInfo)
    {
        var user = await userManager.FindByNameAsync(userInfo.Username);
        if (user != null && await userManager.CheckPasswordAsync(user, userInfo.Password))
        {
            return new AuthenticationToken(tokenService.GenerateToken(user.UserName!));
        }
        return null;
    }

    public async Task<(AuthenticationToken? Token, IdentityResult Result)> GenerateExternalTokenAsync(string provider, ExternalUserInfo userInfo)
    {
        var user = await userManager.FindByLoginAsync(provider, userInfo.KeyProvider);
        var result = IdentityResult.Success;

        if (user == null)
        {
            user = new IdentityUser { UserName = userInfo.Username };
            result = await userManager.CreateAsync(user);

            if (result.Succeeded)
            {
                result = await userManager.AddLoginAsync(user, new UserLoginInfo(provider, userInfo.KeyProvider, displayName: null));
            }
        }

        if (result.Succeeded)
        {
            return (new AuthenticationToken(tokenService.GenerateToken(user.UserName!)), result);
        }

        return (null, result);
    }
}