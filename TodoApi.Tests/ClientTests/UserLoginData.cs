namespace Tests.ClientTests
{
    public class UserLoginData(string password, string userName)
    {
        public string UserName { get; init; } = userName;
        public string Password { get; init; } = password;
    }
}
