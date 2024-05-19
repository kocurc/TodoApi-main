namespace Todo.Web.Server.Authentication;

public interface ITokenService
{
    // Generate a JWT token for the specified user name and admin role
    string GenerateToken(string username, bool isAdmin = false);
}
