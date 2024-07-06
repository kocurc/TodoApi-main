namespace Todo.Web.Server.Authentication;

public interface ITokenService
{
    string GenerateToken(string username, bool isAdministrator = false);
}
